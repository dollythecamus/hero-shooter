using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Player
{
    public enum StatusEffect
    {
        Speed,
        FireRate,
        DamageTaken,
        Climb
    }

    /// <summary>
    /// Timed status effects for the player, and the only place that multiplies speed / fire rate /
    /// incoming damage. Abilities never touch the motor or the weapon directly — they apply an effect
    /// here and it expires on its own.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerStatus : MonoBehaviour
    {
        struct ActiveEffect
        {
            public StatusEffect Kind;
            public float EndTime;
            public float Magnitude;
        }

        [SerializeField] Health health;
        [SerializeField] float climbSpeed = 7f;

        readonly System.Collections.Generic.List<ActiveEffect> _active = new System.Collections.Generic.List<ActiveEffect>();

        CharacterController _controller;
        float _firingSlowUntil;
        float _firingSlowMultiplier = 1f;

        public bool IsClimbing { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float FireRateMultiplier { get; private set; } = 1f;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

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

        /// <summary>Used by weapons that penalise firing (Rock's heavy slug).</summary>
        public void ApplyFiringSlow(float multiplier, float duration)
        {
            _firingSlowMultiplier = multiplier;
            _firingSlowUntil = Time.time + duration;
        }

        void Update()
        {
            SpeedMultiplier = 1f;
            FireRateMultiplier = 1f;
            float damageTaken = 1f;
            float climb = 0f;
            IsClimbing = false;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _active[i].EndTime)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                ActiveEffect effect = _active[i];
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
                    case StatusEffect.Climb:
                        climb = effect.Magnitude;
                        IsClimbing = true;
                        break;
                }
            }

            if (Time.time < _firingSlowUntil)
            {
                SpeedMultiplier *= _firingSlowMultiplier;
            }

            if (health != null)
            {
                health.IncomingDamageMultiplier = damageTaken;
            }

            if (IsClimbing)
            {
                _controller.Move(Vector3.up * (climbSpeed * climb * Time.deltaTime));
            }
        }
    }
}
