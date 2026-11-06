# Weapons & projectiles

> **Currency note (2026-11-06):** this document describes the system as designed and built on 2026-10-23/24. Everything that changed afterwards is listed in the *2026-11-06 update* at the bottom; where the two disagree, the update wins and `docs/PROJECT_LEDGER.md` has the evidence.

## Purpose

The graded "shooting with correct mechanics — no instantiate, use object pooling" feature. A hero's gun is a
`WeaponDefinition` ScriptableObject read by one `Weapon` component; every shot asks the single shared
`ProjectilePool` for a bullet, and the bullet flies a swept sphere-cast and returns itself. One bullet prefab
serves the player and the enemies, distinguished only by the layer mask it is launched with.

## Interface

```csharp
class WeaponDefinition : ScriptableObject {      // menu: Hero Shooter/Weapon Definition
    string displayName;
    float shotsPerSecond;        // 4
    float damage;                // 12
    float projectileSpeed;       // 70
    float spreadDegrees;         // 0.6  → ± this yaw and pitch, per shot; halved while RMB aiming
    float projectileLifetime;    // 3    → this * speed is the weapon's effective range
    float projectileRadius;      // 0.12 → swept-cast radius
    float chargeTime;            // 0 = instant fire, > 0 = seconds to full charge
    float chargedDamage;         // lerped from damage by charge ratio
    float chargedProjectileSpeed;
    [Range(0,1)] float firingSpeedMultiplier;  // 1 = no slowdown
    float firingSlowDuration;    // 0.4
    bool RequiresCharge { get; } // chargeTime > 0
}

class Weapon : MonoBehaviour {
    // serialized: definition, pool (optional), muzzle (optional), aimCamera (optional), status
    WeaponDefinition Definition { get; }
    bool IsCharging { get; }
    float ChargeRatio { get; }        // 0..1 while charging, else 0
    int ShotsFired { get; }
    void SetDefinition(WeaponDefinition newDefinition);   // called by HeroLoadout; resets charge, rate, counter
    void BeginTrigger();              // LMB down: starts a charge on charge weapons, fires instantly otherwise
    void EndTrigger();                // LMB up: fires a charge weapon with its charge ratio
    void TryFire();                   // fires at charge 0; ignored while charging
}

class Projectile : MonoBehaviour, IPooled {
    ObjectPool<Projectile> Pool { get; set; }   // set by ProjectilePool on creation
    // serialized: speed 70, damage 20, maxLifetime 3, radius 0.14, hitMask (prefab value = 191)
    void Launch(Vector3 direction, float speedOverride, float damageOverride, GameObject owner,
                LayerMask maskOverride = default, float lifetimeOverride = 0f, float radiusOverride = 0f);
    void OnSpawned(); void OnDespawned();
}

class ProjectilePool : ObjectPool<Projectile> {
    static ProjectilePool Instance { get; }     // first pool to Awake becomes the shared instance
    Projectile Fire(Vector3 position, Vector3 direction, float speed, float damage, GameObject owner,
                    LayerMask maskOverride = default, float lifetimeOverride = 0f, float radiusOverride = 0f);
}
```

## Depends on

- `damage-and-pooling.md` — `Projectile` is `IPooled`, pooled by `ObjectPool<Projectile>`; hits are delivered
  as `DamageInfo` to whatever `IDamageable` is on the hit collider's parent.
- `player-movement-and-camera.md` — rate of fire is multiplied by `PlayerStatus.FireRateMultiplier`; a
  `firingSpeedMultiplier < 1` calls `PlayerStatus.ApplyFiringSlow`; aim uses `Camera.main` when no camera is wired.

## Depended on by

- `enemy-fsm.md` — `RangedAttack` fires through `ProjectilePool.Instance` with `targetMask` = 64 (Player only)
  and damage/cooldown from `EnemyStats`, not from a `Weapon`.
- `characters-and-abilities.md` — each `CharacterDefinition` references exactly one `WeaponDefinition`;
  `HeroLoadout` calls `Weapon.SetDefinition`.
- `hud-aiming-and-feedback.md` — the HUD is meant to show `ChargeRatio`, and bullet traces/impact VFX are
  planned to hang off this file.

## Behavior notes

- **Trigger paths split by weapon kind.** `PlayerCombat` asks `Definition.RequiresCharge`: charge weapons call
  `BeginTrigger` on LMB down and `EndTrigger` on release; everything else calls `TryFire` every frame while
  held (that is also how automatic fire is implemented — the rate limiter, not the input, decides cadence).
- A charge weapon loses its shot if the trigger is released before `_charging` was ever set (e.g. the
  `Definition` was null on the press frame); `EndTrigger` returns early when `!_charging`.
- `Fire` calls `ResolvePool()` first, then returns early on a null definition, a null pool or
  `Time.time < _nextShotTime` — a missing pool therefore costs **nothing**, and a pool that woke up late is still
  found. `_nextShotTime` advances by `1 / (shotsPerSecond * FireRateMultiplier)` after those guards and before
  the projectile is requested, so a shot that fails later still consumes the rate slot.
- Damage and speed lerp by charge ratio only for charge weapons; non-charge weapons ignore `chargedDamage`.
- **Aim is solved at the screen centre**: a ray from `aimCamera` through `(Screen.width/2, Screen.height/2)`
  with `maxDistance = max(30, speed * lifetime)`; the first hit becomes the aim point, otherwise the far end of
  the ray. The muzzle then fires at that point, so the bullet converges on the crosshair rather than on the
  barrel axis. Spread (±`spreadDegrees` yaw/pitch) is applied *after* that solve, and **halved while RMB is
  held** (`Weapon.AimSpreadMultiplier = 0.5f`, a roster decision, so it is a const and not a per-weapon field —
  every hero gets the same −50 %). The flag is `InputReader.AimHeld`, read directly, not the camera rig's state.
- The aim ray uses `~0` — **everything**, including the Player layer. The camera looks at the hero, so the
  hero's own `CharacterController` capsule (layer 6, height 1.8, radius 0.35) is a candidate hit for that ray.
  It currently misses in practice because of the pivot offset, but any change to camera framing or pivot can
  silently redirect every bullet into the player's own back. Fix direction: raycast a mask that excludes
  Player, not `~0`.
- `Projectile` moves by `Physics.SphereCast` per `Update` over `speed * deltaTime`, `QueryTriggerInteraction.Ignore`.
  No Rigidbody, no `OnCollisionEnter` — no physics-tick latency and no tunnelling, and it must keep working if
  the prefab is reused with a much higher speed.
- `Projectile.Impact` uses `GetComponentInParent<IDamageable>()` and still returns to the pool when nothing
  damageable is there — the bullet is always consumed. It never destroys itself; with no pool it just
  deactivates.
- `maskOverride` uses `default(LayerMask)` (value 0) as the "keep the prefab mask" sentinel, so 0 **cannot**
  be passed to mean "hit nothing".
- Local scale is set on launch to `radiusOverride / serialized radius`, i.e. the visual scales with the
  collision radius. Changing the prefab's radius field rescales every variant.
- `Weapon` re-resolves `pool` on Awake **and** on every use if it is null (`ResolvePool`) because the pool
  object can wake after the player. `ProjectilePool.Instance` is the fallback; the scene's `ProjectilePool`
  object has `warmUpCount = 64` (verified in `90_TestArena.unity`).

## Current numbers (from `Assets/_Project/Data/Weapons/`)

| Weapon | Hero | shots/s | dmg | speed | range (speed × lifetime) | spread | notes |
|---|---|---|---|---|---|---|---|
| Heavy Slug | Rock | 1.1 | 32 | 45 | 12.6 m | 2.5° | slows to 45 % for 0.5 s per shot |
| Charge Rifle | Paper | 1.0 | 15 → 65 | 90 → 160 | 270 → 480 m | 0.15° | 1.2 s charge, fires on release |
| Quick SMG | Scissors | 7 | 11 | 85 | 102 m | 1.6° | no charge, no slow |

`Projectile.prefab` itself still carries `speed 70 / damage 20 / lifetime 3 / radius 0.12`, but every field is
overridden at launch, so those numbers only matter if something calls `Launch` with zeros.

## Known limitations / deliberately not handled

- Recoil: none. Ammunition, reload, muzzle flash, impact VFX, hit marker and tracer all exist now — see the
  2026-11-06 update below.
- No hit-scan option: everything is a travelling projectile, so range limits are lifetime × speed.
- The gun-feel rework **landed** on 2026-11-06 (ammunition, Rock's shotgun, fire rates); the values in the
  2026-11-06 update at the bottom of this file are the ones that shipped.
- The balance pass is done (2026-11-06); the 102 m SMG range stands as a deliberate tuning choice.
- `ShotsFired` is reset by `SetDefinition` and **consumed by the tutorial's Shoot gate**
  (`TutorialDirector` compares it against the count taken when the step started).

## 2026-11-06 update — ammunition, shotgun, and the shot now leaves the camera

- **Ammunition exists** (it did not when this doc was written): `WeaponDefinition.magazineSize` and `reloadTime`,
  owned by `Weapon` as `Magazine`/`IsReloading`. `R` reloads, an empty magazine reloads itself, and a reload runs on
  a deadline (`Time.time >= _reloadEnd`) so a paused game freezes it. **No reserve ammo** — a reload always fills to
  the magazine, so the player can never be left with nothing.
- **Multiple projectiles per trigger pull:** `pelletsPerShot` + `pelletSpreadDegrees`. Rock is 8 pellets × 4 damage
  (32 per shell, same total as the old slug) in a 3.5° cone.
- **The round leaves the camera-centre ray, not the muzzle.** `Weapon.ResolveAim` starts it at the hero's own depth
  on that ray, so the reticle and the bullet are the same line at every range. The first version aimed from the
  muzzle at a point on the ray, which is why the crosshair lied at close range — a gate found it, not a probe.
- Because the round now starts inside the hero, `Projectile` sweeps with `SphereCastNonAlloc` and **skips its
  owner's colliders**; that skip is what stops point-blank melee enemies being missed.
- **Fire-rate feedback:** `Weapon.ShotReadyRatio` (fills to the next shot) and `ReloadRatio` (amber while
  reloading) drive one bar under the crosshair.
- Feel numbers after the balance pass: Rock 0.85/s, Paper 0.7/s, Scissors 7/s unchanged.
