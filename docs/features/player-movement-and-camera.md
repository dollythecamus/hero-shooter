# Player movement & camera

## Purpose

Everything the player character does that is not shooting or abilities: reading hardware, moving a
`CharacterController` relative to the camera, and orbiting a third-person camera that never clips into
geometry. `PlayerStatus` is the single sink where timed modifiers (firing slowdown, Overclock, Bulwark, climb)
become multipliers, so abilities and weapons never poke at the motor directly.

## Interface

```csharp
static class InputReader {                    // the only file allowed to touch Keyboard/Mouse/Gamepad
    Vector2 Move;            // WASD + left stick, NOT normalised (diagonals exceed length 1)
    Vector2 Look;            // mouse delta in pixels + right stick * 12
    bool FireHeld, FirePressed;              // LMB
    bool SprintHeld;                         // LShift / L3
    bool JumpPressed;                        // Space / buttonSouth
    bool Ability1Pressed;                    // Q  OR  RMB  ← see the RMB collision below
    bool Ability2Pressed;                    // E  OR  MMB
    bool FragsHeld;                          // Tab
    bool PausePressed;                       // Esc
    static void SetCursorLocked(bool locked);
}

[RequireComponent(typeof(CharacterController))]
class PlayerMotor : MonoBehaviour {
    // serialized: walkSpeed 6, sprintMultiplier 1.6, acceleration 45, rotationSpeed 14,
    //             jumpHeight 1.4, gravity -25, cameraTransform, status
    bool IsSprinting { get; }
    float PlanarSpeed { get; }
    void Configure(float newWalkSpeed, float newSprintMultiplier);   // called by HeroLoadout
}

class ThirdPersonCameraRig : MonoBehaviour {
    // serialized: target, pivotOffset (0,1.6,0), distance 4.5, mouseSensitivity 0.12,
    //             pitchLimits (-35,70), followSharpness 20, collisionMask, collisionRadius 0.25, lockCursor
    Camera Cam { get; }        // resolved in Awake with GetComponent<Camera>()
}

public enum StatusEffect { Speed, FireRate, DamageTaken, Climb }   // top-level, namespace HeroShooter.Player

[RequireComponent(typeof(CharacterController))]
class PlayerStatus : MonoBehaviour {
    void AddEffect(StatusEffect kind, float duration, float magnitude);
    void ApplyFiringSlow(float multiplier, float duration);
    bool IsClimbing { get; }
    float SpeedMultiplier { get; }       // rebuild0..n each Update from live effects
    float FireRateMultiplier { get; }
}
```

## Depends on

- `damage-and-pooling.md` — `PlayerStatus` writes `Health.IncomingDamageMultiplier` every frame.

## Depended on by

- `weapons-and-projectiles.md` — `Weapon` multiplies `definition.shotsPerSecond` by
  `status.FireRateMultiplier` and applies `ApplyFiringSlow` when `firingSpeedMultiplier < 1`.
- `characters-and-abilities.md` — `HeroLoadout` calls `PlayerMotor.Configure` and
  `PlayerAbilities.Configure` (which reads `Camera.main` for the ability context); four of the six abilities
  are expressed purely as `PlayerStatus.AddEffect` calls; `MatchBootstrap` disables the rig and the hero
  while the select screen is up.
- `hud-aiming-and-feedback.md` — the HUD is meant to read `Weapon.ChargeRatio`, `PlayerAbilities.CooldownRatio`
  and `PlayerStatus`; RMB aiming will change this file (FOV, shoulder offset, aim state, crosshair).

## Behavior notes

- `InputReader` reads state at call time. There is no cached frame state and therefore no dependency on
  script execution order — but also no input buffering: a press that happens between two `Update` calls in the
  same frame is visible in both.
- Movement is camera-relative: the desired direction is built from `cameraTransform.forward/right` flattened
  and renormalised, then clamped to length 1 when the input is diagonal. If `cameraTransform` is null the
  motor falls back to its own transform axes, which is wrong for a third-person camera — keep the reference set.
- Vertical is separate: `_verticalVelocity` carries gravity and jump, planar velocity is `MoveTowards`'d at
  `acceleration`, and the two are combined into one `CharacterController.Move` per `Update`. While grounded
  **and moving downward** the vertical velocity is pinned to `-2` (a bias that keeps the controller glued to
  slopes and stairs); an upward velocity while grounded is left untouched.
- Jump impulse is `Mathf.Sqrt(jumpHeight * -2 * gravity)` — it follows from the gravity field, so changing
  `gravity` changes jump height unless `jumpHeight` is compensated.
- Body rotation only turns toward the move direction when `desired.sqrMagnitude > 0.01`, i.e. input length
  above **0.1** — standing still leaves the last facing. There is no strafe/orbit mode; the aim work needs one.
- `ThirdPersonCameraRig` runs in `LateUpdate`: yaw/pitch from `InputReader.Look`, pitch clamped to
  `pitchLimits`, position exponentially smoothed with `1 - exp(-followSharpness * dt)`, and a sphere-cast
  pull-in along the boom that never comes closer than 0.2 m. `collisionMask` must contain the Structure layer
  (value 8) or the camera walks through walls.
- `PlayerStatus` recomputes `SpeedMultiplier` / `FireRateMultiplier` / `DamageTaken` from scratch every
  `Update` by multiplying all live effects, then multiplies in the firing slow. Two slows stack
  multiplicatively (0.75 × 0.45 = 0.34) — this is intended, and the reason no ability may write the motor.
- Effects are stored with an absolute end time and removed lazily on the next `Update`; an effect with
  `duration <= 0` still lives until the next frame.
- `Climb` is the **one effect whose magnitude is a speed (m/s), not a multiplier**: the case reads
  `climb = magnitude`, falling back to the serialized `climbSpeed` (7) only when the ability passes a
  non-positive value. Every other kind multiplies.

## Known limitations / deliberately not handled

- **Two movers (violates the one-mover rule).** `PlayerStatus.Update` calls `_controller.Move` directly for
  the `Climb` effect, while `PlayerMotor.Update` also calls it in the same frame. The result is a climb that
  fights gravity and rotation. The ability rework (`docs/tasks/2026-10-23_S3b-ability-rework.md`) replaces
  this: `SlamDriver` / `WallClimbPassive` drive the controller and suspend `PlayerMotor`.
- **Climb is not wall traversal.** Today it is a 0.9 s vertical boost launched from anywhere at the
  definition's `magnitude` (6 m/s), with a 3 s cooldown. The roster specifies a passive wall climb: 6 m cap,
  3 m/s, exit jump on cap, mantle over a lower wall top, reset only on floor contact.
- **RMB is overloaded.** `Ability1Pressed` returns true for Q **and** right mouse button, while the roster
  makes RMB the aim button for all three heroes. The rework must remove `rightButton` from `Ability1Pressed`
  before aiming lands, or RMB will fire an ability and aim at once.
- No aim state, no FOV change, no shoulder offset, no crouch, no ground/air control distinction, no wall-run.
- `PlayerMotor` exposes no way to suspend itself, which is why the rework's movers need one.
- Death is unhandled: when the player's `Health` reaches 0 the controller keeps accepting input.
