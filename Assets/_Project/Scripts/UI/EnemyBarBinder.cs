using System;
using System.Collections.Generic;
using HeroShooter.Core;
using HeroShooter.Enemies;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Hands each enemy its pooled world-space bar once, and is the HUD's single source of enemy-side
    /// state: the longest running Mark / Snare for the chips, and a damage event for the hit marker and
    /// the damage numbers.
    ///
    /// Binding is public because the enemy set stopped being static: the arena's hand-placed enemies are
    /// picked up at Start, and <see cref="WaveDirector"/> binds each body as it comes out of its pool. A
    /// brain is bound once even though a pooled body is spawned many times.
    /// </summary>
    public class EnemyBarBinder : MonoBehaviour
    {
        [Header("Authored")]
        [SerializeField] EnemyHealthBarPool pool;

        [Tooltip("Enemies to give a bar to. Left empty, every EnemyBrain in the scene is picked up at Start.")]
        [SerializeField] EnemyBrain[] enemies;

        readonly List<EnemyBrain> _bound = new List<EnemyBrain>();
        readonly List<Health> _boundHealth = new List<Health>();

        /// <summary>Raised for every hit taken by a bound enemy; the HUD compares the source to the hero.</summary>
        public event Action<DamageInfo> EnemyDamaged;

        /// <summary>Longest Mark / Snare window currently running on any enemy, for the HUD chips.</summary>
        public float MarkSecondsLeft { get; private set; }

        public float SnareSecondsLeft { get; private set; }

        void Awake()
        {
            if (pool == null)
            {
                pool = GetComponent<EnemyHealthBarPool>();
            }
        }

        void Start()
        {
            if (enemies == null || enemies.Length == 0)
            {
                enemies = FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            }

            if (pool == null || enemies == null)
            {
                return;
            }

            foreach (EnemyBrain brain in enemies)
            {
                Bind(brain);
            }
        }

        /// <summary>Gives one enemy a bar and starts reporting its damage. Calling it twice is a no-op.</summary>
        public void Bind(EnemyBrain brain)
        {
            if (brain == null || pool == null || _bound.Contains(brain))
            {
                return;
            }

            Health health = brain.Health != null ? brain.Health : brain.GetComponent<Health>();

            if (health == null)
            {
                return;
            }

            _bound.Add(brain);
            _boundHealth.Add(health);
            health.Damaged += OnEnemyDamaged;

            EnemyHealthBar bar = pool.Get(brain.transform.position, brain.transform.rotation);
            bar.Bind(health, brain);
        }

        void Update()
        {
            float mark = 0f;
            float snare = 0f;

            for (int i = 0; i < _bound.Count; i++)
            {
                Health health = _boundHealth[i];
                EnemyBrain brain = _bound[i];

                if (health != null && health.DamageMultiplierSecondsLeft > mark)
                {
                    mark = health.DamageMultiplierSecondsLeft;
                }

                if (brain != null && brain.SpeedMultiplierSecondsLeft > snare)
                {
                    snare = brain.SpeedMultiplierSecondsLeft;
                }
            }

            MarkSecondsLeft = mark;
            SnareSecondsLeft = snare;
        }

        void OnDestroy()
        {
            foreach (Health health in _boundHealth)
            {
                if (health != null)
                {
                    health.Damaged -= OnEnemyDamaged;
                }
            }
        }

        void OnEnemyDamaged(DamageInfo info) => EnemyDamaged?.Invoke(info);
    }
}
