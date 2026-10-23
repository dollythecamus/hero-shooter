using System.Collections;
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

        public override void Activate(AbilityContext context)
        {
            Transform target = AimRaycast.FindTarget(context, Range);
            if (target == null)
            {
                return;
            }

            Health health = target.GetComponentInParent<Health>();
            if (health == null)
            {
                return;
            }

            float previous = health.IncomingDamageMultiplier;
            health.IncomingDamageMultiplier = Magnitude;
            context.Host.StartCoroutine(Restore(health, previous, Duration));
        }

        static IEnumerator Restore(Health health, float previous, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (health != null)
            {
                health.IncomingDamageMultiplier = previous;
            }
        }
    }
}
