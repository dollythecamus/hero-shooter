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

## Wiring checklist — **done by the agent through the MCP bridge, 2026-10-23 13:32**

Executed in the editor over MCP for Unity (v10, HTTP :8080), not by hand: layers, materials,
`Projectile.prefab`, `ProjectilePool`, `Player.prefab` (CharacterController + motor + combat + health
+ weapon + Muzzle + body), camera rig, ProBuilder-free greybox arena, three damageable targets,
scene saved to `Assets/_Project/Scenes/90_TestArena.unity` and added to Build Settings.
`Assets/Scenes/Level.unity` is Chloe's own ProBuilder scene and was left untouched.

## Gate — **passed 2026-10-23 13:47**

| Check | Evidence |
|---|---|
| Pool warms up | `created=64 active=0 inactive=64` on entering play mode |
| Weapon→pool path | `weapon.TryFire()` → `shotsFired=1 active=1 created=64` |
| No runtime growth | 61 shots fired → `created=64 active=61`, then `active=0 inactive=64` after settling |
| Damage lands | console: `[DamageLog] Target_2 hit for 20 → 40/60` … `Target_2 hit for 20 → 0/60` |
| No self-damage | player `100/100` after firing through the crosshair path |
| `Instantiate` only in warm-up | `Core/ObjectPool.cs` is the only file that calls it |

### Pitfall found here (cost 20 minutes)

A `LayerMask` field does **not** serialize as `hitMask.m_Bits` in Unity 6. `SerializedObject
.FindProperty("hitMask.m_Bits")` returns **null**, the `if (prop != null)` guard silently skipped the
write, the mask stayed `Nothing`, and every `Physics.SphereCast` in the projectile hit nothing — bullets
flew 200 m and expired on lifetime, so the pool behaved perfectly while doing zero damage. The correct
property path is the field itself: `so.FindProperty("hitMask").intValue = ~(1 << 6)`.
**Always read the value back** (`new SerializedObject(...).FindProperty("hitMask").intValue`) instead of
trusting that `ApplyModifiedProperties` did what the code intended.

### Second pitfall — LMB did nothing (found by Chloe in the first manual play)

`PlayerCombat.weapon` was **NULL** in the prefab: `AddComponent` in editor code never fills
`[SerializeField]` object references, and the null-guard in `Update()` swallowed the whole fire path, so
move/look worked while shooting silently did nothing. `Weapon.aimCamera` was null for a second, structural
reason: **a prefab asset cannot hold a reference to a scene object**, so an assignment made on the instance
is dropped when the prefab is saved.

Rules that follow:

- After any programmatic wiring, enumerate every component's serialized **object-reference** properties
  and assert non-null; a null reference field plus a guard is indistinguishable from a broken feature.
- Give same-GameObject collaborators a `GetComponent<T>()` fallback in `Awake` and scene lookups
  (`Camera.main`) a lazy resolve at use, so a prefab stays self-sufficient.
- Verify the real input path, not just the method call: inject device state in play mode and read the
  resulting counters.
  `InputSystem.QueueStateEvent<MouseState>(Mouse.current, new MouseState { buttons = 1 });`
  then `InputSystem.Update()` — `InputReader.FireHeld` reads `true`, the weapon fires at its rate, and the
  state reverts on the next system update, so sample within the same or the next second.


