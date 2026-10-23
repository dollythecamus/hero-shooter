# Damage & pooling

## Purpose

The two primitives every other system stands on. `DamageInfo` + `IDamageable` + `Health` give one shape for
"something was hit" and one owner for a hit-point value, so the player, the enemies and the test targets all
take damage through the same path. `IPooled` + `ObjectPool<T>` give the no-runtime-`Instantiate` rule the
assignment grades: objects are built once during warm-up and afterwards only toggle active.

## Interface

```csharp
struct DamageInfo {
    float Amount;        // pre-multiplier damage
    Vector3 Point;       // world-space impact point
    Vector3 Normal;      // surface normal at impact
    GameObject Source;   // who caused it; may be null
    bool IsCritical;     // never set true by any caller today
    DamageInfo(float amount, Vector3 point, Vector3 normal, GameObject source, bool isCritical = false);
}

interface IDamageable {
    bool IsAlive { get; }
    void TakeDamage(DamageInfo info);
}

class Health : MonoBehaviour, IDamageable {
    // serialized: maxHealth = 100, destroyOnDeath = false
    event Action<DamageInfo> Damaged;            // every accepted hit, after the value is applied
    event Action<DamageInfo> Died;               // once, when Current reaches 0
    float Max { get; }
    float Current { get; private set; }
    float Normalized { get; }                    // 0..1 of max; 0 when maxHealth <= 0
    bool IsAlive { get; }                        // Current > 0
    float IncomingDamageMultiplier { get; set; } // default 1; <1 armour, >1 vulnerability
    void ResetHealth();                          // Current = max (pool recycling)
    void ConfigureMaxHealth(float max);          // replaces maxHealth and refills; clamps to >= 1
    void TakeDamage(DamageInfo info);
}

interface IPooled { void OnSpawned(); void OnDespawned(); }

class ObjectPool<T> : MonoBehaviour where T : Component, IPooled {
    // serialized: prefab, warmUpCount = 32, container (defaults to this transform)
    T Prefab { get; }
    int TotalCreated { get; }
    int ActiveCount { get; }
    int InactiveCount { get; }
    void WarmUp(int count);
    protected virtual T CreateInstance();        // override to wire a back-reference
    T Get(Vector3 position, Quaternion rotation);
    void Release(T instance);
    [ContextMenu("Log pool stats")] void LogStats();   // the gate readout: created/active/inactive
}
```

## Depends on

Nothing. This is the root of the graph.

## Depended on by

- `weapons-and-projectiles.md` — `Projectile` implements `IPooled` and is pooled by `ProjectilePool`;
  hits land as `DamageInfo` on `IDamageable`, and `Weapon` reads `PlayerStatus`, not `Health`.
- `enemy-fsm.md` — `EnemyBrain` requires `Health`, sets it from `EnemyStats.maxHealth`, listens to `Died`,
  and uses `Normalized` for the flee threshold.
- `characters-and-abilities.md` — `GroundSlam` deals `DamageInfo` in a radius, `MarkTarget` writes
  `Health.IncomingDamageMultiplier`, `HeroLoadout` calls `ConfigureMaxHealth`.
- `hud-aiming-and-feedback.md` — the HUD is meant to bind to `Health.Damaged` / `Health.Died` and
  `Normalized`; enemy bars to `Normalized`.

## Behavior notes

- `TakeDamage` returns early when `!IsAlive` or `Amount <= 0`. Damage applied is
  `Amount * Mathf.Max(0f, IncomingDamageMultiplier)` — a multiplier of 0 means immune, not an error.
- Order inside `TakeDamage`: clamp the value → `Damaged` → if still alive return → `Died` → optional
  self-`Destroy`. A listener that kills the target from `Damaged` therefore fires `Died` inside the same call.
- `Died` fires at most once per life because later hits hit the `!IsAlive` early return.
- Every enemy prefab has `destroyOnDeath = false` (verified on both enemy prefabs). Corpses are handled by
  `EnemyBrain.OnDied` (agent disabled, coroutines stopped). Do **not** flip this flag: it would delete the
  body out from under the state machine and, later, under wave accounting.
- `Health.Awake` sets `Current = maxHealth`. `EnemyBrain.Awake` calls `ConfigureMaxHealth(stats.maxHealth)` and
  `EnemyBrain.OnEnable` calls `ResetHealth()` — `OnEnable` runs after `Awake` on the first activation, so a
  freshly spawned enemy is refilled twice. Harmless, but it is why pooled re-use works with no reset code.
- `ObjectPool.Get` creates a **new** instance when the queue is empty; the pool grows at runtime. The S1 gate
  is the counter: `created=64` before the test, 61 shots fired, `created=64` after. If `TotalCreated` climbs
  during play, warm-up is too small for the firefight (or an instance was released twice).
- `Release` has no double-release guard beyond a null check. Releasing the same instance twice puts it in the
  queue twice, and two later `Get` calls hand out the same GameObject. Only the object that owns the instance
  may return it — `Projectile.ReturnToPool` is the only caller today.
- `Get` does not parent or reparent; parenting is fixed at `CreateInstance` time from `container`.
- `Instantiate` appears in exactly one place (`Core/ObjectPool.cs:43`), which is the assignment's whole
  "object pooling, no instantiate" claim. Keep `grep -rn "Instantiate(" Assets/_Project/Scripts` at one hit.

## Known limitations / deliberately not handled

- No damage types, resistances, armour curves, crit maths (`IsCritical` is always false) and no healing API.
- No hit cooldown / invulnerability window: a shotgun-style multi-collider target can be hit several times
  in one frame.
- `IncomingDamageMultiplier` is a single settable float, so it cannot be layered. Two `MarkTarget` casts on
  the same enemy overlap badly: the second stores the first's 1.35 as "previous", so whichever coroutine
  restores last wins and the other is either cancelled early or left permanently applied. One caster per
  enemy is the current assumption.
- `Health` owns no visuals and no death flow by design; kill counting, corpse recycling and respawn belong to
  the wave/score systems that do not exist yet.
