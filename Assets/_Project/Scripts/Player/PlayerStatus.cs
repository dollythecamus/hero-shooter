using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Player
{
    public enum StatusEffect
    {
        Speed,
        FireRate,
        DamageTaken,

        /// <summary>
        /// Not a multiplier: it marks the hero as overclocked so the weapon can tint bullets green.
        /// <see cref="PlayerStatus.Apply"/> deliberately leaves the multipliers alone for it.
        /// </summary>
        Overclock
    }

    /// <summary>
    /// Timed status effects for the player, and the only place that multiplies speed / fire rate /
    /// incoming damage. It moves nothing: movement belongs to `PlayerMotor` alone (the one-mover rule), which
    /// is why the old `Climb` effect — the second caller of `CharacterController.Move` — is gone.
    /// </summary>
    public class PlayerStatus : MonoBehaviour
    {
        struct ActiveEffect
        {
            public StatusEffect Kind;
            public float EndTime;
            public float Magnitude;
        }

        [SerializeField] Health health;

        readonly System.Collections.Generic.List<ActiveEffect> _active = new System.Collections.Generic.List<ActiveEffect>();

        // Effects held by a channelled ability (Bulwark): applied while the channel runs, dropped on release.
        readonly System.Collections.Generic.List<ActiveEffect> _channels = new System.Collections.Generic.List<ActiveEffect>();

        float _firingSlowUntil;
        float _firingSlowMultiplier = 1f;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float FireRateMultiplier { get; private set; } = 1f;
        public float IncomingDamageMultiplier { get; private set; } = 1f;

        void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        /// <summary>Adds (or refreshes) one effect. Magnitudes multiply, so two slows stack.</summary>
        public void AddEffect(StatusEffect kind, float duration, float magnitude)
        {
            _active.Add(new ActiveEffect { Kind = kind, EndTime = Time.time + duration, Magnitude = magnitude });
        }

        /// <summary>
        /// Begins a channelled effect: it lasts until <see cref="EndChannel"/> for the same kind, not until a
        /// clock runs out. Re-beginning replaces the previous value for that kind.
        /// </summary>
        public void BeginChannel(StatusEffect kind, float magnitude)
        {
            EndChannel(kind);
            _channels.Add(new ActiveEffect { Kind = kind, Magnitude = magnitude });
        }

        public void EndChannel(StatusEffect kind)
        {
            for (int i = _channels.Count - 1; i >= 0; i--)
            {
                if (_channels[i].Kind == kind)
                {
                    _channels.RemoveAt(i);
                }
            }
        }

        /// <summary>Used by weapons that penalise firing (Rock's heavy slug).</summary>
        public void ApplyFiringSlow(float multiplier, float duration)
        {
            _firingSlowMultiplier = multiplier;
            _firingSlowUntil = Time.time + duration;
        }

        /// <summary>True while an effect of this kind is live — the weapon asks this for `Overclock`.</summary>
        public bool HasEffect(StatusEffect kind)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Kind == kind && Time.time < _active[i].EndTime)
                {
                    return true;
                }
            }

            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        void Update()
        {
            SpeedMultiplier = 1f;
            FireRateMultiplier = 1f;
            float damageTaken = 1f;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _active[i].EndTime)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                Apply(_active[i], ref damageTaken);
            }

            for (int i = 0; i < _channels.Count; i++)
            {
                Apply(_channels[i], ref damageTaken);
            }

            if (Time.time < _firingSlowUntil)
            {
                SpeedMultiplier *= _firingSlowMultiplier;
            }

            IncomingDamageMultiplier = damageTaken;

            if (health != null)
            {
                health.IncomingDamageMultiplier = damageTaken;
            }
        }

        void Apply(ActiveEffect effect, ref float damageTaken)
        {
            switch (effect.Kind)
            {
                case StatusEffect.Speed:
                    SpeedMultiplier *= effect.Magnitude;
                    break;
                case StatusEffect.FireRate:
                    FireRateMultiplier *= effect.Magnitude;
                    break;
                case StatusEffect.DamageTaken:
                    damageTaken *= effect.Magnitude;
                    break;
                case StatusEffect.Overclock:
                    break;
            }
        }
    }
}
