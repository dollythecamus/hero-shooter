# S1 — player, camera, pooled shooting (2026-10-23)

Code written by the agent (committed). Everything below is the **editor-side wiring** that only
someone sitting in the editor can do — GUI Unity does not let the agent edit scenes on disk safely
while the editor holds them (use the MCP bridge once Auto-Connect is running).

## Files in this block

| File | Responsibility |
|---|---|
| `Core/IPooled.cs` | contract a pooled object implements (`OnSpawned` / `OnDespawned`) |
| `Core/ObjectPool.cs` | generic warm-up pool: `Get` / `Release`, exposes `TotalCreated` / `ActiveCount` |
| `Core/IDamageable.cs` | `DamageInfo` struct + `IDamageable` interface |
| `Core/Health.cs` | health value + `Damaged` / `Died` events, shared by player, enemies, props |
| `Weapons/Projectile.cs` | pooled bullet, `Physics.SphereCast` sweep, returns itself to the pool |
| `Weapons/ProjectilePool.cs` | `ObjectPool<Projectile>` + back-reference wiring + `Fire(...)` |
| `Weapons/Weapon.cs` | fire-rate gate, crosshair aim solve, spread, pool request |
| `Player/InputReader.cs` | the only place hardware is read (WASD, mouse, Q/E, Tab, Esc) |
| `Player/PlayerMotor.cs` | CharacterController movement, camera-relative, gravity + jump |
| `Player/ThirdPersonCameraRig.cs` | orbit camera with pitch clamp, smoothing, collision pull-in |
| `Player/PlayerCombat.cs` | fire input → `Weapon.TryFire()` (abilities land here in S3) |
| `Systems/DamageLog.cs` | S1 verification helper: console line per hit (delete once the HUD exists) |

## Wiring checklist

1. **Layers** (*Project Settings → Tags and Layers*): add `Player` (6) and `Enemy` (7).
2. **Projectile prefab** (`Assets/_Project/Prefabs/Projectiles/Projectile.prefab`): root = empty
   `Projectile` GameObject with the `Projectile` component; child = sphere mesh scaled ~0.12 with an
   emissive URP Lit material; **no collider needed** (the sweep does the collision). Set the
   component's `hitMask` = `Default` + `Enemy`, leave `Player` out (no shooting yourself).
3. **Pool** (`Assets/_Project/Prefabs/Projectiles/ProjectilePool.prefab` or a scene object):
   `ProjectilePool` component, `prefab` = the projectile, `warmUpCount` = 64, `container` = itself.
4. **Player** (`Assets/_Project/Prefabs/Player/Player.prefab`):
   - root `Player` on layer `Player`: `CharacterController` (height 1.8, radius 0.35, centre y 0.9),
     `PlayerMotor`, `PlayerCombat`, `Health` (maxHealth 100);
   - child `Muzzle` (empty, at chest height, local +Z forward) — assign to `Weapon.muzzle`;
   - child mesh for the body (capsule or ProBuilder block + coloured material);
   - `Weapon` component on the root: `pool` = the pool, `muzzle` = the Muzzle child,
     `aimCamera` = the main camera. `shotsPerSecond` 8, `projectileSpeed` 70, `damage` 20.
5. **Camera**: the scene's `Main Camera` gets `ThirdPersonCameraRig`, `target` = Player root,
   `distance` 4.5, `pivotOffset` (0, 1.6, 0), `collisionMask` = Default (not the Player layer).
   `PlayerMotor.cameraTransform` = that camera transform.
6. **Test arena** (`Assets/_Project/Scenes/90_TestArena.unity`, ProBuilder): floor 40×40, a few
   blocks at 5–20 m for cover and for hitting, a directional light, one `ProjectilePool` in the scene.
7. **Test target**: 3 cubes, each with `Health` (maxHealth 60) + `DamageLog`. Give them the `Enemy`
   layer so the bullet's `hitMask` catches them.
8. **Save the scene, add it to Build Settings**, then commit.

## Gate (must pass before S2)

- Play, hold LMB ~8 s at 8 shots/s → console shows `[DamageLog] ... hit for 20 → 40/60` on the cubes.
- Stop play, select the `ProjectilePool`, context menu **Log pool stats** →
  `created=64 active=0 inactive=64` — the count from before firing is unchanged.
- `grep -rn "Instantiate(" Assets/_Project/Scripts` returns **only** `Core/ObjectPool.cs`
  (warm-up). Nothing else in the project may create objects at runtime.

If `created` grew past the warm-up count, the fire rate outran the pool: raise `warmUpCount`, do not
add a second pool.
