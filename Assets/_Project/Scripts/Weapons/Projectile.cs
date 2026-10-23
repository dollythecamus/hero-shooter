using HeroShooter.Core;
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

        Vector3 _direction;
        float _speed;
        float _damage;
        float _age;
        GameObject _owner;
        int _mask;

        /// <summary>Set by <see cref="ProjectilePool"/> when the instance is created.</summary>
        public ObjectPool<Projectile> Pool { get; set; }

        public void OnSpawned()
        {
            _age = 0f;
            _mask = hitMask.value;
        }

        public void OnDespawned()
        {
            _direction = Vector3.zero;
            _owner = null;
        }

        /// <summary>
        /// Called by the weapon through the pool: give the bullet its flight parameters. A non-zero
        /// <paramref name="maskOverride"/> replaces the prefab's hit mask, so the same bullet prefab can serve
        /// a player weapon (hits enemies) and an enemy weapon (hits only the player).
        /// </summary>
        public void Launch(Vector3 direction, float speedOverride, float damageOverride, GameObject owner,
            LayerMask maskOverride = default(LayerMask))
        {
            _direction = direction.normalized;
            _speed = speedOverride > 0f ? speedOverride : speed;
            _damage = damageOverride > 0f ? damageOverride : damage;
            _owner = owner;
            _age = 0f;

            if (maskOverride.value != 0)
            {
                _mask = maskOverride.value;
            }

            transform.rotation = Quaternion.LookRotation(_direction);
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            Vector3 origin = transform.position;

            if (Physics.SphereCast(origin, radius, _direction, out RaycastHit hit, step, _mask,
                    QueryTriggerInteraction.Ignore))
            {
                Impact(hit.collider, hit.point, hit.normal);
                return;
            }

            transform.position = origin + _direction * step;
            _age += Time.deltaTime;

            if (_age >= maxLifetime)
            {
                ReturnToPool();
            }
        }

        void Impact(Collider other, Vector3 point, Vector3 normal)
        {
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
