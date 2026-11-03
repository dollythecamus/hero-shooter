using HeroShooter.Core;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// Concrete enemy pool. It exists only because a generic component cannot be added in the inspector —
    /// the behaviour is entirely <see cref="ObjectPool{T}"/>, so enemy bodies are warmed up and recycled
    /// exactly like projectiles and VFX, and `Instantiate` still appears in one file.
    /// </summary>
    public class EnemyPool : ObjectPool<EnemyBrain>
    {
    }
}
