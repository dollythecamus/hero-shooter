# S3b — ability rework (Chloe's spec, 2026-10-23)

Source of truth for behaviour: `docs/HERO_ROSTER.md`. This task file is the implementation plan and the list of
what must change from the S3 code.

## What changes, ability by ability

| Ability | S3 behaviour (wrong) | Target behaviour | Files |
|---|---|---|---|
| Rock Slam | instant AoE at the feet, knockback broken | jump up → forced fast descent → landing AoE + knockback + outward particles | `GroundSlam.cs` → becomes a 3-phase ability; needs vertical movement ownership |
| Rock Bulwark | timed buff (4 s, ×0.4 damage) | **hold to channel**, 5 s meter draining, longer cooldown on depletion, refill 1/s after a short delay on release | `Bulwark.cs` + a new channelled-ability path in `PlayerAbilities.cs` |
| Paper Mark | works, no feedback | same numbers + world marker + HUD chip with remaining time | `MarkTarget.cs`, HUD |
| Paper Snare | works, no feedback | same numbers + visible snared state + HUD chip | `Snare.cs`, HUD, enemy bar tint |
| Scissors Climb | button, 8 s cooldown, arbitrary vertical boost | **passive**: hold Space while touching and facing a wall, climb to a max height, jump out, reset on floor contact, no cooldown | `WallClimb.cs` → wall-detection + climb state in `PlayerMotor`/`PlayerStatus` |
| Scissors Overclock | works, no feedback | same numbers + particles, trails, coloured bullets | `Overclock.cs`, projectile tint |

## Structural consequences

1. **Abilities need two activation shapes**: instant (`IAbility.Activate`) and **held/channelled**
   (activate on press, tick while held, release on up). `PlayerAbilities` must own that lifecycle, and
   `PlayerInput` (the reader) must expose press/hold/release for Q and E separately — today it only exposes
   `wasPressedThisFrame`.
2. **Slam owns vertical motion**, so either it drives `CharacterController` directly with the motor told to
   stand down, or the motor gains an external-motion channel. Pick one and write it down; do not have two
   things calling `CharacterController.Move`.
3. **Climb is no longer an ability at all** — it is passive traversal. `CharacterDefinition` should therefore
   allow a passive movement trait per character instead of assuming two active abilities. Options: keep two
   slots and mark slot Q as passive (HUD shows "passive"), or add a `MovementTrait` enum. Recommended first
   option: no new asset type, and the assignment only requires "2 habilidades", not "two buttons".
4. **Knockback must not fight the NavMeshAgent.** Use `agent.isStopped` + `agent.updatePosition` handling, or
   move the body with a short-lived impulse that the brain respects for its duration (a `KnockbackState` would
   also make the FSM explanation richer). Do not use `agent.Move` per frame while states call `SetDestination`.
5. **Bulwark's meter is a first-class resource**, not a status effect: it persists between fights, refills,
   and gates activation. Model it in a `BulwarkMeter` component owned by the player, exposed to the HUD.

## Planned implementation (design only — **nothing here is built**)

Written 2026-10-23 so the next execution pass is mechanical. All values come from `docs/HERO_ROSTER.md`.

### Three ability shapes

`AbilityDefinition.kind` decides which shape a slot is; `PlayerAbilities` is the only router.

| Shape | Slots | Owner | Lifecycle |
|---|---|---|---|
| **instant** | Slam, Mark, Snare, Overclock | the ability class (`IAbility.Activate`) | press → activate → cooldown clocked by `PlayerAbilities` |
| **channelled** | Bulwark | `BulwarkShield` component | press → begin; release → end; the component owns the reservoir *and* both cooldowns |
| **passive** | Wall Climb | `WallClimbPassive` component | no button, no cooldown; the HUD labels the slot "passive" |

This keeps the assignment's "2 abilities per character" intact without pretending a passive traversal trait is a
button.

### Files to add / change

| File | Change |
|---|---|
| `Player/InputReader.cs` | add `Ability1Held/Released`, `Ability2Held/Released`, `AimHeld`, `JumpHeld` (today only `wasPressedThisFrame` exists) |
| `Player/PlayerAbilities.cs` | route press/hold/release per slot; expose `CooldownRatio` and `MeterNormalized` for the HUD; mark passive/channelled slots |
| `Player/PlayerStatus.cs` | keep timed effects, add **held** multipliers for channelled abilities (a dictionary cleared when the channel ends) |
| `Player/BulwarkShield.cs` | **new**: 5 s reservoir, ×0.4 damage, ×0.75 speed, 0.5 s short / 8 s depleted cooldowns, refill 1/s after 1 s, blocks firing |
| `Player/WallClimbPassive.cs` | **new**: Structure wall probe, 3 m/s climb, 6 m cap, exit jump wall-normal + up, mantle over a wall top below the cap, reset on floor |
| `Player/SlamDriver.cs` | **new**: Rise → Fall → Impact phases, 4 m jump, 2.5× gravity descent, 5 m / 40 damage AoE + knockback + outward particles; refused while airborne |
| `Player/PlayerMotor.cs` | `MovementSuspended` + `Launch(velocity)` so exactly one component moves the controller at a time |
| `Player/PlayerCombat.cs` | refuse to fire while the shield is up (drop any charge) |
| `Weapons/Weapon.cs` | `CancelCharge()`; spread ×0.5 while aiming |
| `Player/ThirdPersonCameraRig.cs` | aim state: FOV 60→42, offset +0.55 right / −0.20 down, blend 8/s |
| `Enemies/EnemyBrain.cs` | knockback owns the body: `Push` clears the path and stops the agent, `MoveTo` returns early while knocked back |

### The one-mover rule

Two components must never call `CharacterController.Move` in the same frame. Slam and the wall climb therefore
set `PlayerMotor.MovementSuspended = true`, drive the controller themselves, and release it when they finish.
That rule is also what made the earlier knockback bug possible on the enemy side: the FSM re-pathed every frame
and cancelled the impulse.

### Verification per ability (the gate)

| Ability | What must be measured |
|---|---|
| Slam | apex ≈ 4 m, descent faster than a free fall, 40 damage inside 5 m, enemy body actually displaced, particles outward |
| Bulwark | reservoir drains 1 unit/s while held, ×0.4 damage observed, firing blocked, early release leaves the remainder and refills, depletion gives 8 s |
| Mark | target `IncomingDamageMultiplier == 1.35` for 6 s, marker + HUD chip visible, no through-wall marker |
| Snare | target `SpeedMultiplier == 0.25` for 3 s, same feedback |
| Climb | climbs at 3 m/s to 6 m, jumping out at the cap, mantle over a 2 m wall, reset only on floor |
| Overclock | fire rate ×1.8, speed ×1.25, green bullets/trails for exactly 3 s |

### History — a head start that was thrown away

An implementation pass was started on 2026-10-23 around 18:10 and **reverted at Chloe's instruction before any
compile verification**. It touched `InputReader`, `PlayerStatus`, `PlayerAbilities`, `PlayerCombat`,
`PlayerMotor`, `Weapon`, `AbilityFactory`, `GroundSlam`, `EnemyBrain`, deleted `Bulwark.cs` and `WallClimb.cs`,
and added `BulwarkShield.cs` + `WallClimbPassive.cs`. None of it survives; the tree is exactly the docs commit
`dced7ba`. Do not assume any of the above table is already applied.



## Gates

- Slam: Rock visibly rises, comes down faster than gravity, lands with damage + **knockback that moves the
  enemy body**, particles outward.
- Bulwark: holding E drains 5 s of meter, blocks damage while held; releasing at 2 s leaves the meter at 2 and
  it refills 1/s after the delay; depleting it gives the longer cooldown; the HUD meter matches the code value.
- Climb: holding Space against a wall climbs to the cap and jumps out; releasing drops; the allowance resets
  only on floor contact; no cooldown anywhere.
- Mark / Snare: the marked/snared enemy is identifiable on screen and in the HUD for exactly the timed window.
- Overclock: bullets change colour and trails appear for exactly the 3 s window.

## Open design questions

Collected in `docs/PROJECT_LEDGER.md` → *Open questions*, asked 2026-10-23. Implementation does not start on a
row whose numbers are unknown; the answers get written into `docs/HERO_ROSTER.md` first.
