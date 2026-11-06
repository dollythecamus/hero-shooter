# Hero roster — rock, paper, scissors

Design owner: Chloe. First version 2026-10-23, **revised the same day** with detailed ability behaviour, the
aiming model and the visual-feedback requirements. Where this file and the code disagree, this file wins and
the code is wrong.

Reference: Team Fortress 2 — three classes, hard counters, readable silhouettes, no role overlap. The point of
the design is that a balanced build produces a visible circle rather than one strongest hero.

## The circle

```
        Rock  ──beats──►  Scissors
          ▲                   │
          │                   │
       beats                 beats
          │                   │
          └───── Paper ◄──────┘
```

- **Paper beats Rock** — out-ranges a slow short-range tank; charged shots punish a big health pool.
- **Scissors beats Paper** — closes distance fast, where a charging sniper cannot fight back.
- **Rock beats Scissors** — survives a skirmisher's burst, and the slam punishes anyone who hugs.

## Global mechanics

### Aiming (all three characters) — added 2026-10-23

- **RMB = aim**, for every character. No exception, no per-character variant of the input.
- While aiming: **camera FOV pulls in** (tighter), the camera takes a **slight shoulder offset**, and an
  **aiming crosshair** replaces the hip crosshair.
- Paper's aim crosshair is a **sniper reticle** (fine centre, thin lines), different from the other two.

Decided 2026-10-23:

| Value | Decision |
|---|---|
| Hip FOV → aim FOV | **60 → 42** |
| Blend speed | **8 / s** (exponential) |
| Shoulder offset | **+0.55 right, −0.20 down** |
| Movement while aiming | **all heroes ×0.9**, Paper **×0.6** |
| Spread while aiming | **−50 % for all three** |
| Through-wall visibility | **never** — aim aids never show anything behind geometry |


### Visual feedback inventory — added 2026-10-23

| Where | What is required |
|---|---|
| Bullet impact | impact flash/spark on every hit, plus a HUD hit marker when the player lands one |
| Enemy health | **world-space bar above each enemy, visible only while below max health**; **amber when marked**, **blue-grey when snared** |
| Abilities | particle effects per ability (each entry below says what), plus HUD cooldown/meter feedback |
| Paper bullets | **thin bright trail from muzzle to impact, ~0.15 s fade**, tinted per state |
| Overclock | particles, trails and **green** bullets while active |
| Mark / Snare | marker above the affected enemy + HUD chip with remaining seconds, **never through walls** |
| HUD | ability cooldowns and meters (Bulwark's meter, and any other metered resource) |


## Rock — the tank

| | |
|---|---|
| Health | 200 |
| Move speed | 4.2 (slowest) |
| Body | largest capsule, warm orange |
| Weapon | heavy short-range slug: 32 damage, 1.1 shots/s, slow projectile, effective range ~13 m |
| Signature | **slows to 45 % speed while firing** — committing to a shot costs mobility |

### Q — Slam (reworked)

1. On activation Rock **jumps high**.
2. He then **slams back down faster than gravity** would take him.
3. On landing: **damage + knockback in a radius** around the impact point.
4. **Particle effect fired outward** from the impact ring.

Known defect from playtest (2026-10-23): **knockback does not move enemies.** Current code adds a decaying
vector through `NavMeshAgent.Move`, which the agent's next `SetDestination` cancels every frame. Fix is part
of the rework.

Decided 2026-10-23:

| Value | Decision |
|---|---|
| Jump height | **4 m** |
| Descent | **2.5× gravity** (forced, faster than a fall) |
| Radius | **5 m** |
| Damage | **40** |
| Cooldown | **8 s** |
| Steering during the jump | **none** — the jump is committed |
| Start mid-air | **not allowed** — Slam requires being grounded |


### E — Bulwark (reworked: channelled shield, not a timed buff)

1. **Hold E** to raise a shield.
2. While held: **damage taken is reduced** and a **meter drains**, total capacity **5 seconds**.
3. Meter **hits 0 while held** → shield drops and the refresh cooldown is **longer**.
4. **Released before 0** → meter **refills at 1 per second** after a short delay.
5. Cannot be re-activated during the short cooldown after a release.

Decided 2026-10-23:

| Value | Decision |
|---|---|
| Meter capacity | **5 s** |
| Damage taken while shielded | **×0.4** |
| Shooting while shielded | **not allowed** |
| Movement while shielded | **×0.75** |
| Short cooldown (released before 0) | **0.5 s** |
| Longer cooldown (drained to 0) | **8 s** |
| Refill | starts **1 s** after release, **1 per second** |


## Paper — the sniper

| | |
|---|---|
| Health | 90 (smallest) |
| Move speed | 5.2 |
| Body | slimmest capsule, pale blue |
| Weapon | **charge on aim**: 15 → 65 damage over 1.2 s, projectile speed 90 → 160, **bullet trail** |
| Signature | no movement ability at all — the charge is the whole kit; **sniper aim reticle** |

### Q — Mark (kept, needs feedback)

The enemy under the crosshair **takes 35 % more damage for 6 s** (range 70, cooldown 10 s). Feedback (decided
2026-10-23): **world marker above that enemy + HUD chip with the remaining seconds**, never visible through
walls.

### E — Snare (kept, needs feedback)

First enemy under the crosshair is **slowed to 25 % for 3 s** (range 14, cooldown 12 s). Feedback (decided
2026-10-23): same treatment — marker above the snared enemy + HUD chip with the remaining seconds, never
through walls.

## Scissors — the skirmisher

| | |
|---|---|
| Health | 130 |
| Move speed | 6.8 (fastest) |
| Body | slim capsule, green |
| Weapon | fast medium gun: 11 damage, 7 shots/s, 85 projectile speed |

### Q — Wall Climb (reworked: passive, no cooldown)

1. **Passive** — not a button ability with a cooldown.
2. **Touching a wall and facing it, holding jump (Space)** moves Scissors **up the wall** at a climb speed,
   up to a **maximum height**.
3. At the maximum height he **jumps out** — an outward/upward impulse, not a straight drop.
4. **Releasing Space** drops off the wall.
5. The climb **resets when Scissors touches a floor** — the height allowance re-arms only on the ground.

Known defect: the current version is an arbitrary vertical boost from a button press, which is not this design.

Decided 2026-10-23:

| Value | Decision |
|---|---|
| Maximum climb | **6 m** |
| Climb speed | **3 m/s** |
| Exit jump | **wall-normal + 2 m up** |
| Cost | **none** (no cooldown, no resource) |
| Reset | only on floor contact |

**Mantle rule:** if the climb reaches the **top of the wall below the 6 m cap**, Scissors does **not** jump off —
he keeps climbing over the top so he ends standing above the wall. The exit jump only happens when the cap is
reached while the wall is still above him.


### E — Overclock (kept, needs feedback)

+80 % fire rate and +25 % move speed for 3 s (12 s cooldown). Feedback (decided 2026-10-23): **particles,
trails and green bullets** while active so the state is readable in a fight.

## As built — 2026-11-06 (where the code ended up against this roster)

The rework step happened across 2026-11-03 and 2026-11-06; these are the values that shipped, so they are what to
quote in the walkthrough:

- **Gun feel (was deferred):** Rock's heavy slug became a **shotgun** — 6-shell magazine, 8 pellets × 4 damage
  (32 per shell, unchanged total) in a 3.5° cone. Paper's charge rifle kept its 15 → 65 charge and gained a **30°
  aim zoom**; its reticle is a four-post gap cross with three mildots. Scissors' SMG is 7/s, 12-round magazine.
- **Fire rates came down after play:** Rock 1.1 → **0.85/s**, Paper 1.0 → **0.7/s**. Scissors unchanged.
- **Ammunition:** magazine + reload only; **no reserve and no ammo pool** — a reload always fills to the magazine
  and the player can never be left empty-handed. Reload: Rock 1.6 s, Paper 1.4 s, Scissors 1.1 s.
- **Overclock:** ×1.8 fire rate and +25 % speed for 3 s, and it **refills the magazine when it starts** (the burst
  out-runs a 12-round magazine in under a second, so starting loaded was the fix Chloe chose).
- **Wall climb:** **5 m/s** (roster said 3) and the top is a **0.08–0.4 s mantle** instead of a teleport. The
  climbable height cap is unchanged at 6 m.
- **Mark / Snare feedback (was "needs feedback"):** a white ring marks the enemy a ready Mark would take, amber
  while marked, cyan plus a tether while snared; a marked enemy's health bar no longer hides at full health.
- **Balance pass:** done, by play, in the same session (`docs/PLAN.md` § 7).


## Why this is code-shaped the way it is

Every number above is a field on a ScriptableObject: `WeaponDefinition` (damage, rate, charge, spread, range,
firing slowdown), `AbilityDefinition` (kind, cooldown, duration, magnitude, radius, range),
`CharacterDefinition` (health, speed, body scale/colour, one weapon + two abilities). Classes implement
**behaviour only**, so rebalancing is an asset edit and the code walkthrough can show that the balance table is
data.
