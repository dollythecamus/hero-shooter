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

        float _damageMultiplierUntil;

        /// <summary>Seconds of Mark left, 0 when not marked. The enemy bar and the HUD chip read this.</summary>
        public float DamageMultiplierSecondsLeft => Mathf.Max(0f, _damageMultiplierUntil - Time.time);

        public bool IsMarked => DamageMultiplierSecondsLeft > 0f;

        /// <summary>
        /// Paper's Mark. A deadline instead of a coroutine in the ability: a second Mark on the same
        /// enemy refreshes the window instead of restoring the first one early (the old restore bug),
        /// and the remaining time is readable, which a chip with a countdown needs.
        /// </summary>
        public void ApplyDamageMultiplier(float magnitude, float duration)
        {
            IncomingDamageMultiplier = magnitude;
            _damageMultiplierUntil = Time.time + Mathf.Max(0.01f, duration);
        }

        void Awake() => Current = maxHealth;

        void Update()
        {
            if (_damageMultiplierUntil > 0f && Time.time >= _damageMultiplierUntil)
            {
                _damageMultiplierUntil = 0f;
                IncomingDamageMultiplier = 1f;
            }
        }

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
