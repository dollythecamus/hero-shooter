# Layers — the project's convention

Set by Chloe 2026-10-23. The arena geometry moved to a dedicated **Structure** layer, so anything that
asks "is this a wall?" must use that layer instead of `Default`.

| Index | Name | What is on it | Used by |
|---|---|---|---|
| 0 | Default | leftovers, primitive test objects | — |
| 3 | **Structure** | arena floor, walls, cover blocks, ProBuilder level geometry | camera collision, ability line of sight, `Spyglass.sightBlockers` |
| 6 | Player | the hero | excluded from `Projectile.hitMask`; camera collision |
| 7 | Enemy | enemy bodies (capsule colliders) | player bullets, `RangedAttack.targetMask`, `PlayerAbilities.enemyMask` |

Mask values that follow from this, as serialized numbers (`1 << index`):

- `Projectile.hitMask` = `-(1 << 6) - 1` = **-65** → everything except Player (one bullet prefab serves both sides)
- `RangedAttack.targetMask` = **64** → Player only (enemy fire cannot hit other enemies)
- `PlayerAbilities.enemyMask` = **128** → Enemy
- `PlayerAbilities.worldMask` = **9** → Default + Structure
- `ThirdPersonCameraRig.collisionMask` = **8** → Structure (the camera boom pulls in on level geometry)
- `Spyglass.sightBlockers` = **1** or **9** → walls break line of sight

Re-verified 2026-10-23 by dumping every `SerializedPropertyType.LayerMask` on the player, enemy and
projectile prefabs. An earlier audit called the `8` and `9` values "wrong" — they are not: they are the
Structure layer. Do not "fix" them back to `Default`.

## Rule going forward

New level geometry goes on **Structure** (index 3), never `Default`. New queries about the world use
`worldMask` / `collisionMask` / `sightBlockers`, all of which already contain Structure.
