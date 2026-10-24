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

public enum StatusEffect { Speed, FireRate, DamageTaken }   // top-level, namespace HeroShooter.Player

class PlayerStatus : MonoBehaviour {          // multiplies; never moves the controller
    void AddEffect(StatusEffect kind, float duration, float magnitude);
    void BeginChannel(StatusEffect kind, float magnitude);   // lasts until EndChannel, for Bulwark
    void EndChannel(StatusEffect kind);
    void ApplyFiringSlow(float multiplier, float duration);
    float SpeedMultiplier { get; }       // rebuilt 0..n each Update from effects + channels
    float FireRateMultiplier { get; }
    float IncomingDamageMultiplier { get; }   // what it pushes into Health
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
  `duration <= 0` still lives until the next frame. **Channels** (`BeginChannel` / `EndChannel`) are the other
  half: they have no clock and last until the ability ends them — that is how Bulwark's ×0.4 / ×0.75 hold while
  the key is down.
- **One mover.** `PlayerMotor.MovementSuspended` makes the motor's `Update` return before it touches the
  controller, so exactly one component moves the hero: the motor normally, `SlamDriver` during Rise/Fall,
  `WallClimbPassive` while climbing. `PlayerStatus` no longer moves anything (the old `Climb` effect did).
- `Launch(velocity)` hands the motor a velocity and clears the suspension, and the impulse keeps its horizontal
  momentum until the hero lands or the player steers: without that rule ground acceleration killed the impulse in
  one frame and the wall climb's exit jump travelled **0.11 m** instead of 4.90 m.

## Known limitations / deliberately not handled

- **Wall traversal is a component, not a motor mode.** `WallClimbPassive` (Scissors only) suspends the motor,
  probes the Structure layer, and owns the controller while climbing — see `characters-and-abilities.md`. The
  motor knows nothing about walls.
- **RMB does nothing yet.** `InputReader.AimHeld` exists and RMB no longer fires an ability, but no camera or
  weapon code reads it — that is the aim block.
- No crouch, no ground/air control distinction, no wall-run, no slope-slide tuning.
- Death is unhandled: when the player's `Health` reaches 0 the controller keeps accepting input.
