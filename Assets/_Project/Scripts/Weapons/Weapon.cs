using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;

namespace HeroShooter.Weapons
{
    /// <summary>
    /// The player's gun. It owns no balance numbers any more: everything (damage, rate, charge, spread, range,
    /// firing slowdown, magazine) comes from a <see cref="WeaponDefinition"/>, so each hero carries its own gun
    /// as data and rebalancing is an asset edit. Charge weapons fire on release; the rest fire on the trigger.
    ///
    /// Ammunition joined that list on 2026-11-06 (gate feedback: *"weapons rework: need ammunition, ammo hud,
    /// and reload times"*). The magazine and the reserve both live here and nowhere else, so the HUD, the
    /// auto-reload and the manual reload cannot disagree about how many rounds are left.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        /// <summary>Roster decision: aiming halves the spread for every hero. Not a per-weapon number.</summary>
        const float AimSpreadMultiplier = 0.5f;

        /// <summary>Paper's bullets carry a thin bright tracer (roster); a charge weapon is Paper's sniper.</summary>
        static readonly Color TracerColor = new Color(0.6f, 0.92f, 1f, 0.55f);

        /// <summary>Overclock tints bullets, trails and particles green for every hero (roster).</summary>
        static readonly Color OverclockGreen = new Color(0.3f, 1f, 0.4f, 0.65f);

        [Header("Data")]
        [SerializeField] WeaponDefinition definition;

        [Header("References")]
        [SerializeField] ProjectilePool pool;
        [SerializeField] Transform muzzle;
        [Tooltip("Camera used to solve the aim point at the screen centre. Falls back to Camera.main.")]
        [SerializeField] Camera aimCamera;
        [SerializeField] PlayerStatus status;

        float _nextShotTime;
        float _chargeStart;
        float _reloadStart;
        float _reloadEnd;
        bool _charging;

        public WeaponDefinition Definition => definition;
        public bool IsCharging => _charging;

        /// <summary>0..1 while a charge weapon is held down.</summary>
        public float ChargeRatio { get; private set; }

        public int ShotsFired { get; private set; }

        /// <summary>
        /// Rounds left in the magazine. There is no reserve any more (second 2026-11-06 gate: a player could
        /// run dry with no way to get ammo back) — a reload always fills to <see cref="WeaponDefinition.magazineSize"/>.
        /// </summary>
        public int Magazine { get; private set; }

        public bool IsReloading { get; private set; }

        /// <summary>True when a trigger pull would actually produce a shot right now.</summary>
        public bool CanFire => definition != null && !IsReloading && Magazine > 0 && Time.time >= _nextShotTime;

        /// <summary>0..1 through the reload; 0 when not reloading. The HUD's bar doubles as its progress.</summary>
        public float ReloadRatio => !IsReloading || definition == null
            ? 0f
            : Mathf.Clamp01((Time.time - _reloadStart) / Mathf.Max(0.01f, definition.reloadTime));

        /// <summary>
        /// 0..1 toward the next shot. The fire-rate bar fills to full exactly as the weapon becomes ready,
        /// which is the "small bar filling up to tell when the next shot is ready" the gate asked for.
        /// </summary>
        public float ShotReadyRatio
        {
            get
            {
                if (definition == null)
                {
                    return 1f;
                }

                float interval = 1f / Mathf.Max(0.01f, definition.shotsPerSecond * (status != null ? status.FireRateMultiplier : 1f));

                return Mathf.Clamp01(1f - (_nextShotTime - Time.time) / interval);
            }
        }

        void Awake()
        {
            ResolvePool();
        }

        void Update()
        {
            ResolvePool();

            if (_charging && definition != null && definition.RequiresCharge)
            {
                ChargeRatio = Mathf.Clamp01((Time.time - _chargeStart) / Mathf.Max(0.01f, definition.chargeTime));
            }

            // Reloads finish on a deadline rather than a coroutine so a paused game (timeScale 0) freezes them
            // with everything else, and so a weapon swap cannot leave a half-finished reload running.
            if (IsReloading && Time.time >= _reloadEnd)
            {
                FinishReload();
            }
        }

        /// <summary>Called by the loadout when a character is selected.</summary>
        public void SetDefinition(WeaponDefinition newDefinition)
        {
            definition = newDefinition;
            _charging = false;
            _chargeStart = 0f;
            _reloadStart = 0f;
            _reloadEnd = 0f;
            ChargeRatio = 0f;
            _nextShotTime = 0f;
            ShotsFired = 0;
            IsReloading = false;

            Magazine = newDefinition != null ? Mathf.Max(0, newDefinition.magazineSize) : 0;
        }

        /// <summary>Late binding: the pool may wake up after this component, so re-check on use.</summary>
        void ResolvePool()
        {
            if (pool == null)
            {
                pool = ProjectilePool.Instance;
            }
        }

        /// <summary>Trigger pressed. Charge weapons start charging, the rest fire immediately.</summary>
        public void BeginTrigger()
        {
            if (definition == null)
            {
                return;
            }

            if (!definition.RequiresCharge)
            {
                TryFire();
                return;
            }

            if (!_charging && !IsReloading)
            {
                _charging = true;
                _chargeStart = Time.time;
                ChargeRatio = 0f;
            }
        }

        /// <summary>Trigger released. Only charge weapons do anything here.</summary>
        public void EndTrigger()
        {
            if (!_charging)
            {
                return;
            }

            float charged = ChargeRatio;
            _charging = false;
            ChargeRatio = 0f;
            Fire(charged);
        }

        /// <summary>Fire without charging — also how automatic weapons are driven while held.</summary>
        public void TryFire()
        {
            if (_charging)
            {
                return;
            }

            Fire(0f);
        }

        /// <summary>Drops a charge without firing (Bulwark comes up mid-charge).</summary>
        public void CancelCharge()
        {
            _charging = false;
            ChargeRatio = 0f;
        }

        /// <summary>
        /// R. Always available while the magazine is not already full — there is no reserve to run out of, so
        /// the only refusal left is "nothing to do" (second 2026-11-06 gate: R did nothing because the reserve
        /// was empty).
        /// </summary>
        public void BeginReload()
        {
            if (definition == null || IsReloading)
            {
                return;
            }

            if (Magazine >= definition.magazineSize)
            {
                return;
            }

            IsReloading = true;
            _charging = false;
            ChargeRatio = 0f;
            _reloadStart = Time.time;
            _reloadEnd = Time.time + Mathf.Max(0.05f, definition.reloadTime);
        }

        void FinishReload()
        {
            IsReloading = false;
            Magazine = Mathf.Max(0, definition.magazineSize);
        }

        void Fire(float charge)
        {
            ResolvePool();

            if (definition == null || pool == null || Time.time < _nextShotTime || IsReloading)
            {
                return;
            }

            // An empty magazine does not click: it starts the reload, which is the one behaviour every
            // shooter player already expects.
            if (Magazine <= 0)
            {
                BeginReload();
                return;
            }

            float fireRate = definition.shotsPerSecond * (status != null ? status.FireRateMultiplier : 1f);
            _nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);

            float damage = definition.RequiresCharge
                ? Mathf.Lerp(definition.damage, definition.chargedDamage, charge)
                : definition.damage;

            float speed = definition.RequiresCharge && definition.chargedProjectileSpeed > 0f
                ? Mathf.Lerp(definition.projectileSpeed, definition.chargedProjectileSpeed, charge)
                : definition.projectileSpeed;

            // Roster decision (2026-11-06): the bullet leaves the camera axis, so the crosshair is true at
            // every range. The muzzle flash stays cosmetic; the shot no longer starts at the barrel,
            // because a barrel offset put the bullet a hand's width off the reticle at close range.
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = transform.forward;
            ResolveAim(ref origin, ref direction);

            bool overclocked = status != null && status.HasEffect(StatusEffect.Overclock);
            Color trail = overclocked ? OverclockGreen : definition.RequiresCharge ? TracerColor : default(Color);

            int pellets = Mathf.Max(1, definition.pelletsPerShot);
            float spread = pellets > 1 ? definition.pelletSpreadDegrees : definition.spreadDegrees;

            for (int i = 0; i < pellets; i++)
            {
                pool.Fire(origin, Scatter(direction, spread), speed, damage, gameObject, default(LayerMask),
                    definition.projectileLifetime, definition.projectileRadius, trail, overclocked);
            }

            Magazine--;
            ShotsFired++;

            if (Magazine <= 0)
            {
                BeginReload();
            }

            if (status != null && definition.firingSpeedMultiplier < 1f)
            {
                status.ApplyFiringSlow(definition.firingSpeedMultiplier, definition.firingSlowDuration);
            }
        }

        /// <summary>
        /// Puts the shot on the crosshair. The aim camera's centre ray <em>is</em> the crosshair, so the
        /// projectile starts on that ray a little past the hero and travels along it — the reticle cannot
        /// disagree with the bullet, at any range, because both are the same line.
        /// </summary>
        void ResolveAim(ref Vector3 origin, ref Vector3 direction)
        {
            // A prefab cannot hold a reference to a scene camera, so resolve it here if it was left empty.
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (aimCamera == null)
            {
                return;
            }

            Ray ray = aimCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

            // Start the round at the hero's own depth on that ray rather than half a metre past it: a melee
            // enemy pressed against the hero lives inside that gap and was being missed (second 2026-11-06
            // gate). Spawning inside the hero's capsule is safe because the projectile ignores its owner.
            Vector3 chest = transform.position + Vector3.up * 1f;
            float start = Mathf.Max(0.2f, Vector3.Dot(chest - ray.origin, ray.direction));

            origin = ray.origin + ray.direction * start;
            direction = ray.direction;
        }

        /// <summary>Random cone around the aim line; aiming halves it for every hero (roster).</summary>
        static Vector3 Scatter(Vector3 direction, float degrees)
        {
            if (InputReader.AimHeld)
            {
                degrees *= AimSpreadMultiplier;
            }

            if (degrees <= 0f)
            {
                return direction;
            }

            float yaw = Random.Range(-degrees, degrees);
            float pitch = Random.Range(-degrees, degrees);
            return Quaternion.Euler(pitch, yaw, 0f) * direction;
        }
    }
}
