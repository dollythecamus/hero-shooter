using System;
using UnityEngine;

namespace HeroShooter.Core
{
    /// <summary>
    /// Small health component shared by the player, enemies and test targets.
    /// It owns no visuals and no game flow: it only clamps a value and raises events,
    /// so death handling belongs to whoever subscribed.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] bool destroyOnDeath;

        /// <summary>Raised on every accepted hit, with the hit that caused it.</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>Raised once, when health reaches zero.</summary>
        public event Action<DamageInfo> Died;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(Current / maxHealth);
        public bool IsAlive => Current > 0f;

        /// <summary>
        /// Scales incoming damage: below 1 is armour (Rock's Bulwark), above 1 is vulnerability (Paper's Mark).
        /// Set by whoever applies the effect and restored by it, so Health stays a plain value holder.
        /// </summary>
        public float IncomingDamageMultiplier { get; set; } = 1f;

        void Awake() => Current = maxHealth;

        /// <summary>Restores full health — used when an enemy is recycled from its pool.</summary>
        public void ResetHealth() => Current = maxHealth;

        /// <summary>
        /// Replaces the serialized maximum and refills. Lets an archetype ScriptableObject stay the single
        /// source of truth for its health instead of duplicating the number in every prefab.
        /// </summary>
        public void ConfigureMaxHealth(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            Current = maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || info.Amount <= 0f)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - info.Amount * Mathf.Max(0f, IncomingDamageMultiplier));
            Damaged?.Invoke(info);

            if (Current > 0f)
            {
                return;
            }

            Died?.Invoke(info);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
