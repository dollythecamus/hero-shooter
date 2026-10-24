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
        /// <summary>
        /// The single shared pool. The weapon falls back to this when its inspector field is empty,
        /// which keeps the player prefab free of a scene reference it could not hold.
        /// </summary>
        public static ProjectilePool Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance == null)
            {
                Instance = this;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        protected override Projectile CreateInstance()
        {
            Projectile projectile = base.CreateInstance();
            projectile.Pool = this;
            return projectile;
        }

        /// <summary>Spawns one bullet at <paramref name="position"/> travelling along <paramref name="direction"/>.</summary>
        public Projectile Fire(Vector3 position, Vector3 direction, float speed, float damage, GameObject owner,
            LayerMask maskOverride = default(LayerMask), float lifetimeOverride = 0f, float radiusOverride = 0f,
            Color trailColor = default(Color), bool overclocked = false)
        {
            Projectile projectile = Get(position, Quaternion.LookRotation(direction));
            projectile.Launch(direction, speed, damage, owner, maskOverride, lifetimeOverride, radiusOverride,
                trailColor, overclocked);
            return projectile;
        }
    }
}
