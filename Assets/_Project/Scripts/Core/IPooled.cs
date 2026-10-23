namespace HeroShooter.Core
{
    /// <summary>
    /// Contract for anything an <see cref="ObjectPool{T}"/> manages.
    /// Raised on the object itself so a pooled object owns its own reset logic.
    /// </summary>
    public interface IPooled
    {
        /// <summary>Called every time the instance leaves the pool and becomes active.</summary>
        void OnSpawned();

        /// <summary>Called every time the instance returns to the pool.</summary>
        void OnDespawned();
    }
}
