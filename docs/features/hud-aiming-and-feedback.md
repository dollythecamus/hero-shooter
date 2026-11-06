# HUD, aiming & feedback

> **Currency note (2026-11-06):** this document describes the system as designed and built on 2026-10-23/24. Everything that changed afterwards is listed in the *2026-11-06 update* at the bottom; where the two disagree, the update wins and `docs/PROJECT_LEDGER.md` has the evidence.

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
- **VFX** (built 2026-10-24) — one pooled burst prefab (`Prefabs/VFX/Blast.prefab`) driven by
  `Scripts/VFX/VfxOneShot.cs` + `VfxPool.cs`, per-hit sparks from `Projectile.Impact`, Paper's tracer and
  Overclock's green through the projectile's `TrailRenderer` + a material swap, a landing burst from
  `SlamDriver`, climb dust from `WallClimbPassive`, and the Bulwark shell (`BulwarkShieldVfx`). Details below.
- **Built after this list was written (2026-11-06)** — the minimap, the Tab frag board, the ammunition readout and
  the fire-rate/reload bar all exist now; see *2026-11-06 update* at the bottom of this file. Nothing on the HUD is
  deferred any more.

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

## VFX (built 2026-10-24)

One pooled prefab covers the whole inventory: `Prefabs/VFX/Blast.prefab` — a non-looping `ParticleSystem`
(0.4 s, one 12-particle sphere burst, gravity 0.6, colour-over-lifetime fade, additive `M_Vfx` on the generated
radial `T_Spark`) plus `VfxOneShot`, which plays it, takes a per-burst colour and returns itself after 0.7 s.
`VfxPool` (`ObjectPool<VfxOneShot>`, 24 warm-up, static `Instance`) is the only way to spawn; `VfxPool.Spawn`
does nothing when there is no pool, so VFX can never break a shot. The scene object `Vfx` carries the pool.

| Call site | Burst |
|---|---|
| `Projectile.Impact` | every hit, walls included, at the contact point oriented by the normal; scale `impactBurstScale` (1), colour `impactColor` |
| `SlamDriver.Impact` | at the landing point, scale `radius × 0.45` (2.25 for the 5 m slam), orange |
| `WallClimbPassive.Climb` | dust every `dustInterval` (0.22 s) while climbing |
| `WallClimbPassive.Stop` / `ExitJump` | one puff at the end of a climb |

Trails are **per launch, not per asset**: `Projectile.Launch(...)` takes a `trailColor` and an `overclocked`
flag, sets `TrailRenderer.emitting`/`startColor` and swaps `body.sharedMaterial` between `M_Projectile` and
`M_Projectile_Overclock`. `Weapon.Fire` decides: pale-blue `TracerColor` when the weapon `RequiresCharge`
(Paper), green `OverclockGreen` while `PlayerStatus.HasEffect(StatusEffect.Overclock)`, nothing otherwise. The
trail is 0.15 s and 0.06 wide, so it reads as a streak rather than a wire, and `Clear()` on launch stops the
previous shot's segment bleeding into the next.

`BulwarkShieldVfx` sits on the authored `ShieldVfx` sphere inside `Player.prefab`: hidden by default, shown only
while `BulwarkShield.IsRaised`, scaled `Lerp(0.96, 1.6, meter)` and tinted through a `MaterialPropertyBlock`
(`_BaseColor`) so the shell fades with the reservoir without touching the shared material.

Measured in play mode (2026-10-24, probe lines in the S4 task file): pool `created=24` at start and `24` after
every burst; Rock hip fire `emitting=False trailCol=(0,0,0,0) body='M_Projectile'`; Paper
`emitting=True trailCol=(0.60,0.92,1.00,0.55)`; Overclock
`trailCol=(0.30,1.00,0.40,0.65) body='M_Projectile_Overclock'` with `HasEffect(Overclock)=True`; slam burst
`[scale=2.25 playing=True]` at the hero's feet; Bulwark shell `enabled=False → True (scale 1.43, meter 0.72) →
False` on release; `Stop()` 0 → 1 burst and one `Climb()` step 1 → 2.

**Not measured:** the dust stream during a real held-key climb — the editor cannot hold a key across frames
(`InputState.Change` is overwritten by the real device each input update, and queued `KeyboardState` events are
not enough), so the two dust call sites were invoked directly instead. The climb itself was measured earlier
(see the S3b gate table).

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

- Minimap and Tab frag board shipped (2026-11-06 update below).
- The pause menu with sensitivity/invert settings and the floating damage numbers exist now. Still true: no key
  rebinding, no localisation, English-only copy, and uGUI `Text` rather than TextMeshPro.
- Screen-space UI uses uGUI `Text`, not TextMeshPro (not in `manifest.json`), so text shaping is basic.

## 2026-11-06 update — minimap dots, status rings, ammo, and the sprite that was smearing

- **Minimap enemy dots:** `UI/MinimapBlips` draws one dot per living enemy in the map's own scale (world offset ÷
  `MinimapCamera.HalfExtent`), pinned to the rim when off-map. The list comes from `EnemyBarBinder.Enemies`, the
  same list the bars use.
- **Ability feedback on the enemy, not only in the corner:** `VFX/EnemyStatusRings` draws a white ring on the target
  a ready Mark would take (aiming, off cooldown, same line-of-sight rule as the cast), amber while marked, cyan plus
  a tether while snared. `EnemyHealthBar` no longer hides a **marked** enemy at full health.
- **Ammunition readout** (`AmmoText`: `6 / 6`, `RELOADING`) and a **fire-rate/reload bar** under the crosshair.
- **The smear was a sprite, not layout:** every fill and thin bar used Unity's built-in `Background` sprite (10 px
  rounded border) at `Simple`, so it stretched along a 420×24 bar. All 35 fills/bars/marks now use the borderless
  `Art/UI/White.png`; the five rounded panels keep the sliced sprite.
- **Pause panel** gained a **Main Menu** entry (Resume / Main Menu / Quit), so any scene has a way out.
- Paper's reticle was rebuilt as a four-post gap cross with three mildots per arm, not a dot.
