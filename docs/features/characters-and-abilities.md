# Characters & abilities

## Purpose

Turns "pick 1 of 3 characters, each with 2 abilities" into data: a `CharacterDefinition` holds the whole hero
(stats, body, one weapon and two `AbilityDefinition` assets); `AbilityFactory` maps `AbilityDefinition.kind` to a
plain C# `IAbility` class that runs on press; `PlayerAbilities` owns the two slots, the cooldown clocks and the
ratios the HUD will read. Selecting a hero is one call to `HeroLoadout.Apply` — there is no per-character script.

## Interface

```csharp
class CharacterDefinition : ScriptableObject {    // menu: Hero Shooter/Character Definition
    string displayName; string pitch;
    float bodyScale; Color color;
    float maxHealth, moveSpeed, sprintMultiplier;
    WeaponDefinition weapon;
    AbilityDefinition abilityPrimary, abilitySecondary;   // index 0 = Q, 1 = E
    AbilityDefinition AbilityAt(int index);               // index 0 → primary, anything else → secondary
}

enum AbilityKind { None = 0,
    GroundSlam = 10, Bulwark = 11,        // Rock
    MarkTarget = 20, Snare = 21,          // Paper
    WallClimb = 30, Overclock = 31 }      // Scissors

class AbilityDefinition : ScriptableObject {      // menu: Hero Shooter/Ability Definition
    string displayName; AbilityKind kind; string description;
    float cooldown, duration, magnitude, radius, range;
    string slotLabel;                             // "Q" / "E" shown in UI copy
}

class AbilityContext {                            // everything an ability may touch; built once per match
    Transform Player, CameraTransform;
    Health Health; PlayerStatus Status; Weapons.Weapon Weapon;
    MonoBehaviour Host;                           // PlayerAbilities — the object abilities StartCoroutine on
    LayerMask EnemyMask, WorldMask;
}

interface IAbility { float Cooldown { get; } void Activate(AbilityContext context); }

abstract class AbilityBase : IAbility {           // plain C#, not a MonoBehaviour
    protected readonly AbilityDefinition Definition;
    float Cooldown { get; }                       // definition.cooldown, or 5 if null/<=0
    protected float Magnitude, Duration, Radius, Range;   // properties over Definition, 0 when it is null
}

static class AbilityFactory { IAbility Create(AbilityDefinition definition); }   // null in → null out

class PlayerAbilities : MonoBehaviour {
    const int SlotCount = 2;
    AbilityDefinition[] Definitions { get; }
    bool IsReady(int slot);
    float CooldownRatio(int slot);                // fraction of cooldown REMAINING: 1 right after use,
                                                  // 0 when ready — reverse of its XML doc comment
    void Configure(CharacterDefinition character);// builds both slots + context; also resets both cooldowns
    bool TryActivate(int slot);                   // false when not ready / no definition / no context
}

class HeroLoadout : MonoBehaviour {
    CharacterDefinition Current { get; }
    void Apply(CharacterDefinition character);    // health, motor, weapon, abilities, appearance — in that order
}

class PlayerAppearance : MonoBehaviour { void Apply(CharacterDefinition character); }  // body scale + colour
                                                          // via MaterialPropertyBlock (no material instancing)

class MatchBootstrap : MonoBehaviour {
    CharacterDefinition[] Roster { get; }
    CharacterDefinition Selected { get; }
    bool SelectionOpen { get; }
    void BeginSelection();                        // rig off, hero GameObject off, cursor free, panel shown
    void Select(int index);                       // hero on → Apply → reposition → panel hidden → rig on
}

class CharacterSelectUI : MonoBehaviour {         // controller for Prefabs/UI/CharacterSelect.prefab
    bool IsVisible { get; }
    void Show(CharacterDefinition[] roster, MatchBootstrap bootstrap);
    void Hide();
}

class CharacterCardUI : MonoBehaviour {           // one authored card; button is a persistent listener
    void Click();                                 // wired in the prefab, calls bootstrap.Select(rosterIndex)
    void Bind(CharacterDefinition character, int index, MatchBootstrap bootstrap);
}
```

## Depends on

- `damage-and-pooling.md` — `Health` (max health), `DamageInfo` (`GroundSlam`).
- `player-movement-and-camera.md` — `PlayerMotor.Configure`, `PlayerStatus` as the only effect sink,
  `InputReader.Ability1Pressed/Ability2Pressed`, `Camera.main` for the ability context.
- `weapons-and-projectiles.md` — `Weapon.SetDefinition`.
- `enemy-fsm.md` — `AimRaycast` targets the Enemy layer; `Snare` writes `EnemyBrain.SpeedMultiplier`;
  `GroundSlam` calls `EnemyBrain.Push`.

## Depended on by

- `hud-aiming-and-feedback.md` — `PlayerAbilities.CooldownRatio` / `IsReady` / `Definitions` and
  `Weapon.ChargeRatio` are the intended HUD inputs; ability VFX hang off these six classes.
- Nothing else. This is the top of the dependency graph.

## Behavior notes

- `PlayerAbilities.Update` reads `Ability1Pressed` / `Ability2Pressed` and calls `TryActivate`. Cooldown is
  written **after** the ability runs (`_readyAt[slot] = Time.time + Cooldown`), so an ability that throws would
  not go on cooldown — nothing throws today.
- `Cooldown` falls back to **5 s** when the definition is null or its `cooldown <= 0`; that fallback is the
  reason a mis-wired slot still blocks for 5 s instead of being free.
- `CooldownRatio` returns `(_readyAt - now) / cooldown`: **1 immediately after use, decaying to 0 when ready.**
  The XML doc comment on the method says the opposite ("1 = fully ready, 0 = just used"), so a HUD that trusts
  the comment draws an inverted bar. Use `1 - CooldownRatio(slot)` for a fill that grows toward ready, and fix
  the comment when the HUD lands.
- Abilities are plain C# objects created once per selection (`AbilityFactory.Create`). They cannot use
  `StartCoroutine` themselves — `MarkTarget`, `Snare` (and the planned Slam driver) run coroutines on
  `context.Host`, i.e. `PlayerAbilities`.
- `HeroLoadout.Apply` order matters: health → motor → weapon → abilities → appearance. `PlayerAbilities.Configure`
  rebuilds the `AbilityContext`, which resolves `Camera.main` at that moment; if the camera is missing the
  context falls back to the player's own transform and every aim-based ability casts from the hero, not the
  crosshair.
- `MatchBootstrap.BeginSelection` **deactivates the hero GameObject** (so no input runs) and later reactivates
  it and calls `Apply`. `Start` always opens the selection panel; there is no "skip" path, so any test scene
  that adds `MatchBootstrap` starts with a modal choice screen.
- A card's button is a **persistent listener** authored on the prefab calling `CharacterCardUI.Click()`; the
  card knows its own `rosterIndex`, so the prefab carries no per-card arguments. `CharacterSelectUI` owns no
  layout — it binds cards, toggles the canvas and unlocks the cursor.
- `AimRaycast.FindTarget` casts from the camera forward along `EnemyMask | WorldMask` and returns the hit only
  if it is on the Enemy layer, so a wall closer than the enemy means "no target". Both Mark and Snare share it,
  which is why they cannot disagree about what the crosshair is over.

## Ability implementations as built (2026-10-23, after S3)

| Ability | Class | Asset values | What the code actually does |
|---|---|---|---|
| Rock Q — Slam | `GroundSlam` | cd 8, mag 40, radius 5 | `OverlapSphere` at the hero's feet: damage + `Push(away, 6)`. **No jump, no descent phase, no landing.** |
| Rock E — Bulwark | `Bulwark` | cd 14, dur 4, mag 0.4 | one-shot buff: damage ×0.4 and speed ×0.75 for 4 s. **Shooting stays enabled; no meter.** |
| Paper Q — Mark | `MarkTarget` | cd 10, dur 6, mag 1.35, range 70 | sets `Health.IncomingDamageMultiplier = 1.35`, restores the previous value after 6 s via coroutine |
| Paper E — Snare | `Snare` | cd 12, dur 3, mag 0.25, range 14 | sets `EnemyBrain.SpeedMultiplier = 0.25`, restores after 3 s via coroutine |
| Scissors Q — Climb | `WallClimb` | cd 3, dur 0.9, mag 6 (**ignored**) | adds the `Climb` status effect → `PlayerStatus` lifts the controller at its own hardcoded 7 m/s for 0.9 s. `WallClimb.Activate` passes a literal `1f` as the magnitude, so the asset's `magnitude = 6` is dead data. **No wall contact test, no cap, no mantle.** |
| Scissors E — Overclock | `Overclock` | cd 12, dur 3, mag 1.8 | fire rate ×1.8 and speed ×1.25 for 3 s (no VFX yet) |

Heroes as built: Rock 200 hp / 4.2 m/s / sprint ×1.35 / body ×1.25 orange; Paper 90 / 5.2 / ×1.6 / ×0.9 pale
blue; Scissors 130 / 6.8 / ×1.7 / ×0.95 green. These match `docs/HERO_ROSTER.md` except that the roster's
per-hero aim multipliers do not exist.

## Drift against `docs/HERO_ROSTER.md` (roster wins; the code is wrong)

Fixing these is the ability rework in `docs/tasks/2026-10-23_S3b-ability-rework.md`, not a bug hunt:

- Slam must become jump → forced 2.5× descent → landing AoE, grounded start only, no steering.
- Bulwark must become a channelled 5 s meter (×0.4 damage, no shooting, ×0.75 speed, 0.5 s / 8 s cooldowns,
  refill 1/s starting 1 s after release) instead of a 4 s buff on a 14 s cooldown.
- Climb must become a passive wall traversal (6 m cap, 3 m/s, exit jump = wall normal + 2 m up, mantle over a
  lower wall top, reset on floor) instead of a cooldown button.
- RMB aiming (FOV 60→42, blend 8/s, +0.55 right / −0.20 down, ×0.9 move, Paper ×0.6, spread −50 %) is not built.
- Mark/Snare/Overclock have no visible feedback yet (`docs/features/hud-aiming-and-feedback.md`).
- **`InputReader.Ability1Pressed` includes `rightButton`**, which collides head-on with RMB aiming. The rework
  must drop RMB from the ability binding before the aim state lands.

## Known limitations / deliberately not handled

- No passive/instant distinction in code: `AbilityKind` decides behaviour but every slot is a cooldown button,
  so the roster's "passive" Climb has no representation yet (the ledger decision records instant / channelled /
  passive as the intended three shapes).
- No ability charges, no ultimate meter, no cancellation, no aiming prediction for moving targets.
- Mark and Snare restore a single "previous" value, so stacking two casts of the same ability on one enemy
  corrupts the restore (see `damage-and-pooling.md`).
- Abilities have no cost, no VFX and no range indicator. `AbilityDefinition.description` is read by **nobody**
  (`grep -rn description Assets/_Project/Scripts` → its declaration only): the select card renders
  `displayName` + `cooldown` from `CharacterCardUI.AbilityLine`.
- `MatchBootstrap` is the whole match flow: no map select, no scene loading, no restart.
