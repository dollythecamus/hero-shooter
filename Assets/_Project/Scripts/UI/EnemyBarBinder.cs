using System;
using HeroShooter.Core;
using HeroShooter.Enemies;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Hands each hand-placed enemy its pooled world-space bar once, and is the HUD's single source of
    /// enemy-side state: the longest running Mark / Snare for the chips, and a damage event for the hit
    /// marker. It exists because the arena has no spawner — there is no per-enemy spawn hook to hang a bar on.
    /// </summary>
    public class EnemyBarBinder : MonoBehaviour
    {
        [Header("Authored")]
        [SerializeField] EnemyHealthBarPool pool;

        [Tooltip("Enemies to give a bar to. Left empty, every EnemyBrain in the scene is picked up at Start.")]
        [SerializeField] EnemyBrain[] enemies;

        Health[] _healths;

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

            _healths = new Health[enemies.Length];

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyBrain brain = enemies[i];

                if (brain == null)
                {
                    continue;
                }

                Health health = brain.Health != null ? brain.Health : brain.GetComponent<Health>();
                _healths[i] = health;

                if (health != null)
                {
                    health.Damaged += OnEnemyDamaged;
                }

                EnemyHealthBar bar = pool.Get(brain.transform.position, brain.transform.rotation);
                bar.Bind(health, brain);
            }
        }

        void Update()
        {
            if (_healths == null)
            {
                return;
            }

            float mark = 0f;
            float snare = 0f;

            for (int i = 0; i < _healths.Length; i++)
            {
                Health health = _healths[i];
                EnemyBrain brain = enemies[i];

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
            if (_healths == null)
            {
                return;
            }

            foreach (Health health in _healths)
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
