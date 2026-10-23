# S3 — three heroes, weapons and abilities as data (2026-10-23)

Roster design: `docs/HERO_ROSTER.md`. Layers: `docs/LAYERS.md`.
Status: **code + wiring complete and compiling; selection verified end to end; the six ability effects are
written but not yet verified in play mode** (that is the next gate, before S4).

## Data layer

| File | Responsibility |
|---|---|
| `Data/WeaponDefinition.cs` | rate, damage, projectile speed/lifetime/radius, spread, **charge** (`chargeTime`, `chargedDamage`, `chargedProjectileSpeed`), firing slowdown |
| `Data/AbilityDefinition.cs` | `AbilityKind` + cooldown, duration, magnitude, radius, range, description |
| `Data/CharacterDefinition.cs` | display name, pitch, body scale/colour, health, speed, one weapon + two abilities |

Assets: `Data/Weapons/{Weapon_Rock,Weapon_Paper,Weapon_Scissors}.asset`,
`Data/Abilities/Ability_{Rock_Slam,Rock_Bulwark,Paper_Mark,Paper_Snare,Scissors_Climb,Scissors_Overclock}.asset`,
`Data/Characters/{Hero_Rock,Hero_Paper,Hero_Scissors}.asset`.

## Runtime

| File | Responsibility |
|---|---|
| `Player/PlayerStatus.cs` | timed effects (Speed / FireRate / DamageTaken / Climb); the only place multipliers are applied; does the climb movement |
| `Player/Abilities/IAbility.cs`, `AbilityBase.cs` | contract + shared definition access |
| `Player/Abilities/AbilityFactory.cs` | `AbilityKind` → behaviour (one switch; adding an ability = enum value + asset + class) |
| `Player/Abilities/{GroundSlam,Bulwark,MarkTarget,Snare,WallClimb,Overclock}.cs` | the six behaviours, each only "what happens on press" |
| `Player/Abilities/AimRaycast.cs` | "whatever the crosshair is over", with line of sight, shared by Mark and Snare |
| `Player/PlayerAbilities.cs` | builds slots from the character, cooldown clocks, Q/E, cooldown ratios for the HUD |
| `Player/HeroLoadout.cs` | applies a `CharacterDefinition` to health, motor, weapon, abilities, appearance — the whole "select a character" operation |
| `Player/PlayerAppearance.cs` | body scale + colour via `MaterialPropertyBlock` |
| `Systems/MatchBootstrap.cs` | roster, freeze hero until selection, spawn point, cursor handling |
| `UI/CharacterSelectUI.cs` | controller for the authored screen (fills cards, shows/hides) |
| `UI/CharacterCardUI.cs` | one authored card: three text refs, background tint, and `Click()` wired as a **persistent** listener |

`Weapons/Weapon.cs` was rewritten to read a `WeaponDefinition`: it no longer owns balance numbers, supports
charge-on-hold (Paper) and applies the firing slowdown through `PlayerStatus` (Rock).

## UI rule (Chloe, 2026-10-23)

**UI is authored as prefabs, never built at runtime.** The first version of the select screen built its own
canvas in `Awake`, which made it hard to edit and read poorly on screen. It is now
`Prefabs/UI/CharacterSelect.prefab`: canvas (ScaleWithScreenSize, 1920×1080, match 0.5), title/subtitle/hint
texts, three cards with their own `Text` widgets, and each card's `Button` wired in the prefab to
`CharacterCardUI.Click` (visible in the inspector — a UnityEvent cannot carry the index, so the card owns
its `rosterIndex`). The same rule applies to the HUD in S4.

## Verified

```
click Card_Scissors → selected=Scissors heroActive=True hp=130/130
weapon=Quick SMG abilities=Scissors Climb/Scissors Overclock uiVisible=False
```

- authored prefab fills all 12 texts from the roster (name, pitch, both abilities with cooldowns)
- persistent listener present on each card button (`GetPersistentEventCount() == 1`, target = the card)
- Rock selection: 200/200 hp, Heavy Slug (1.1 shots/s), body scaled ×1.25
- Paper selection earlier in a live session: Charge Rifle, 22 shots fired, charge ratio live at 0.06

## Still to verify (next gate)

| Ability | Expected |
|---|---|
| Rock Slam | 40 damage in 5 m + knockback |
| Rock Bulwark | `Health.IncomingDamageMultiplier == 0.4`, speed ×0.75 for 4 s |
| Paper Mark | target's `IncomingDamageMultiplier == 1.35` for 6 s |
| Paper Snare | target `EnemyBrain.SpeedMultiplier == 0.25` for 3 s |
| Scissors Climb | hero rises ~6 m over 0.9 s |
| Scissors Overclock | fire rate ×1.8, speed ×1.25 for 3 s |

Cooldowns must read `CooldownRatio > 0` immediately after activation and `IsReady == false`.
