# Enemy FSM

## Purpose

The graded ranged/melee FSM rows: **Search, Attack, Chase, Run away, Call friends**, twice over. The design
claim is "one brain, two bodies": a single `EnemyBrain` owns the NavMeshAgent, the health, the perception and
the state machine and makes every transition in one method; an archetype differs only by its `IEnemyAttack`
strategy (`RangedAttack` / `MeleeAttack`) plus an `EnemyStats` ScriptableObject.

## Interface

```csharp
enum EnemyState { None, Search, Chase, Attack, Flee, CallAllies }

interface IState { void Enter(); void Tick(float deltaTime); void Exit(); }

class StateMachine {                            // plain C# class, not a MonoBehaviour
    EnemyState Current { get; }
    float TimeInState { get; }
    event Action<EnemyState, EnemyState> Transitioned;   // (from, to)
    void Register(EnemyState key, IState state);
    void Tick(float deltaTime);
    void ChangeState(EnemyState next);          // no-op when next == Current
}

class EnemyStats : ScriptableObject {           // menu: Hero Shooter/Enemy Stats
    float maxHealth, searchSpeed, chaseSpeed, fleeSpeed;
    float sightRange, loseSightGrace;
    float attackRange /* read by nobody — see below */, stopDistance, damage, attackCooldown;
    [Range(0,1)] float fleeHealthPercent;  float fleeDuration;
    float allyCallRadius, allyCallCooldown, callAlliesDuration;
}

[RequireComponent(typeof(Health))] [RequireComponent(typeof(NavMeshAgent))]
class EnemyBrain : MonoBehaviour {
    // serialized: stats, spyglass, logTransitions = true
    EnemyStats Stats { get; }
    StateMachine Machine { get; }
    Health Health { get; }
    NavMeshAgent Agent { get; }
    IEnemyAttack Attack { get; }
    Transform Target { get; }
    Transform CallTarget { get; }               // the target the last answered call pointed at; null outside CallAllies
    Vector3 SpawnPoint { get; }
    bool HasTarget { get; }
    float SpeedMultiplier { get; set; }         // 1; Paper's Snare sets 0.25
    float DistanceToTarget { get; }
    float TimeInState { get; }

    void MoveTo(Vector3 worldPosition, float speed);   // applies SpeedMultiplier
    void Push(Vector3 direction, float force);         // knockback, decays in Update
    void HoldPosition();                               // agent.isStopped = true
    void FaceTarget(Transform target);
    void RequestAllyCall();                            // self-throttled by stats.allyCallCooldown
    void EndFlee();                                    // re-arms flee after fleeDuration * 2
    void EndCallAllies();
}

interface IEnemyAttack {
    float Range { get; }                        // max distance at which Attack may be entered
    bool TryAttack(Transform target);           // false while on cooldown
}

class Spyglass : MonoBehaviour {                // perception only
    // serialized: targetTag = "Player", eyeHeight = 1.4, sightBlockers (mask)
    Transform Target { get; }
    bool HasTarget { get; }                     // in grace window
    float DistanceToTarget { get; }
    void Initialise(float sightRange, float loseSightGrace);
    bool InRange(float range);
    bool CanSee(Transform target);
    void Refresh(float sightRange);             // called once per frame by the brain
}

static class AllyCall {
    static event Action<EnemyBrain, Transform> Broadcasted;   // (caller, caller's target)
    static void Broadcast(EnemyBrain caller, Transform target);
    static void Clear();
}
```

## Depends on

- `damage-and-pooling.md` — `Health` for hit points, `Normalized` for the flee threshold, `Died` for corpse
  handling; the pool via `RangedAttack`.
- `weapons-and-projectiles.md` — `RangedAttack` fires through `ProjectilePool.Fire` with its own `targetMask`.
- `player-movement-and-camera.md` — the player must carry the `Player` tag and the Player layer, or perception
  and enemy bullets find nothing.

## Depended on by

- `characters-and-abilities.md` — `GroundSlam` calls `EnemyBrain.Push`; `Snare` writes `SpeedMultiplier`;
  `AimRaycast` filters by the Enemy layer, which the enemy prefabs sit on.
- `hud-aiming-and-feedback.md` — enemy health bars are meant to read `Health.Normalized` per enemy and to
  change colour from `Health.IncomingDamageMultiplier` (marked) and `EnemyBrain.SpeedMultiplier` (snared).

## Behavior notes

**Transition table** — `EnemyBrain.EvaluateTransitions`, in priority order, once per `Update`:

1. Current state is `Flee` or `CallAllies` → return immediately; both are time-boxed and end themselves
   through `EndFlee()` / `EndCallAllies()`.
2. No target → `Search`.
3. `Health.Normalized <= stats.fleeHealthPercent` and past the flee cooldown → `Flee`.
4. `DistanceToTarget <= Attack.Range` → `Attack`.
5. otherwise → `Chase`.

- `EndFlee` sets `_nextFleeTime = Time.time + fleeDuration * 2`, so an enemy that survives a flee cannot
  immediately flee again and will not stutter between Flee and Chase. `EndCallAllies` has no such cooldown but
  `RequestAllyCall` throttles broadcasts to one per `allyCallCooldown` (12 s).
- Hearing a call (`OnAllyCallHeard`) is ignored when: caller is self, brain is dead, distance to the caller
  exceeds `stats.allyCallRadius` (16 m), or the current state is already `Flee` or `Attack`. Otherwise it stores
  the caller's target in `CallTarget` and the state becomes `CallAllies`. `CallAlliesState` walks to
  **`CallTarget`** — the caller's target — falling back to its own `Spyglass.Target` only when that is null, at
  `chaseSpeed` for `callAlliesDuration` (4 s). `CallTarget` is cleared by `EndCallAllies`.
  Play-mode evidence (2026-10-23): an ally whose own `Spyglass.Target` was a decoy 18 m away entered
  `CallAllies` with `callTarget=Player`, `agent.destination` = the player's position, and closed 10.48 → 5.63 m
  on the player while moving *away* from its own target (18.37 → 22.98 m).
- Calls are broadcast from two places: `RangedAttack.TryAttack` (every shot attempt) and `MeleeAttack.Swing`
  after a landed swing. Both only *request*; the brain owns the cooldown.
- Death (`OnDied`) sets `_isDead`, stops and disables the agent, and stops coroutines. The corpse stays in the
  scene — recycling belongs to the wave director that does not exist yet. `Update` returns early for a corpse,
  so a dead enemy cannot think, be pushed, or answer further calls.
- Pooled re-use: `OnEnable` calls `Health.ResetHealth()`, so re-activating a body restores it. `_isDead` is
  never reset — a recycled enemy would stay inert. Anything that recycles enemies must fix this.
- `SearchState` wanders within 9 m of `SpawnPoint`, repathing every 4 s or on arrival, snapping each candidate
  through `NavMesh.SamplePosition` (4 m radius, all areas). It keeps moving even while it has a target: the
  brain only leaves Search when `HasTarget` is true, and `SearchState` is not target-aware.
- `Spyglass` acquires the target by tag (`GameObject.FindGameObjectWithTag("Player")`) and **retries every
  frame while `Target` is null** (`Refresh`), then re-checks line of sight with one ray from eye height (1.4 m)
  to the target's chest (`+1 m`). `HasTarget` stays true for `loseSightGrace` seconds after the last sighting.
  `Initialise` is called only from `EnemyBrain.Awake`, and only when `stats != null`: **an enemy with no
  `EnemyStats` returns from `Update` before `Refresh`, so it perceives nothing at all** — not merely a zero grace.
- `Attack` calls `HoldPosition()` on enter, `FaceTarget` + `TryAttack` per tick, and re-enables the agent on exit.
  Attack cooldown is owned by the strategy, not the state.
- `MeleeAttack`: `range` 2.2, `hitRadius` 2.6, `windUp` 0.35. The cooldown is set to
  `stats.attackCooldown + windUp`; the hit is validated against `hitRadius` after the wind-up, so a target that
  steps back during the telegraph is missed.
- `RangedAttack`: `range` 14, `projectileSpeed` 45, aims at `target.position + up * 1`, fires with
  `targetMask` (prefab value 64 → Player only, so enemy fire cannot hit other enemies). It refuses to fire when
  `ProjectilePool.Instance` is null — a missing pool silently disables enemy shooting.
- Knockback (`Push`) stores one vector (`_knockback = direction.normalized * force`) — a second push in the
  same frame **replaces** the first instead of adding to it — and decays it with `Lerp` toward zero at 8/s while
  applying it through `agent.Move` every frame.
- Transitions are logged (`[EnemyBrain] name: From → To (dist X)`) while `logTransitions` is true. That log is
  the S2 gate evidence and the cheapest way to confirm the FSM in a demo.

## Known limitations / deliberately not handled

- **`EnemyStats.attackRange` is dead data.** `Attack.Range` comes from the strategy component's own serialized
  `range` field (`MeleeAttack` 2.2, `RangedAttack` 14), so the SO's `attackRange` is read by nobody (verified
  by grep: only the declaration matches). Changing it in the inspector does nothing. Either the strategies must
  read the SO or the field must be deleted — do not "fix" one side only.
- **Knockback does not move enemies in practice** (play-test 2026-10-23): the agent's next `SetDestination`
  cancels the `agent.Move` offset. The rework replaces the push with an agent-disabling impulse.
- **"Calls friends" is wired but blind outside its radius test.** A called ally converges on the caller's
  target, but the recall is only heard by enemies within `allyCallRadius` of the *caller*, and a called ally
  that is already in `Flee`/`Attack` ignores the call entirely. Nothing makes the caller pick a target worth
  converging on, and the state has no wall/cover check of its own — it just paths at the target.
- No wave spawning, no corpse recycling, no pooled enemies, no score/kill events; four enemies are hand-placed
  in `90_TestArena.unity`.
- No attack telegraph visual, no audio, no damage numbers, no ranged enemy strafing or backing off (the plan's
  "backs off when too close" was never implemented) and no cover use.
- `AttackState.Exit` sets `Agent.isStopped = false` unconditionally — a state exit after death would touch a
  disabled agent; it is safe today only because death freezes the machine first.
- Both enemy prefabs still carry `DamageLog`, the S1 console probe. It is intentional for now and must be
  removed once the HUD reads `Health` directly.
