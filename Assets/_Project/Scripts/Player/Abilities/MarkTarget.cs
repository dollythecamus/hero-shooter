using HeroShooter.Core;
using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Paper's Q: paint the enemy under the crosshair so it takes more damage for a while.
    /// This is Paper's answer to Rock's large health pool — it does not add damage, it multiplies it.
    /// </summary>
    public class MarkTarget : AbilityBase
    {
        public MarkTarget(AbilityDefinition definition) : base(definition)
        {
        }

        public override bool Activate(AbilityContext context)
        {
            Transform target = AimRaycast.FindTarget(context, Range);
            if (target == null)
            {
                return false;
            }

            Health health = target.GetComponentInParent<Health>();
            if (health == null)
            {
                return false;
            }

            // Health owns the countdown, so a re-mark refreshes the window and the HUD can read the time left.
            health.ApplyDamageMultiplier(Magnitude, Duration);
            return true;
        }
    }
}
