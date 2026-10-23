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
- Open questions (asked 2026-10-23): exact FOV values, offset amount, whether aiming slows movement, whether
  aiming tightens spread or changes damage.

### Visual feedback inventory — added 2026-10-23

| Where | What is required |
|---|---|
| Bullet impact | feedback on every hit: impact flash/spark, plus a HUD hit marker when the player lands one |
| Enemy health | **world-space health bar above each enemy, visible only while below max health** |
| Abilities | particle effects per ability (each entry below says what), plus HUD cooldown/meter feedback |
| Paper bullets | trail from muzzle to impact point, fading shortly after |
| Overclock | particles, trails and coloured bullets while active |
| Mark / Snare | visible indication of which enemy is marked / snared and for how long |
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

Open: jump height, slam descent speed, radius, damage, steering during the jump, cooldown (8 s today),
whether it can be started in mid-air.

### E — Bulwark (reworked: channelled shield, not a timed buff)

1. **Hold E** to raise a shield.
2. While held: **damage taken is reduced** and a **meter drains**, total capacity **5 seconds**.
3. Meter **hits 0 while held** → shield drops and the refresh cooldown is **longer**.
4. **Released before 0** → meter **refills at 1 per second** after a short delay.
5. Cannot be re-activated during the short cooldown after a release.

Open: damage reduction value (×0.4 today), whether Rock can shoot while shielded, whether movement slows,
length of the short cooldown after release, length of the longer cooldown after depletion, delay before the
refill starts.

## Paper — the sniper

| | |
|---|---|
| Health | 90 (smallest) |
| Move speed | 5.2 |
| Body | slimmest capsule, pale blue |
| Weapon | **charge on aim**: 15 → 65 damage over 1.2 s, projectile speed 90 → 160, **bullet trail** |
| Signature | no movement ability at all — the charge is the whole kit; **sniper aim reticle** |

### Q — Mark (kept, needs feedback)

The enemy under the crosshair **takes 35 % more damage for 6 s** (range 70, cooldown 10 s). Needs **visual
feedback of which enemy is marked and which mark is active right now** — world marker on the enemy plus a HUD
chip with remaining time.

### E — Snare (kept, needs feedback)

First enemy under the crosshair is **slowed to 25 % for 3 s** (range 14, cooldown 12 s). Needs the same
feedback: visible state on the snared enemy plus a HUD indicator with remaining time.

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

Open: maximum height (6 m today), climb speed, whether the exit jump direction is forward or wall-normal,
whether climbing costs anything.

### E — Overclock (kept, needs feedback)

+80 % fire rate and +25 % move speed for 3 s (12 s cooldown). Needs **particles, trails and coloured bullets**
while active so the state is readable in a fight.

## Deferred to the rework step

- Guns and projectiles: **Chloe is not satisfied with the current feel** — design questions pending (asked
  2026-10-23, answers will be recorded here before the rework starts).
- Balance pass: still deferred until every feature exists (`docs/PLAN.md` § 7).

## Why this is code-shaped the way it is

Every number above is a field on a ScriptableObject: `WeaponDefinition` (damage, rate, charge, spread, range,
firing slowdown), `AbilityDefinition` (kind, cooldown, duration, magnitude, radius, range),
`CharacterDefinition` (health, speed, body scale/colour, one weapon + two abilities). Classes implement
**behaviour only**, so rebalancing is an asset edit and the code walkthrough can show that the balance table is
data.
