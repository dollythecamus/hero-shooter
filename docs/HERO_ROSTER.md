# Hero roster — rock, paper, scissors

Decided by Chloe 2026-10-23. Explicitly **Team Fortress 2 as the reference**: three classes, each strong
against one and weak against another, readable silhouettes, no overlap in role. The point of the design is
that a balanced build produces a visible circle rather than one strongest hero.

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
- **Rock beats Scissors** — survives a skirmisher's burst, and the ground slam punishes anyone who hugs.

## Rock — the tank

| | |
|---|---|
| Health | 200 |
| Move speed | 4.2 (slowest) |
| Body | largest capsule, warm orange |
| Weapon | heavy short-range slug: 32 damage, 1.1 shots/s, slow projectile, effective range ~13 m |
| Signature | **slows to 45 % speed while firing** — committing to a shot costs mobility |

- **Q — Ground Slam** (8 s): point-blank area burst, 40 damage in a 5 m radius. Answer to anyone rushing in.
- **E — Bulwark** (14 s, 4 s): takes 40 % damage and moves at 75 % speed. Survives a Scissors burst.

## Paper — the sniper

| | |
|---|---|
| Health | 90 (smallest) |
| Move speed | 5.2 |
| Body | slimmest capsule, pale blue |
| Weapon | **charge on aim**: 15 → 65 damage over 1.2 s, projectile speed 90 → 160 |
| Signature | no movement ability at all — the charge is the whole kit |

- **Q — Mark Target** (10 s, 6 s, range 70): paints an enemy, it takes 35 % more damage. Anti-tank.
- **E — Snare** (12 s, 3 s, range 14): roots the first enemy in front to 25 % speed. Anti-rush.

## Scissors — the skirmisher

| | |
|---|---|
| Health | 130 |
| Move speed | 6.8 (fastest) |
| Body | slim capsule, green |
| Weapon | fast medium gun: 11 damage, 7 shots/s, 85 projectile speed |

- **Q — Wall Climb** (8 s): climbs up to 6 m of wall — reach a sightline or escape a bad angle.
- **E — Overclock** (12 s, 3 s): +80 % fire rate, +25 % move speed. The burst that deletes a sniper.

## Why this is code-shaped the way it is

Every number above is a field on a ScriptableObject: `WeaponDefinition` (damage, rate, charge, spread, range,
firing slowdown), `AbilityDefinition` (kind, cooldown, duration, magnitude, radius, range),
`CharacterDefinition` (health, speed, body scale/colour, one weapon + two abilities). The classes implement
**behaviour only**, so rebalancing after the sprint is an asset edit, not a code change — and the code
walkthrough can show "the balance table is data" instead of hunting for magic numbers.
