using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// What makes a ranged enemy ranged and a melee enemy melee. The brain owns everything else —
    /// movement, perception, the state machine — and only asks this strategy to attack.
    /// </summary>
    public interface IEnemyAttack
    {
        /// <summary>Maximum distance at which the Attack state may be entered.</summary>
        float Range { get; }

        /// <summary>Performs one attack. Returns false when still on cooldown.</summary>
        bool TryAttack(Transform target);
    }
}
