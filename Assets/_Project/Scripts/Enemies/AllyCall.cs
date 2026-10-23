using System;
using UnityEngine;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// The "calls friends" mechanic as a one-way broadcast: the caller announces itself and its target,
    /// every enemy brain hears it and decides for itself whether it is close enough to respond.
    /// One static event instead of enemies scanning for each other every frame.
    /// </summary>
    public static class AllyCall
    {
        /// <summary>(caller, caller's target).</summary>
        public static event Action<EnemyBrain, Transform> Broadcasted;

        public static void Broadcast(EnemyBrain caller, Transform target)
        {
            if (caller == null || target == null)
            {
                return;
            }

            Broadcasted?.Invoke(caller, target);
        }

        /// <summary>Called on domain/play-mode exit so stale subscribers never survive a session.</summary>
        public static void Clear()
        {
            Broadcasted = null;
        }
    }
}
