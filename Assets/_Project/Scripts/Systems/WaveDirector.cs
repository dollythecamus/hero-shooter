using System;
using System.Collections;
using System.Collections.Generic;
using HeroShooter.Core;
using HeroShooter.Enemies;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Free-choice mechanic A: escalating waves. Enemies come out of two warm pools (ranged / melee),
    /// placed on the NavMesh in a ring around the hero so no spawn points have to be authored per map,
    /// and each wave is bigger than the last. It reads the same <see cref="Health.Died"/> signal the frag
    /// board uses — a kill counts for the score and for the wave in one place, not two.
    ///
    /// The arena's hand-placed enemies are untouched: this adds a loop, it does not replace the scene.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [Header("Pools")]
        [SerializeField] EnemyPool rangedPool;
        [SerializeField] EnemyPool meleePool;

        [Header("Wave shape")]
        [Tooltip("Enemies in wave 1; every later wave adds sizeGrowthPerWave.")]
        [SerializeField] int firstWaveSize = 3;
        [SerializeField] int sizeGrowthPerWave = 2;
        [SerializeField] int lastWave = 5;
        [Tooltip("Pause between a cleared wave and the next one.")]
        [SerializeField] float breatherSeconds = 5f;
        [Tooltip("Grace period after the map loads before wave 1.")]
        [SerializeField] float startDelay = 3f;
        [Tooltip("How long a corpse stays on the ground before its body is recycled.")]
        [SerializeField] float corpseSeconds = 1.5f;

        [Header("Spawn ring")]
        [SerializeField] float minRadius = 12f;
        [SerializeField] float maxRadius = 20f;
        [SerializeField] float navMeshSnap = 4f;

        /// <summary>Raised when a wave starts, with its number (1-based). The HUD reads the fields instead.</summary>
        public event Action<int> WaveStarted;

        public int Wave { get; private set; }
        public int Alive { get; private set; }
        public bool Breather => _breatherLeft > 0f;
        public float BreatherLeft => _breatherLeft;
        public int LastWave => lastWave;
        public bool Finished => Wave >= lastWave && Alive == 0;

        readonly Dictionary<EnemyBrain, Action<DamageInfo>> _deathHandlers = new Dictionary<EnemyBrain, Action<DamageInfo>>();
        readonly Dictionary<EnemyBrain, EnemyPool> _poolOf = new Dictionary<EnemyBrain, EnemyPool>();
        readonly Dictionary<EnemyBrain, float> _releaseAt = new Dictionary<EnemyBrain, float>();
        readonly List<EnemyBrain> _releasing = new List<EnemyBrain>();

        ScoreService _score;
        HeroLoadout _hero;
        EnemyBarBinder _bars;
        Vector3 _heroStart;
        bool _respawning;
        float _breatherLeft;
        Coroutine _loop;

        void Awake()
        {
            _score = GetComponent<ScoreService>();
        }

        void Start()
        {
            _hero = FindFirstObjectByType<HeroLoadout>();
            _bars = FindFirstObjectByType<EnemyBarBinder>();

            if (_hero != null)
            {
                _loop = StartCoroutine(Run());
            }

            if (_score != null)
            {
                _score.HeroDied += OnHeroDied;
            }
        }

        void OnDestroy()
        {
            if (_score != null)
            {
                _score.HeroDied -= OnHeroDied;
            }
        }

        /// <summary>
        /// A wave game with no respawn leaves a corpse holding the mouse. Two seconds after the hero runs
        /// out of health it is put back on the spawn point with full health, so a bad wave costs progress
        /// instead of the demo.
        /// </summary>
        void OnHeroDied()
        {
            if (_hero == null || _respawning)
            {
                return;
            }

            StartCoroutine(RespawnHero());
        }

        IEnumerator RespawnHero()
        {
            _respawning = true;
            yield return new WaitForSeconds(2f);

            var controller = _hero.GetComponent<CharacterController>();

            // A CharacterController owns the transform; it has to be off for one frame to teleport the body.
            if (controller != null)
            {
                controller.enabled = false;
            }

            _hero.transform.position = _heroStart;

            if (controller != null)
            {
                controller.enabled = true;
            }

            _hero.Health?.ResetHealth();
            _respawning = false;
        }

        IEnumerator Run()
        {
            // One frame first: MatchBootstrap may still be placing the hero on the spawn point during
            // Start, and the respawn should return the hero where the match began, not where the prefab
            // was authored in the scene.
            yield return null;
            _heroStart = _hero.transform.position;

            yield return new WaitForSeconds(startDelay);

            while (Wave < lastWave)
            {
                Wave++;
                SpawnWave(Wave);
                WaveStarted?.Invoke(Wave);

                while (Alive > 0)
                {
                    yield return null;
                }

                if (Wave < lastWave)
                {
                    _breatherLeft = breatherSeconds;

                    while (_breatherLeft > 0f)
                    {
                        _breatherLeft -= Time.deltaTime;
                        yield return null;
                    }

                    _breatherLeft = 0f;
                }
            }

            _loop = null;
        }

        void Update()
        {
            if (_releaseAt.Count == 0)
            {
                return;
            }

            _releasing.Clear();

            foreach (KeyValuePair<EnemyBrain, float> entry in _releaseAt)
            {
                if (Time.time >= entry.Value)
                {
                    _releasing.Add(entry.Key);
                }
            }

            foreach (EnemyBrain enemy in _releasing)
            {
                _releaseAt.Remove(enemy);
                Release(enemy);
            }
        }

        void SpawnWave(int wave)
        {
            int size = firstWaveSize + (wave - 1) * sizeGrowthPerWave;

            for (int i = 0; i < size; i++)
            {
                EnemyPool pool = (i + wave) % 2 == 0 ? rangedPool : meleePool;

                if (pool == null)
                {
                    continue;
                }

                EnemyBrain enemy = pool.Get(FindSpawnPosition(), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

                if (enemy == null)
                {
                    continue;
                }

                // The pool lives under the HUD canvas; a canvas scales its children, so a body that is going
                // to be moved by a NavMeshAgent belongs at the scene root.
                enemy.transform.SetParent(null, true);

                _poolOf[enemy] = pool;
                _score?.Register(enemy.Health);
                _bars?.Bind(enemy);
                Alive++;

                Action<DamageInfo> handler = _ => OnEnemyDied(enemy);
                _deathHandlers[enemy] = handler;
                enemy.Health.Died += handler;
            }
        }

        void OnEnemyDied(EnemyBrain enemy)
        {
            if (!_deathHandlers.TryGetValue(enemy, out Action<DamageInfo> handler))
            {
                return;
            }

            enemy.Health.Died -= handler;
            _deathHandlers.Remove(enemy);
            Alive = Mathf.Max(0, Alive - 1);

            // Leave the body where it fell for a beat, then recycle it.
            _releaseAt[enemy] = Time.time + corpseSeconds;
        }

        void Release(EnemyBrain enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (_poolOf.TryGetValue(enemy, out EnemyPool pool))
            {
                _poolOf.Remove(enemy);
                _score?.Unregister(enemy.Health);
                pool.Release(enemy);
            }
        }

        Vector3 FindSpawnPosition()
        {
            if (_hero == null)
            {
                _hero = FindFirstObjectByType<HeroLoadout>();
            }

            Vector3 origin = _hero != null ? _hero.transform.position : transform.position;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                float radius = UnityEngine.Random.Range(minRadius, maxRadius);
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSnap, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // No NavMesh hit in the ring — fall back to the straight-ahead edge so a wave still arrives.
            return origin + Vector3.forward * maxRadius;
        }
    }
}
