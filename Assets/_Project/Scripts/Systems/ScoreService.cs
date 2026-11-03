using System;
using System.Collections.Generic;
using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// The match's scoreboard source (requirement #9, Tab frags). It listens to the plain
    /// <see cref="Health.Died"/> events that already exist, so nothing in the enemy or weapon code knows
    /// a scoreboard is being kept — the same reason <see cref="Health"/> raises instead of handling.
    ///
    /// Registration is explicit and public on purpose: the arena's hand-placed enemies are discovered at
    /// Start, and a spawner (waves) registers each enemy it creates, so kill counts keep working when the
    /// enemy set stops being static.
    /// </summary>
    public class ScoreService : MonoBehaviour
    {
        /// <summary>Layers from `docs/LAYERS.md` — a Health on one of these is the hero, on the other an enemy.</summary>
        const int HeroLayer = 6;
        const int EnemyLayer = 7;

        public event Action<Health> EnemyKilled;
        public event Action HeroDied;

        public int Kills { get; private set; }
        public int Deaths { get; private set; }

        /// <summary>Enemies registered and still alive — the board's "enemies alive" line and the wave driver's cue.</summary>
        public int EnemiesAlive => _enemies.Count;

        readonly List<Health> _enemies = new List<Health>();
        readonly Dictionary<Health, Action<DamageInfo>> _handlers = new Dictionary<Health, Action<DamageInfo>>();

        void Start()
        {
            foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                Register(health);
            }
        }

        /// <summary>Starts tracking one Health. Safe to call twice; the hero is recognised by layer, not by name.</summary>
        public void Register(Health health)
        {
            if (health == null || _handlers.ContainsKey(health))
            {
                return;
            }

            if (health.gameObject.layer == HeroLayer)
            {
                Action<DamageInfo> heroHandler = _ => OnHeroDied();
                _handlers[health] = heroHandler;
                health.Died += heroHandler;
                return;
            }

            if (health.gameObject.layer != EnemyLayer)
            {
                return;
            }

            Action<DamageInfo> enemyHandler = _ => OnEnemyDied(health);
            _handlers[health] = enemyHandler;
            health.Died += enemyHandler;
            _enemies.Add(health);
        }

        /// <summary>Stops tracking a Health without scoring it — for an enemy recycled by a spawner.</summary>
        public void Unregister(Health health)
        {
            if (health == null || !_handlers.TryGetValue(health, out Action<DamageInfo> handler))
            {
                return;
            }

            health.Died -= handler;
            _handlers.Remove(health);
            _enemies.Remove(health);
        }

        void OnEnemyDied(Health health)
        {
            Unregister(health);
            Kills++;
            EnemyKilled?.Invoke(health);
        }

        void OnHeroDied()
        {
            Deaths++;
            HeroDied?.Invoke();
        }
    }
}
