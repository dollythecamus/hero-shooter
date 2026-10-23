using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Weapons
{
    /// <summary>
    /// The projectile pool. It <b>is</b> an <see cref="ObjectPool{T}"/> — the only things it adds are
    /// the back-reference wiring (so a bullet can return itself) and <see cref="Fire"/>.
    /// </summary>
    public class ProjectilePool : ObjectPool<Projectile>
    {
        protected override Projectile CreateInstance()
        {
            Projectile projectile = base.CreateInstance();
            projectile.Pool = this;
            return projectile;
        }

        /// <summary>Spawns one bullet at <paramref name="position"/> travelling along <paramref name="direction"/>.</summary>
        public Projectile Fire(Vector3 position, Vector3 direction, float speed, float damage, GameObject owner)
        {
            Projectile projectile = Get(position, Quaternion.LookRotation(direction));
            projectile.Launch(direction, speed, damage, owner);
            return projectile;
        }
    }
}
