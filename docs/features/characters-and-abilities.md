# Characters & abilities

> **Currency note (2026-11-06):** this document describes the system as designed and built on 2026-10-23/24. Everything that changed afterwards is listed in the *2026-11-06 update* at the bottom; where the two disagree, the update wins and `docs/PROJECT_LEDGER.md` has the evidence.

## Purpose

Turns "pick 1 of 3 characters, each with 2 abilities" into data: a `CharacterDefinition` holds the whole hero
(stats, body, one weapon and two `AbilityDefinition` assets); `AbilityDefinition.kind` decides the slot's shape
(instant / channelled / passive); instant kinds are plain C# `IAbility` classes built by `AbilityFactory`, the
three stateful ones are components on the hero; `PlayerAbilities` is the only router and the only place that
reads Q/E. Selecting a hero is one call to `HeroLoadout.Apply` — there is no per-character script.

## Interface

```csharp
class CharacterDefinition : ScriptableObject {    // menu: Hero Shooter/Character Definition
    string displayName; string pitch;
    float bodyScale; Color color;
    float maxHealth, moveSpeed, sprintMultiplier;
    float aimMoveMultiplier;                      // speed × while RMB aiming: 0.9 everywhere, Paper 0.6
    bool sniperReticle;                           // true on Paper only: fine aim crosshair instead of the shared one
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

interface IAbility { float Cooldown { get; } bool Activate(AbilityContext context); }   // false = nothing happened,
                                                                                        // so the cooldown is not spent

abstract class AbilityBase : IAbility {           // plain C#, not a MonoBehaviour
    protected readonly AbilityDefinition Definition;
    float Cooldown { get; }                       // definition.cooldown, or 5 if null/<=0
    protected float Magnitude, Duration, Radius, Range;   // properties over Definition, 0 when it is null
}

enum AbilityShape { None, Instant, Channel, Passive }
static class AbilityShapes { AbilityShape Of(AbilityKind kind); }   // GroundSlam → Instant (component),
                                                                    // Bulwark → Channel, WallClimb → Passive,
                                                                    // Mark/Snare/Overclock → Instant (class)

static class AbilityFactory { IAbility Create(AbilityDefinition definition); }   // instant kinds only;
                                                                                // null in → null out

class PlayerAbilities : MonoBehaviour {           // [DefaultExecutionOrder(-100)]: raises a shield before
    const int SlotCount = 2;                      // PlayerCombat reads the fire button on the same frame
    AbilityDefinition[] Definitions { get; }
    AbilityShape ShapeOf(int slot);
    bool IsReady(int slot);                       // instant: own clock · channel: shield.CanRaise · passive: true
    float CooldownRatio(int slot);                // HUD fill amount: 0 right after use, rising to 1 when ready
    float MeterNormalized(int slot);              // channel slot → Bulwark reservoir 0..1; otherwise 1
    void Configure(CharacterDefinition character);// builds both slots + context, configures the components,
                                                  // enables WallClimbPassive only for a hero that has it
    bool TryActivate(int slot);                   // press path; channel slots delegate to BulwarkShield.TryRaise
}

class BulwarkShield : MonoBehaviour {             // Rock E, channelled
    bool IsRaised { get; } bool CanRaise { get; }
    float Meter { get; } float Normalized { get; } float CooldownRatio { get; }
    void Configure(AbilityDefinition definition); // duration → reservoir (5 s), magnitude → damage ×
    bool TryRaise(); void Release();
}

class SlamDriver : MonoBehaviour {                // Rock Q, three phases
    bool IsActive { get; } float Radius { get; }
    void Configure(AbilityDefinition definition); // radius + damage from the asset
    bool TryStart();                              // false unless grounded and idle
}

class WallClimbPassive : MonoBehaviour {          // Scissors Q, passive trait
    bool IsClimbing { get; } float Climbed { get; }
    void Configure(AbilityDefinition definition); // magnitude → cap in metres (6)
}

class HeroLoadout : MonoBehaviour {
    CharacterDefinition Current { get; }
    void Apply(CharacterDefinition character);    // health, motor, weapon, abilities, appearance — in that order
}

class PlayerAppearance : MonoBehaviour { void Apply(CharacterDefinition character); }  // body scale + colour
                                                          // via MaterialPropertyBlock (no material instancing)

class MatchBootstrap : MonoBehaviour, ICharacterPicker {
    CharacterDefinition[] Roster { get; }
    CharacterDefinition Selected { get; }
    bool SelectionOpen { get; }
    void BeginSelection();                        // rig off, hero GameObject off, cursor free, panel shown
    void Select(int index);                       // hero on → Apply → reposition → panel hidden → rig on
    void PickCharacter(int index);                // ICharacterPicker — the select screen's entry point
}

// The card's callback is the interface, not MatchBootstrap: the same screen runs in 00_Boot under MainMenuUI.
interface ICharacterPicker { void PickCharacter(int index); }

class CharacterSelectUI : MonoBehaviour {         // controller for Prefabs/UI/CharacterSelect.prefab
    bool IsVisible { get; }
    void Show(CharacterDefinition[] roster, ICharacterPicker picker);
    void Hide();
}

class CharacterCardUI : MonoBehaviour {           // one authored card; button is a persistent listener
    void Click();                                 // wired in the prefab, calls picker.PickCharacter(rosterIndex)
    void Bind(CharacterDefinition character, int index, ICharacterPicker picker);
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
- `match-flow-and-maps.md` — `GameFlow` and `MainMenuUI` carry the pick into a map scene and drive
  `CharacterSelectUI` through `ICharacterPicker`; `PlayerAbilities.Activated` is the tutorial's ability gate.
- Nothing else. This is the top of the dependency graph.

## Behavior notes

- `PlayerAbilities.Update` reads `Ability1Pressed` / `Ability2Pressed` and calls `TryActivate`. Cooldown is
  written **after** the ability runs (`_readyAt[slot] = Time.time + Cooldown`), so an ability that throws would
  not go on cooldown — nothing throws today.
- `Cooldown` falls back to **5 s** when the definition is null or its `cooldown <= 0`; that fallback is the
  reason a mis-wired slot still blocks for 5 s instead of being free.
- `CooldownRatio` returns `1 - (_readyAt - now) / cooldown`: **0 immediately after use, rising to 1 when
  ready** — a uGUI `Image.fillAmount` / radial-fill value, so the HUD draws it as-is. (`IsReady` is the same
  clock as a bool.) Measured in play mode 2026-10-23 on Scissors' 3 s Climb: `0.000 / 0.107 / 0.312 / 0.520 /
  0.726 / 0.939 / 1.000`, with `IsReady` flipping true exactly at 1.000.
- Abilities are plain C# objects created once per selection (`AbilityFactory.Create`) — but only the instant
  ones. They cannot use `StartCoroutine` themselves, so `MarkTarget` and `Snare` run their restore coroutine on
  `context.Host`, i.e. `PlayerAbilities`. Slam, Bulwark and Climb are MonoBehaviour components instead, because
  they own per-frame state and, for Slam and Climb, the controller itself.
- Input routing is one method per slot (`HandleSlot`): a press activates an instant slot or begins a channel, and
  a release ends a channel. A passive slot is never read — `WallClimbPassive` asks `InputReader.JumpHeld`
  directly, and `PlayerAbilities.Configure` enables that component only when the selected hero has a
  `WallClimb` definition. RMB is not an ability button any more (`InputReader.AimHeld`), precisely so holding aim
  cannot begin a channel.
- An instant ability that finds nothing to do returns `false` and the cooldown is **not** spent (Mark or Snare
  with nothing under the crosshair, Slam while airborne). A channel is not on the shared cooldown at all:
  `BulwarkShield` owns the two cooldowns its own way (0.5 s after an early release, 8 s after depletion).
- `PlayerAbilities` runs at `[DefaultExecutionOrder(-100)]` so a shield raised on the same frame as the fire
  button blocks that shot; `WallClimbPassive` runs at `-50` so the jump key that starts a climb never fires the
  motor's jump.
- `HeroLoadout.Apply` order matters: health → motor → weapon → abilities → appearance. `PlayerAbilities.Configure`
  rebuilds the `AbilityContext`, which resolves `Camera.main` at that moment; if the camera is missing the
  context falls back to the player's own transform and every aim-based ability casts from the hero, not the
  crosshair.
- `MatchBootstrap.BeginSelection` **deactivates the hero GameObject** (so no input runs) and later reactivates
  it and calls `Apply`. Since S5, `Start` opens the selection panel only when there is no pick to inherit and no
  select screen in the scene: `GameFlow.SelectedCharacter` wins first (a map scene reached from `00_Boot`), then
  `roster[0]` (a map scene opened directly in the editor), and only then the modal pick. `90_TestArena` is the
  scene that still gets the modal screen. Flow and scene routing: `match-flow-and-maps.md`.
- A card's button is a **persistent listener** authored on the prefab calling `CharacterCardUI.Click()`; the
  card knows its own `rosterIndex`, so the prefab carries no per-card arguments. `CharacterSelectUI` owns no
  layout — it binds cards, toggles the canvas and unlocks the cursor.
- `AimRaycast.FindTarget` casts from the camera forward along `EnemyMask | WorldMask` and returns the hit only
  if it is on the Enemy layer, so a wall closer than the enemy means "no target". Both Mark and Snare share it,
  which is why they cannot disagree about what the crosshair is over.

## Ability implementations as built (2026-10-23, after the S3b rework)

| Ability | Shape | Owner | Asset values | Verified in play mode |
|---|---|---|---|---|
| Rock Q — Slam | instant, component | `SlamDriver` | cd 8, mag 40, radius 5 | apex **3.96 m** above take-off, flight 0.92 s (rise 0.57 s + fall **0.35 s** at 2.5× gravity); enemy 90 → **50 hp** and displaced **2.12 m**; refused while airborne (`slamActive=False`, Q still ready) |
| Rock E — Bulwark | channelled | `BulwarkShield` | cd 0 (component owns), dur 5, mag 0.4 | raises on press (`meter 4.94`), drains **1/s** (3.99 after 1 s), damage 10 → **4**; firing blocked (shots 0 → 0 while held, 0 → 1 after lowering); early release at 2 s left `meter 3.00`, `canRaise=False` for the 0.5 s, refilled to 4.50 by 4.5 s; depletion at 5.34 s → `canRaise=False` at +1/+4/+6.5 s, **true at +8.6 s** |
| Paper Q — Mark | instant, class | `MarkTarget` | cd 10, dur 6, mag 1.35, range 70 | `markOk=True dmgMult=1.35 left=6.0`; bar turns amber `rgb=(1.00,0.72,0.20) st='MARK 5.5 → 4.6'`; `Health.Update` restores 1.00 when the deadline lapses |
| Paper E — Snare | instant, class | `Snare` | cd 12, dur 3, mag 0.25, range 14 | `SNARE ok=True speedMult=0.25 left=2.4`; bar turns blue-grey `rgb=(0.55,0.66,0.78) st='SNARE 2.4'`; HUD chip `'SNARE 2.5s' → '1.5s'` |
| Scissors Q — Climb | **passive**, component | `WallClimbPassive` | no cooldown, mag 6 = cap | climbing **3.0 m/s** (0.08 → 2.19 m in 0.7 s), cap reached at **6.04 m**, mantle onto a 2 m block ended at y **2.05** grounded, exit jump **+1.94 m** above the cap and **4.90 m** out along the wall normal |
| Scissors E — Overclock | instant, class | `Overclock` | cd 12, dur 3, mag 1.8 | fire rate **1.80**, speed **1.25**, still active at 2.8 s, both back to 1.00 after the 3 s window |

Two things the rework removed, which the S3 table used to describe as defects: `PlayerStatus` no longer touches
the controller (no second `CharacterController.Move`, so gravity cannot cancel a climb), and the old climb's
`magnitude`-as-speed experiment is gone with `WallClimb.cs`.

Heroes as built: Rock 200 hp / 4.2 m/s / sprint ×1.35 / body ×1.25 orange; Paper 90 / 5.2 / ×1.6 / ×0.9 pale
blue; Scissors 130 / 6.8 / ×1.7 / ×0.95 green. These match `docs/HERO_ROSTER.md`, including the per-hero aim
multipliers (`aimMoveMultiplier` 0.9 / 0.6 / 0.9) and Paper's `sniperReticle`.

## Drift against `docs/HERO_ROSTER.md` (roster wins; the code is wrong)

What is left is the VFX half of the roster's feedback inventory:

- Mark/Snare/Overclock now have HUD chips, an amber / blue-grey enemy bar and (for the two Paper abilities) a
  countdown above the enemy. Still missing: **Overclock's green bullets/trails and its particles**, and the
  stand-alone body effects (Snare's root, Bulwark's shield tint).
- Slam has no outward particles and Climb has no dust: both are VFX-block work.
- Bullet impacts have no spark yet, and Paper's bullets have no tracer.

Built since this table was written: RMB aiming (FOV 60→42, 8/s, +0.55/−0.20, ×0.9 move, Paper ×0.6, spread
−50 %), the HUD (health, both slots, Bulwark meter, passive label, chips, crosshairs, hit marker) and the
world-space enemy bars.

## Known limitations / deliberately not handled

- No ability charges, no ultimate meter, no cancellation, no aiming prediction for moving targets. A channelled
  Bulwark can be kept up until it empties; there is no manual "cancel" beyond releasing the key.
- The passive slot has no *button*, and the HUD renders it as one: `AbilitySlotUI.Bind` swaps the cooldown bar
  for the `passive` label and the "always on, no cooldown" line when `ShapeOf(slot) == Passive`, and the key hint
  reads `passive` instead of `Q`. The character-select card still prints the definition's `cooldown` (0).
- A climb's budget resets on floor contact, so mantling onto a wall top counts as landing and refunds it; there
  is no notion of "the same wall" and no cooldown of any kind, per the roster.
- Mark and Snare write a deadline instead of restoring a captured value, so re-casting on the same enemy
  refreshes the window; what is not supported is *layering* two vulnerability sources on one enemy
  (see `damage-and-pooling.md`).
- Abilities have no resource cost and no range indicator, and `AbilityDefinition.description` is still read by
  nobody (the select card renders `displayName` + `cooldown`). VFX exist now: climb dust, slam burst, Bulwark
  shell, Overclock's green bullets, and the status rings added on 2026-11-06.
- `MatchBootstrap` no longer owns the whole match flow: scene routing, map choice and the tutorial live in
  `match-flow-and-maps.md`; `MatchBootstrap` only applies a pick to the hero already in the scene.

## 2026-11-06 update — abilities now read on screen, and Overclock starts loaded

- **Mark/Snare are legible:** `VFX/EnemyStatusRings` (pre-cast white ring, amber marked, cyan snared + tether) and a
  marked enemy's health bar now shows at full health. Both effects were already wired into `Health` and
  `EnemyBrain`; what was missing was any cue on the enemy model, which the gate reported as "I don't know when an
  enemy is marked".
- **Overclock refills the magazine when it activates** (`Weapon.RefillMagazine`), because ×1.8 fire rate empties a
  12-round magazine in 0.95 s — the burst now starts loaded (Chloe's decision).
- **Wall climb is 5 m/s with a 0.08–0.4 s mantle** instead of a position teleport at the top; the height cap is
  unchanged. `IsClimbing` is true during the mantle too.
- Abilities do have VFX now (climb dust, slam burst, Bulwark shell, Overclock's green bullets).
