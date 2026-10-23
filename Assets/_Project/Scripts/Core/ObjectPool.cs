using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroShooter.Core
{
    /// <summary>
    /// Warm-up pool for one prefab type. Objects are created once in <see cref="Awake"/> and
    /// afterwards only activated/deactivated, so firing a weapon never allocates.
    /// Concrete pools subclass this to add a spawn convenience method (see ProjectilePool).
    /// </summary>
    public class ObjectPool<T> : MonoBehaviour where T : Component, IPooled
    {
        [SerializeField] T prefab;

        [Tooltip("Instances built during Awake. Must cover the worst case of one firefight.")]
        [SerializeField] int warmUpCount = 32;

        [Tooltip("Optional parent for pooled instances. Defaults to this transform.")]
        [SerializeField] Transform container;

        readonly Queue<T> _available = new Queue<T>();
        int _created;

        public T Prefab => prefab;
        public int TotalCreated => _created;
        public int ActiveCount => _created - _available.Count;
        public int InactiveCount => _available.Count;

        protected virtual void Awake() => WarmUp(warmUpCount);

        public void WarmUp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _available.Enqueue(CreateInstance());
            }
        }

        /// <summary>Creates one inactive instance. Override to wire a back-reference on the object.</summary>
        protected virtual T CreateInstance()
        {
            T instance = Instantiate(prefab, container != null ? container : transform);
            instance.gameObject.SetActive(false);
            _created++;
            return instance;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = _available.Count > 0 ? _available.Dequeue() : CreateInstance();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.OnSpawned();

            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.OnDespawned();
            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }

        /// <summary>Editor/console readout used by the S1 verification gate.</summary>
        [ContextMenu("Log pool stats")]
        public void LogStats() =>
            Debug.Log($"[{name}] created={_created} active={ActiveCount} inactive={InactiveCount}", this);
    }
}
