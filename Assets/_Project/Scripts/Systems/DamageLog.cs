using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Verification helper for the S1 gate: prints who was hit and for how much, so a hit on a
    /// ProBuilder block in the test arena is visible in the console without any UI yet.
    /// Delete it once the HUD exists.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageLog : MonoBehaviour
    {
        [SerializeField] bool logToConsole = true;

        Health _health;

        void Awake() => _health = GetComponent<Health>();

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info)
        {
            if (logToConsole)
            {
                Debug.Log($"[DamageLog] {name} hit for {info.Amount:0.#} → {_health.Current:0.#}/{_health.Max:0.#}", this);
            }
        }

        void OnDied(DamageInfo info)
        {
            if (logToConsole)
            {
                Debug.Log($"[DamageLog] {name} died (source: {(info.Source != null ? info.Source.name : "unknown")})", this);
            }
        }
    }
}
