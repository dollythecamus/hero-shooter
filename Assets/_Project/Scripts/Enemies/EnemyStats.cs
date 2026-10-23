using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// Tunables for one enemy archetype. A ScriptableObject so ranged and melee differ by data,
    /// not by code, and so the demo can rebalance an enemy live without recompiling anything.
    /// </summary>
    [CreateAssetMenu(menuName = "Hero Shooter/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        public float maxHealth = 60f;

        [Header("Movement")]
        public float searchSpeed = 2.2f;
        public float chaseSpeed = 5f;
        public float fleeSpeed = 6f;

        [Header("Perception")]
        public float sightRange = 20f;
        public float loseSightGrace = 3f;

        [Header("Combat")]
        [Tooltip("Distance at which the Attack state may start. Melee should be small, ranged large.")]
        public float attackRange = 2.2f;
        public float stopDistance = 1.6f;
        public float damage = 8f;
        public float attackCooldown = 1.5f;

        [Header("Flee")]
        [Range(0f, 1f)] public float fleeHealthPercent = 0.25f;
        public float fleeDuration = 2.5f;

        [Header("Ally call")]
        public float allyCallRadius = 16f;
        public float allyCallCooldown = 12f;
        [Tooltip("How long a called ally keeps converging on the caller's target.")]
        public float callAlliesDuration = 4f;
    }
}
