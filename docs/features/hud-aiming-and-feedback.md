# HUD, aiming & feedback

## Design note (not built)

**Status: planned, zero code.** This file exists so the seams are named before anyone writes the HUD. The
specification is `docs/tasks/2026-10-23_S4-hud-and-feel.md`; the design decisions (numbers) are the tables in
`docs/HERO_ROSTER.md`. Nothing below describes existing behaviour.

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
| cooldown ratio (1 = ready) | `PlayerAbilities.CooldownRatio(slot)`, `.IsReady(slot)` | ready |
| charge progress | `Weapon.ChargeRatio`, `.IsCharging` | ready |
| speed / fire-rate state | `PlayerStatus.SpeedMultiplier`, `.FireRateMultiplier`, `.IsClimbing` | ready |
| enemy "marked" | `Health.IncomingDamageMultiplier > 1` | partial — value only, no remaining time |
| enemy "snared" | `EnemyBrain.SpeedMultiplier < 1` | partial — value only, no remaining time |
| camera to hang a reticle/FOV on | `ThirdPersonCameraRig.Cam` | partial — no aim state exists |

## What must be added (do not assume it exists)

- **An aim state**: `ThirdPersonCameraRig` has no FOV, shoulder-offset or aim mode, and `InputReader` has no
  aim input — RMB is currently consumed by `Ability1Pressed`. Aiming therefore requires, in this order:
  remove `rightButton` from `Ability1Pressed`, add `AimHeld` to `InputReader`, add FOV/offset/blend + aim flag
  to the rig.
- **A hit signal**: nothing tells the HUD that *the player* landed a shot. `Projectile.Impact` delivers
  `DamageInfo` to the target and returns; `DamageInfo.Source` is the firing `GameObject`, so a hit marker can
  be derived by comparing `Source` to the hero, but an explicit event on `Weapon` is the cheaper contract.
- **Timed effect handles**: Mark and Snare write a raw multiplier and restore it from a coroutine. A HUD chip
  that counts down seconds needs the remaining time exposed (an effect handle with an end time), not a bool.
- **A Bulwark reservoir**: the channelled 5 s meter does not exist. Today Bulwark is a 4 s status effect with
  no resource, so there is nothing to draw a meter from.
- **Enemy health bars**: authored prefab + pool + per-enemy bind; hidden at full health; amber when marked,
  blue-grey when snared. `Enemies/` prefabs carry `Health`, so the value exists; the bar prefab and the pool
  do not.
- **VFX pool**: `Prefabs/VFX/` is empty and there is no VFX pool. Impact sparks, ability particles and Paper's
  tracer must go through the same no-runtime-`Instantiate` rule as bullets, i.e. a pooled prefab per effect.
- **Minimap and Tab frag board**: deferred by the ledger — a minimap needs a dedicated layer/culling mask and a
  second camera + `RenderTexture`; the frag board needs a `ScoreService` that does not exist and no kill events
  are produced anywhere yet.

## Interface (proposed — settle before building)

```csharp
class HudController : MonoBehaviour {       // one per scene, references the authored HUD.prefab widgets
    void Bind(Health health, PlayerAbilities abilities, Weapon weapon, PlayerStatus status);
}
static class InputReader { bool AimHeld; }                  // RMB, replaces its ability binding
class ThirdPersonCameraRig { bool IsAiming { get; } void SetAim(float fov, Vector3 offset, float blendPerSecond); }
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
