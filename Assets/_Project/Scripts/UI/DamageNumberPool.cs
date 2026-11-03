using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Pool for <see cref="DamageNumber"/>. Same warm-up contract as the projectile and VFX pools: a
    /// firefight must not allocate, and a number that has faded is switched off, not destroyed.
    /// </summary>
    public class DamageNumberPool : ObjectPool<DamageNumber>
    {
        public void Show(Vector3 worldPoint, float amount)
        {
            DamageNumber number = Get(worldPoint, Quaternion.identity);

            if (number == null)
            {
                return;
            }

            number.Bind(this);
            number.Play(worldPoint, amount);
        }
    }
}
