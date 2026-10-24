using HeroShooter.Core;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Concrete pool for <see cref="EnemyHealthBar"/>. A generic component cannot be attached to a
    /// GameObject, so every pooled prefab type needs its own one-line subclass (same as ProjectilePool).
    /// </summary>
    public class EnemyHealthBarPool : ObjectPool<EnemyHealthBar>
    {
    }
}
