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

## Order of work

1. Input: press/hold/release for Q and E (`InputReader` + `PlayerAbilities` lifecycle).
2. Bulwark meter (self-contained, testable in isolation).
3. Slam three-phase (depends on the vertical-motion decision).
4. Climb passive (depends on wall detection; test on the arena walls).
5. Mark / Snare feedback hooks (they already work; only visuals and durations to display).
6. Overclock visuals (cheapest, do it while testing 1–5).

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
