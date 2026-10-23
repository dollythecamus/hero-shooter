using UnityEngine;

namespace HeroShooter.Core
{
    /// <summary>
    /// One hit. Passed by read-only reference so the struct never allocates on the heap.
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;
        public Vector3 Normal;
        public GameObject Source;
        public bool IsCritical;

        public DamageInfo(float amount, Vector3 point, Vector3 normal, GameObject source, bool isCritical = false)
        {
            Amount = amount;
            Point = point;
            Normal = normal;
            Source = source;
            IsCritical = isCritical;
        }
    }

    /// <summary>Anything that can receive damage (player, enemies, props).</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(in DamageInfo info);
    }
}
