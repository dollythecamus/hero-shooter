# Weapons & projectiles

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
    float spreadDegrees;         // 0.6  → ± this yaw and pitch, per shot
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
    // serialized: speed 70, damage 20, maxLifetime 3, radius 0.12, hitMask (prefab value = -65)
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
  barrel axis. Spread (±`spreadDegrees` yaw/pitch) is applied *after* that solve.
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

- No recoil, no ammo, no reload, no muzzle flash, no impact VFX, no hit marker, no tracer — the S4 feedback
  block (`docs/features/hud-aiming-and-feedback.md`) owns all of it.
- No hit-scan option: everything is a travelling projectile, so range limits are lifetime × speed.
- Chloe is not satisfied with the current gun feel; a guns/projectiles rework is **pending her answers**
  (ledger open question). Do not change feel numbers before that lands.
- Balance is deferred on purpose until every feature exists (`docs/PLAN.md` § 7). The 102 m SMG range is a
  tuning observation, not a bug to "fix" now.
- `ShotsFired` is a debugging counter reset by `SetDefinition`; nothing consumes it yet.
