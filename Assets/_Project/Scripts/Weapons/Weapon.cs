using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;

namespace HeroShooter.Weapons
{
    /// <summary>
    /// The player's gun. It owns no balance numbers any more: everything (damage, rate, charge, spread, range,
    /// firing slowdown) comes from a <see cref="WeaponDefinition"/>, so each hero carries its own gun as data
    /// and rebalancing is an asset edit. Charge weapons fire on release; the rest fire on the trigger.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        /// <summary>Roster decision: aiming halves the spread for every hero. Not a per-weapon number.</summary>
        const float AimSpreadMultiplier = 0.5f;

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
        bool _charging;

        public WeaponDefinition Definition => definition;
        public bool IsCharging => _charging;

        /// <summary>0..1 while a charge weapon is held down.</summary>
        public float ChargeRatio { get; private set; }

        public int ShotsFired { get; private set; }

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
        }

        /// <summary>Called by the loadout when a character is selected.</summary>
        public void SetDefinition(WeaponDefinition newDefinition)
        {
            definition = newDefinition;
            _charging = false;
            ChargeRatio = 0f;
            _nextShotTime = 0f;
            ShotsFired = 0;
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

            if (!_charging)
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

        void Fire(float charge)
        {
            ResolvePool();

            if (definition == null || pool == null || Time.time < _nextShotTime)
            {
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

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplySpread(SolveAimDirection(origin));

            pool.Fire(origin, direction, speed, damage, gameObject, default(LayerMask),
                definition.projectileLifetime, definition.projectileRadius);

            if (status != null && definition.firingSpeedMultiplier < 1f)
            {
                status.ApplyFiringSlow(definition.firingSpeedMultiplier, definition.firingSlowDuration);
            }

            ShotsFired++;
        }

        /// <summary>Aim at whatever the crosshair is over, then fire from the muzzle toward that point.</summary>
        Vector3 SolveAimDirection(Vector3 origin)
        {
            // A prefab cannot hold a reference to a scene camera, so resolve it here if it was left empty.
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (aimCamera == null)
            {
                return transform.forward;
            }

            float maxDistance = definition != null
                ? Mathf.Max(30f, definition.projectileSpeed * definition.projectileLifetime)
                : 200f;

            Ray ray = aimCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            Vector3 aimPoint = ray.GetPoint(maxDistance);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                aimPoint = hit.point;
            }

            return (aimPoint - origin).normalized;
        }

        Vector3 ApplySpread(Vector3 direction)
        {
            float spread = definition != null ? definition.spreadDegrees : 0f;

            if (InputReader.AimHeld)
            {
                spread *= AimSpreadMultiplier;
            }

            if (spread <= 0f)
            {
                return direction;
            }

            float yaw = Random.Range(-spread, spread);
            float pitch = Random.Range(-spread, spread);
            return Quaternion.Euler(pitch, yaw, 0f) * direction;
        }
    }
}
