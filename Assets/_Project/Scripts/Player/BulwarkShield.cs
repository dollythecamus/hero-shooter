using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Rock's Bulwark as a **channelled** ability: hold the key to raise the shield, release to lower it.
    /// The reservoir is a first-class resource — it drains while held, refills after a delay, and gates
    /// activation — so it is a component, not a status effect. Cooldowns are owned here too, because a
    /// channel has two ways to end (released vs depleted) and they cost different amounts.
    /// </summary>
    public class BulwarkShield : MonoBehaviour
    {
        [Header("Reservoir")]
        [Tooltip("Seconds of channel available from full (roster: 5 s).")]
        [SerializeField] float maxMeter = 5f;
        [SerializeField] float drainPerSecond = 1f;
        [SerializeField] float refillPerSecond = 1f;
        [Tooltip("Delay between lowering the shield and the reservoir starting to refill (roster: 1 s).")]
        [SerializeField] float refillDelay = 1f;

        [Header("Cooldown")]
        [Tooltip("After an early release (roster: 0.5 s).")]
        [SerializeField] float releaseCooldown = 0.5f;
        [Tooltip("After the reservoir empties on its own (roster: 8 s).")]
        [SerializeField] float depletedCooldown = 8f;

        [Header("Effect")]
        [Tooltip("Incoming damage multiplier while raised (roster: 0.4).")]
        [SerializeField] float damageMultiplier = 0.4f;
        [Tooltip("Movement multiplier while raised (roster: 0.75).")]
        [SerializeField] float speedMultiplier = 0.75f;

        [Header("References")]
        [SerializeField] PlayerStatus status;

        float _readyAt;
        float _refillAt;

        public bool IsRaised { get; private set; }
        public float Meter { get; private set; }
        public float Normalized => maxMeter > 0f ? Mathf.Clamp01(Meter / maxMeter) : 0f;

        /// <summary>True when pressing the key would actually raise the shield.</summary>
        public bool CanRaise => !IsRaised && Meter > 0f && Time.time >= _readyAt;

        /// <summary>Fraction of the cooldown already served, for the HUD (0 = just lowered, 1 = ready).</summary>
        public float CooldownRatio
        {
            get
            {
                float cooldown = Mathf.Max(0.01f, depletedCooldown);
                return 1f - Mathf.Clamp01((_readyAt - Time.time) / cooldown);
            }
        }

        void Awake()
        {
            Meter = maxMeter;

            if (status == null)
            {
                status = GetComponent<PlayerStatus>();
            }
        }

        /// <summary>Called by the loadout. `duration` is the reservoir, `magnitude` the damage multiplier.</summary>
        public void Configure(AbilityDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.duration > 0f)
            {
                maxMeter = definition.duration;
            }

            if (definition.magnitude > 0f)
            {
                damageMultiplier = definition.magnitude;
            }

            Lower();
            Meter = maxMeter;
            _readyAt = 0f;
            _refillAt = 0f;
        }

        /// <summary>Press. Returns false when the reservoir or a cooldown blocks it — no cost either way.</summary>
        public bool TryRaise()
        {
            if (!CanRaise)
            {
                return false;
            }

            IsRaised = true;

            if (status != null)
            {
                status.BeginChannel(StatusEffect.DamageTaken, damageMultiplier);
                status.BeginChannel(StatusEffect.Speed, speedMultiplier);
            }

            return true;
        }

        /// <summary>Release. An early release costs the short cooldown and keeps what is left of the reservoir.</summary>
        public void Release()
        {
            if (!IsRaised)
            {
                return;
            }

            Lower();
            _readyAt = Time.time + releaseCooldown;
        }

        void Update()
        {
            if (IsRaised)
            {
                Meter -= drainPerSecond * Time.deltaTime;

                if (Meter <= 0f)
                {
                    // Depleted: the long cooldown, and the reservoir is empty so refill has to rebuild it.
                    Meter = 0f;
                    Lower();
                    _readyAt = Time.time + depletedCooldown;
                }

                return;
            }

            if (Time.time >= _refillAt)
            {
                Meter = Mathf.Min(maxMeter, Meter + refillPerSecond * Time.deltaTime);
            }
        }

        void Lower()
        {
            IsRaised = false;
            _refillAt = Time.time + refillDelay;

            if (status != null)
            {
                status.EndChannel(StatusEffect.DamageTaken);
                status.EndChannel(StatusEffect.Speed);
            }
        }
    }
}
