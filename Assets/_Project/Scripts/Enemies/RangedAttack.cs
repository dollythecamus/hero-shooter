using HeroShooter.Weapons;
using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// Ranged archetype: fires the shared pooled projectile at the player. Damage, cooldown and range come
    /// from the brain's <see cref="EnemyStats"/>; this class only decides where the bullet goes.
    /// </summary>
    public class RangedAttack : MonoBehaviour, IEnemyAttack
    {
        [Header("Spawn")]
        [SerializeField] Transform muzzle;
        [SerializeField] float projectileSpeed = 45f;

        [Header("Range")]
        [SerializeField] float range = 14f;

        [Tooltip("Layers an enemy bullet may hit — set this to the Player layer only.")]
        [SerializeField] LayerMask targetMask;

        EnemyBrain _brain;
        float _nextAttackTime;

        public float Range => range;

        void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
        }

        public bool TryAttack(Transform target)
        {
            if (target == null || Time.time < _nextAttackTime)
            {
                return false;
            }

            EnemyStats stats = _brain != null ? _brain.Stats : null;
            ProjectilePool pool = ProjectilePool.Instance;

            if (stats == null || pool == null)
            {
                return false;
            }

            _nextAttackTime = Time.time + stats.attackCooldown;

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f;
            Vector3 aimPoint = target.position + Vector3.up * 1f;
            pool.Fire(origin, (aimPoint - origin).normalized, projectileSpeed, stats.damage, gameObject, targetMask);

            // "Chama amigos": the first shot of an engagement brings the neighbours.
            if (_brain != null)
            {
                _brain.RequestAllyCall();
            }

            return true;
        }
    }
}
