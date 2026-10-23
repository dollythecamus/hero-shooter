using UnityEngine;

namespace HeroShooter.Weapons
{
    /// <summary>
    /// Fire-rate gate, aim solve and pool request. It owns no projectile instance:
    /// the pool does the creating, this only asks for one.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] ProjectilePool pool;

        [Header("Spawn point")]
        [SerializeField] Transform muzzle;

        [Header("Ballistics")]
        [SerializeField] float shotsPerSecond = 8f;
        [SerializeField] float projectileSpeed = 70f;
        [SerializeField] float damage = 20f;

        [Tooltip("Random cone in degrees applied around the aim direction.")]
        [SerializeField] float spreadDegrees = 0.5f;

        [Header("Aim")]
        [Tooltip("Camera used to solve the aim point at the screen centre (crosshair aiming).")]
        [SerializeField] Camera aimCamera;
        [SerializeField] float maxAimDistance = 200f;

        float _nextShotTime;

        public int ShotsFired { get; private set; }
        public float Damage => damage;

        void Awake()
        {
            ResolvePool();
        }

        /// <summary>Late binding: the pool may wake up after this component, so re-check on use.</summary>
        void ResolvePool()
        {
            if (pool == null)
            {
                pool = ProjectilePool.Instance;
            }
        }

        public void TryFire()
        {
            ResolvePool();

            if (pool == null || Time.time < _nextShotTime)
            {
                return;
            }

            _nextShotTime = Time.time + 1f / Mathf.Max(1f, shotsPerSecond);

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplySpread(SolveAimDirection(origin));

            pool.Fire(origin, direction, projectileSpeed, damage, gameObject);
            ShotsFired++;
        }

        /// <summary>Aim at whatever the crosshair is over, then fire from the muzzle toward that point.</summary>
        Vector3 SolveAimDirection(Vector3 origin)
        {
            if (aimCamera == null)
            {
                return transform.forward;
            }

            Ray ray = aimCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            Vector3 aimPoint = ray.GetPoint(maxAimDistance);

            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                aimPoint = hit.point;
            }

            return (aimPoint - origin).normalized;
        }

        Vector3 ApplySpread(Vector3 direction)
        {
            if (spreadDegrees <= 0f)
            {
                return direction;
            }

            float yaw = Random.Range(-spreadDegrees, spreadDegrees);
            float pitch = Random.Range(-spreadDegrees, spreadDegrees);
            return Quaternion.Euler(pitch, yaw, 0f) * direction;
        }
    }
}
