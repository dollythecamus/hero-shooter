using System.Collections;
using HeroShooter.Data;
using HeroShooter.Enemies;
using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Paper's E: slow the first enemy under the crosshair to a crawl. Paper's answer to anything that
    /// closes distance, since Scissors cannot fight a sniper while rooted.
    /// </summary>
    public class Snare : AbilityBase
    {
        public Snare(AbilityDefinition definition) : base(definition)
        {
        }

        public override bool Activate(AbilityContext context)
        {
            Transform target = AimRaycast.FindTarget(context, Range);
            if (target == null)
            {
                return false;
            }

            EnemyBrain brain = target.GetComponentInParent<EnemyBrain>();
            if (brain == null)
            {
                return false;
            }

            float previous = brain.SpeedMultiplier;
            brain.SpeedMultiplier = Magnitude;
            context.Host.StartCoroutine(Restore(brain, previous, Duration));
            return true;
        }

        static IEnumerator Restore(EnemyBrain brain, float previous, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (brain != null)
            {
                brain.SpeedMultiplier = previous;
            }
        }
    }
}
