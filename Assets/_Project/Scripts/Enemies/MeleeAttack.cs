using System.Collections;
using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// Melee archetype: a telegraphed swing. The wind-up delay is what makes the attack readable
    /// (and dodgeable) instead of instant damage on contact.
    /// </summary>
    public class MeleeAttack : MonoBehaviour, IEnemyAttack
    {
        [Header("Range")]
        [SerializeField] float range = 2.2f;
        [SerializeField] float hitRadius = 2.6f;

        [Header("Timing")]
        [SerializeField] float windUp = 0.35f;

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
            float cooldown = stats != null ? stats.attackCooldown : 1.5f;
            _nextAttackTime = Time.time + cooldown + windUp;

            StartCoroutine(Swing(target, stats != null ? stats.damage : 8f));
            return true;
        }

        IEnumerator Swing(Transform target, float damage)
        {
            yield return new WaitForSeconds(windUp);

            if (target == null)
            {
                yield break;
            }

            if (Vector3.Distance(transform.position, target.position) > hitRadius)
            {
                yield break;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector3 point = target.position + Vector3.up * 1f;
                Vector3 normal = (transform.position - point).normalized;
                damageable.TakeDamage(new DamageInfo(damage, point, normal, gameObject));
            }

            if (_brain != null)
            {
                _brain.RequestAllyCall();
            }
        }
    }
}
