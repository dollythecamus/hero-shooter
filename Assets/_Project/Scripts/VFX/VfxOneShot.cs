using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.VFX
{
    /// <summary>
    /// One pooled particle burst. The pool builds these at start and they are only ever activated and
    /// deactivated, so a firefight produces no allocations — the same rule bullets follow.
    /// A burst returns itself after <see cref="lifetime"/>; nothing else releases it.
    /// </summary>
    public class VfxOneShot : MonoBehaviour, IPooled
    {
        [Header("Authored")]
        [SerializeField] ParticleSystem particles;

        [Tooltip("Seconds before this instance goes back to the pool. Must cover the particle lifetime.")]
        [SerializeField] float lifetime = 0.7f;

        ObjectPool<VfxOneShot> _pool;
        float _releaseAt;

        /// <summary>Set by <see cref="VfxPool"/> when the instance is created.</summary>
        public void Bind(ObjectPool<VfxOneShot> pool)
        {
            _pool = pool;
        }

        /// <summary>Per-burst colour — one prefab serves sparks, dust, slam and the shield pop.</summary>
        public void SetColor(Color color)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
        }

        public void OnSpawned()
        {
            _releaseAt = Time.time + lifetime;

            if (particles != null)
            {
                particles.Play(true);
            }
        }

        public void OnDespawned()
        {
            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void Update()
        {
            if (_pool != null && Time.time >= _releaseAt)
            {
                _pool.Release(this);
            }
        }
    }
}
