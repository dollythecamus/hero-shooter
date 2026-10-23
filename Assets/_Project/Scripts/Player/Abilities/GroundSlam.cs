using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Enemies;
using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Rock's Q: point-blank area burst with knockback. The answer to anything that tries to hug the tank.
    /// </summary>
    public class GroundSlam : AbilityBase
    {
        public GroundSlam(AbilityDefinition definition) : base(definition)
        {
        }

        public override void Activate(AbilityContext context)
        {
            Collider[] hits = Physics.OverlapSphere(context.Player.position, Radius, context.EnemyMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    Vector3 point = hits[i].ClosestPoint(context.Player.position);
                    Vector3 normal = (hits[i].transform.position - context.Player.position).normalized;
                    damageable.TakeDamage(new DamageInfo(Magnitude, point, normal, context.Player.gameObject));
                }

                EnemyBrain brain = hits[i].GetComponentInParent<EnemyBrain>();
                if (brain != null)
                {
                    brain.Push((brain.transform.position - context.Player.position).normalized, 6f);
                }
            }
        }
    }
}
