using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.VFX
{
    /// <summary>
    /// The one VFX pool. Everything visual that is not UI goes through here — bullet impacts, Paper's tracer
    /// sparks, the slam burst, climb dust and the Bulwark pop are the same burst at different scales and
    /// colours, so one prefab covers the whole feedback inventory and the no-runtime-Instantiate rule holds.
    /// </summary>
    public class VfxPool : ObjectPool<VfxOneShot>
    {
        public static VfxPool Instance { get; private set; }

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

        protected override VfxOneShot CreateInstance()
        {
            VfxOneShot instance = base.CreateInstance();
            instance.Bind(this);
            return instance;
        }

        /// <summary>
        /// Fire one burst. Silently does nothing when the scene has no pool: VFX is optional dressing, and a
        /// missing pool must never break a shot or an ability.
        /// </summary>
        public static void Spawn(Vector3 position, Quaternion rotation, float scale, Color color)
        {
            if (Instance == null)
            {
                return;
            }

            VfxOneShot burst = Instance.Get(position, rotation);
            burst.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            burst.SetColor(color);
        }

        public static void Spawn(Vector3 position, float scale, Color color)
        {
            Spawn(position, Quaternion.identity, scale, color);
        }
    }
}
