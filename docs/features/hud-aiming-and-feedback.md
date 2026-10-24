# HUD, aiming & feedback

## Status (2026-10-23 22:5x)

**Aiming, the HUD and the enemy health bars are built and measured in play mode.** The VFX half (impact
sparks, tracers, per-ability particles, green Overclock bullets) is **not** built. Numbers and probe lines:
`docs/tasks/2026-10-23_S4-hud-and-feel.md` § *Landed*.

Two rules govern the whole feature and come from Chloe:

1. **UI is authored as prefabs, never built at runtime.** `Prefabs/UI/CharacterSelect.prefab` +
   `CharacterSelectUI`/`CharacterCardUI` are the reference implementation: layout and persistent listener
   live in the prefab, the script only fills widgets and shows/hides.
2. **Every gameplay state must be visible without reading the console.** A demo has to show it.

## Purpose

Give the player read-out and feedback for what the other five systems already compute: health, both ability
cooldowns and the Bulwark meter, the weapon charge, enemy health, the aiming state, and per-ability VFX.

## What already exists to bind to (verified in code)

| Signal | Where | Readiness |
|---|---|---|
| health value + hit/death events | `Health.Current`, `.Normalized`, `.Damaged`, `.Died` | ready |
| ability identity per slot | `PlayerAbilities.Definitions[slot]` (`displayName`, `slotLabel`, `cooldown`) | ready |
| slot shape — instant / channelled / passive | `PlayerAbilities.ShapeOf(slot)` (`AbilityShape`) | ready — the passive slot has to render as "passive", not as a cooldown |
| Bulwark reservoir | `PlayerAbilities.MeterNormalized(slot)` / `BulwarkShield.Meter`, `.Normalized`, `.IsRaised` | ready |
| cooldown ratio — **0 = just used, 1 = ready** | `PlayerAbilities.CooldownRatio(slot)`, `.IsReady(slot)` | ready — bind straight to a fill amount |
| charge progress | `Weapon.ChargeRatio`, `.IsCharging` | ready |
| speed / fire-rate state | `PlayerStatus.SpeedMultiplier`, `.FireRateMultiplier`, `.IncomingDamageMultiplier` | ready |
| enemy "marked" | `Health.IncomingDamageMultiplier > 1` | partial — value only, no remaining time |
| enemy "snared" | `EnemyBrain.SpeedMultiplier < 1` | partial — value only, no remaining time |
| camera to hang a reticle/FOV on | `ThirdPersonCameraRig.Cam` | partial — no aim state exists |

## What exists now (2026-10-23) and what is still missing

- **Aim state** — `ThirdPersonCameraRig` owns it: `aimFov`, `aimBlendPerSecond`, `aimOffset` serialized,
  `IsAiming` / `AimBlend` public, hip FOV taken from the scene camera at `Awake`. `PlayerMotor` applies
  `CharacterDefinition.aimMoveMultiplier` while `InputReader.AimHeld`; `Weapon` halves `spreadDegrees` on the
  same flag (`const AimSpreadMultiplier = 0.5f`).
- **Hit signal** — no `Weapon` event was needed. `EnemyBarBinder` re-raises `Health.Damaged` for every bound
  enemy and `HudController` flashes the hit marker when `DamageInfo.Source` is the hero or a child of it.
- **Timed effect handles** — Mark now lives on `Health.ApplyDamageMultiplier(magnitude, duration)` with
  `IsMarked` / `DamageMultiplierSecondsLeft`; Snare mirrors it on `EnemyBrain.ApplySpeedMultiplier`. Both are
  deadlines, so a re-cast refreshes instead of restoring early, and a chip can count down.
- **Bulwark reservoir** — already existed (`BulwarkShield.Meter`/`Normalized`), read through
  `PlayerAbilities.MeterNormalized(slot)`.
- **HUD** — authored `Prefabs/UI/HUD.prefab`, driven by `UI/HudController.cs` (reads `HeroLoadout`, pushes into
  `UI/AbilitySlotUI.cs` widgets). Instanced in the arena scene with `hero`, `cameraRig` and `enemyBars` wired
  as scene refs; the prefab itself holds the widget refs.
- **Enemy bars** — `Prefabs/UI/EnemyHealthBar.prefab` (world-space canvas, 0.01 scale) + `UI/EnemyHealthBar.cs`
  (`IPooled`, billboards, hides at full health, hides behind `blockerMask` = 9, tints amber/blue-grey),
  `UI/EnemyHealthBarPool.cs`, `UI/EnemyBarBinder.cs` on the scene object `EnemyBars` (4 enemies serialized,
  `FindObjectsByType` fallback).
- **Still missing** — the whole VFX half: `Prefabs/VFX/` is empty, there is no VFX pool, so impact sparks,
  Paper's tracer, green Overclock bullets and the per-ability particles do not exist. Also missing: the
  Scissors fire-rate-window readout (only Paper's charge bar is drawn), and the minimap / Tab frag board stay
  deferred with the reasons below.

## Interface (as built)

```csharp
class HudController : MonoBehaviour { /* hero, cameraRig, enemyBars + authored widget refs; pushed per frame */ }
class AbilitySlotUI : MonoBehaviour { void Bind(AbilityDefinition, AbilityShape); void Push(float ratio, float meter, bool ready); }
class EnemyBarBinder : MonoBehaviour { float MarkSecondsLeft; float SnareSecondsLeft; event Action<DamageInfo> EnemyDamaged; }
class EnemyHealthBar : MonoBehaviour, IPooled { void Bind(Health, EnemyBrain); }
class ThirdPersonCameraRig { float AimBlend; bool IsAiming; }
class Health { void ApplyDamageMultiplier(float magnitude, float duration); bool IsMarked; float DamageMultiplierSecondsLeft; }
class EnemyBrain { void ApplySpeedMultiplier(float magnitude, float duration); bool IsSnared; float SpeedMultiplierSecondsLeft; }
```

## Depends on

`damage-and-pooling.md` (health + pooling rules), `player-movement-and-camera.md` (rig, status, input),
`weapons-and-projectiles.md` (charge, hit source), `enemy-fsm.md` (per-enemy state), `characters-and-abilities.md`
(ability definitions and cooldown ratios).

## Depended on by

Nothing yet.

## Behavior notes for the build (constraints that will bite)

- `grep -rn "Instantiate(" Assets/_Project/Scripts` must stay at one hit (`Core/ObjectPool.cs`) — a HUD that
  creates widgets, bars or VFX at runtime fails the graded pooling claim as well as the UI rule.
- Values are **pushed**, not polled by the widgets: widgets hold no `Update` and no `FindObjectOfType`.
  Anything that needs a pull should be filled by `HudController` in one pass.
- The HUD must survive the hero being deactivated: `MatchBootstrap.BeginSelection` turns the hero GameObject
  off and only `Select` turns it back on. A HUD bound in `Start` will be bound to an inactive object.
- `AddComponent` leaves serialized references null (this already cost one debugging session on `PlayerCombat`):
  authored prefab wiring is what makes these references exist at all.
- Enemy bars sit in world space: they must not be visible through walls (the roster's "never through walls"
  applies to aim aids and markers) and must not draw when the enemy is at full health.
- Overclock's bullets/trails turn **green** while active; the projectile prefab is shared between player and
  enemies, so the tint must be per-launch state, not a material edit.

## Known limitations / deliberately not handled

- Minimap and Tab frag board are out of this block (ledger, 2026-10-23) — they need systems that do not exist.
- No settings/pause menu, no key rebinding, no damage numbers, no localisation; the HUD is English-only like
  the rest of the UI copy.
- Screen-space UI uses uGUI `Text`, not TextMeshPro (not in `manifest.json`), so text shaping is basic.
