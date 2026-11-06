using HeroShooter.Core;
using HeroShooter.VFX;
using UnityEngine;

namespace HeroShooter.Weapons
{
    /// <summary>
    /// Pooled bullet. Movement is a swept sphere (<see cref="Physics.SphereCast"/>) checked
    /// every frame, not a Rigidbody: no physics-tick latency and no tunnelling at high speed.
    /// The instance never destroys itself — it always returns to its pool.
    /// </summary>
    public class Projectile : MonoBehaviour, IPooled
    {
        [Header("Flight")]
        [SerializeField] float speed = 70f;
        [SerializeField] float damage = 20f;
        [SerializeField] float maxLifetime = 3f;
        [SerializeField] float radius = 0.12f;

        [Tooltip("Layers this bullet can hit. Keep the player's own layer out for friendly fire.")]
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Visuals")]
        [Tooltip("Optional trail. Off unless a launch asks for one — Paper's tracer and Overclock's green.")]
        [SerializeField] TrailRenderer trail;
        [SerializeField] MeshRenderer body;
        [Tooltip("Material swapped in for an overclocked shot (roster: green bullets). Per launch, not per asset.")]
        [SerializeField] Material overclockMaterial;
        [SerializeField] Color impactColor = new Color(1f, 0.85f, 0.45f, 1f);
        [SerializeField] float impactBurstScale = 1f;

        Vector3 _direction;
        float _speed;
        float _damage;
        float _age;
        GameObject _owner;
        int _mask;
        float _lifetime;
        float _collisionRadius;
        Material _baseMaterial;

        /// <summary>
        /// Reused by the per-frame sweep. Non-alloc so a bullet in flight costs nothing; projectiles update
        /// one at a time on the main thread, so a shared buffer cannot be read by two of them at once.
        /// </summary>
        static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>Set by <see cref="ProjectilePool"/> when the instance is created.</summary>
        public ObjectPool<Projectile> Pool { get; set; }

        public void OnSpawned()
        {
            _age = 0f;
            _mask = hitMask.value;
            _lifetime = maxLifetime;
            _collisionRadius = radius;

            if (body != null)
            {
                _baseMaterial = body.sharedMaterial;
            }
        }

        public void OnDespawned()
        {
            _direction = Vector3.zero;
            _owner = null;
        }

        /// <summary>
        /// Called by the weapon through the pool: give the bullet its flight parameters. Overrides let one
        /// bullet prefab serve every weapon — a heavy slug is slower with a shorter lifetime (range), a sniper
        /// round is faster and thinner. A non-zero <paramref name="maskOverride"/> retargets the bullet, so the
        /// same prefab also serves enemy fire.
        /// </summary>
        public void Launch(Vector3 direction, float speedOverride, float damageOverride, GameObject owner,
            LayerMask maskOverride = default(LayerMask), float lifetimeOverride = 0f, float radiusOverride = 0f,
            Color trailColor = default(Color), bool overclocked = false)
        {
            _direction = direction.normalized;
            _speed = speedOverride > 0f ? speedOverride : speed;
            _damage = damageOverride > 0f ? damageOverride : damage;
            _owner = owner;
            _age = 0f;
            _lifetime = lifetimeOverride > 0f ? lifetimeOverride : maxLifetime;
            _collisionRadius = radiusOverride > 0f ? radiusOverride : radius;

            if (maskOverride.value != 0)
            {
                _mask = maskOverride.value;
            }

            transform.rotation = Quaternion.LookRotation(_direction);
            transform.localScale = Vector3.one * (_collisionRadius / Mathf.Max(0.01f, radius));

            ApplyVisuals(trailColor, overclocked);
        }

        /// <summary>
        /// Trail and body tint are per-launch state, not a material edit: one prefab serves every weapon and
        /// both sides, so the visual has to come with the shot rather than with the asset.
        /// </summary>
        void ApplyVisuals(Color trailColor, bool overclocked)
        {
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = trailColor.a > 0.01f;
                trail.startColor = trailColor;
            }

            if (body != null && overclockMaterial != null)
            {
                body.sharedMaterial = overclocked ? overclockMaterial : _baseMaterial;
            }
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            Vector3 origin = transform.position;

            int count = Physics.SphereCastNonAlloc(origin, _collisionRadius, _direction, Hits, step, _mask,
                QueryTriggerInteraction.Ignore);

            // The round now starts inside the hero (it leaves the camera axis, so it spawns at the hero's own
            // depth). Ignoring the owner's colliders is what replaces the old spawn gap — a gap that also
            // dodged enemies pressed against the hero.
            int nearest = -1;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_owner != null && Hits[i].collider.transform.IsChildOf(_owner.transform))
                {
                    continue;
                }

                if (Hits[i].distance < nearestDistance)
                {
                    nearestDistance = Hits[i].distance;
                    nearest = i;
                }
            }

            if (nearest >= 0)
            {
                Impact(Hits[nearest].collider, Hits[nearest].point, Hits[nearest].normal);
                return;
            }

            transform.position = origin + _direction * step;
            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                ReturnToPool();
            }
        }

        void Impact(Collider other, Vector3 point, Vector3 normal)
        {
            // The spark fires even when nothing damageable was hit: a bullet that lands on a wall has to read too.
            Vector3 sparkRotation = normal.sqrMagnitude > 0.0001f ? normal : Vector3.up;
            VfxPool.Spawn(point, Quaternion.LookRotation(sparkRotation), impactBurstScale, impactColor);

            IDamageable target = other.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo(_damage, point, normal, _owner));
            ReturnToPool();
        }

        void ReturnToPool()
        {
            if (Pool != null)
            {
                Pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
