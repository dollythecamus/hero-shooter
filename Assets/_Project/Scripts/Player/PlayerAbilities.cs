using System;
using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Player.Abilities;
using UnityEngine;

namespace HeroShooter.Player
{
    /// Runs before PlayerCombat: a shield raised on the same frame as the fire button must block that shot.
    /// <summary>
    /// Owns the two ability slots and is the only place that routes input to them. A slot's
    /// <see cref="AbilityShape"/> decides everything: an instant ability fires on press and clocks its cooldown
    /// here, a channelled one is delegated to <see cref="BulwarkShield"/> (which owns its own reservoir and
    /// cooldowns), and a passive one is not driven at all — it is a trait on the hero.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerAbilities : MonoBehaviour
    {
        public const int SlotCount = 2;

        [Header("References")]
        [SerializeField] PlayerStatus status;
        [SerializeField] Health health;
        [SerializeField] Weapons.Weapon weapon;

        [Tooltip("Stateful abilities that live as components; left empty they are found on this GameObject.")]
        [SerializeField] BulwarkShield shield;
        [SerializeField] SlamDriver slam;
        [SerializeField] WallClimbPassive climb;

        [Header("Targeting")]
        [Tooltip("Layers an ability may aim at — the Enemy layer.")]
        [SerializeField] LayerMask enemyMask;
        [Tooltip("Layers that block an ability's line of sight — walls and cover.")]
        [SerializeField] LayerMask worldMask;

        readonly IAbility[] _abilities = new IAbility[SlotCount];
        readonly AbilityShape[] _shapes = new AbilityShape[SlotCount];
        readonly float[] _readyAt = new float[SlotCount];
        AbilityContext _context;

        public AbilityDefinition[] Definitions { get; } = new AbilityDefinition[SlotCount];

        /// <summary>Raised with the slot index when an ability actually fired — the tutorial gate and, later, the ultimate meter read this instead of polling cooldowns.</summary>
        public event Action<int> Activated;

        void Awake()
        {
            if (status == null) status = GetComponent<PlayerStatus>();
            if (health == null) health = GetComponent<Health>();
            if (weapon == null) weapon = GetComponent<Weapons.Weapon>();
            if (shield == null) shield = GetComponent<BulwarkShield>();
            if (slam == null) slam = GetComponent<SlamDriver>();
            if (climb == null) climb = GetComponent<WallClimbPassive>();
        }

        public AbilityShape ShapeOf(int slot) => slot >= 0 && slot < SlotCount ? _shapes[slot] : AbilityShape.None;

        public bool IsReady(int slot)
        {
            switch (ShapeOf(slot))
            {
                case AbilityShape.Instant:
                    return (_abilities[slot] != null || IsSlamSlot(slot)) && Time.time >= _readyAt[slot];
                case AbilityShape.Channel:
                    return shield != null && shield.CanRaise;
                case AbilityShape.Passive:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Fill amount for the ability bar: 1 = fully ready, 0 = just used.</summary>
        public float CooldownRatio(int slot)
        {
            switch (ShapeOf(slot))
            {
                case AbilityShape.Instant:
                {
                    if (_abilities[slot] == null && !IsSlamSlot(slot))
                    {
                        return 0f;
                    }

                    float cooldown = CooldownOf(slot);
                    return 1f - Mathf.Clamp01((_readyAt[slot] - Time.time) / cooldown);
                }
                case AbilityShape.Channel:
                    return shield != null ? shield.CooldownRatio : 0f;
                case AbilityShape.Passive:
                    return 1f;
                default:
                    return 0f;
            }
        }

        /// <summary>0..1 reservoir for a channelled slot (Bulwark); 1 for every other shape.</summary>
        public float MeterNormalized(int slot) =>
            ShapeOf(slot) == AbilityShape.Channel && shield != null ? shield.Normalized : 1f;

        public void Configure(CharacterDefinition character)
        {
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;

            _context = new AbilityContext
            {
                Player = transform,
                CameraTransform = cameraTransform,
                Health = health,
                Status = status,
                Weapon = weapon,
                Host = this,
                EnemyMask = enemyMask,
                WorldMask = worldMask
            };

            bool hasClimb = false;

            for (int slot = 0; slot < SlotCount; slot++)
            {
                AbilityDefinition definition = character != null ? character.AbilityAt(slot) : null;
                Definitions[slot] = definition;
                _readyAt[slot] = 0f;

                AbilityKind kind = definition != null ? definition.kind : AbilityKind.None;
                _shapes[slot] = AbilityShapes.Of(kind);

                // Only the instant abilities that are plain C# get a class; the stateful three are components.
                _abilities[slot] = _shapes[slot] == AbilityShape.Instant ? AbilityFactory.Create(definition) : null;

                switch (kind)
                {
                    case AbilityKind.GroundSlam:
                        if (slam != null) slam.Configure(definition);
                        break;
                    case AbilityKind.Bulwark:
                        if (shield != null) shield.Configure(definition);
                        break;
                    case AbilityKind.WallClimb:
                        if (climb != null) climb.Configure(definition);
                        hasClimb = true;
                        break;
                }
            }

            if (climb != null)
            {
                climb.enabled = hasClimb;
            }
        }

        void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            HandleSlot(0, InputReader.Ability1Pressed, InputReader.Ability1Released);
            HandleSlot(1, InputReader.Ability2Pressed, InputReader.Ability2Released);
        }

        void HandleSlot(int slot, bool pressed, bool released)
        {
            if (pressed)
            {
                TryActivate(slot);
            }

            // A channel ends on release; instant slots ignore it because their work already happened on press.
            if (released && ShapeOf(slot) == AbilityShape.Channel && shield != null)
            {
                shield.Release();
            }
        }

        public bool TryActivate(int slot)
        {
            if (slot < 0 || slot >= SlotCount || _context == null)
            {
                return false;
            }

            bool activated;

            switch (_shapes[slot])
            {
                case AbilityShape.Instant:
                {
                    if (Time.time < _readyAt[slot])
                    {
                        return false;
                    }

                    activated = _abilities[slot] != null
                        ? _abilities[slot].Activate(_context)
                        : IsSlamSlot(slot) && slam != null && slam.TryStart();

                    if (activated)
                    {
                        _readyAt[slot] = Time.time + CooldownOf(slot);
                    }

                    break;
                }
                case AbilityShape.Channel:
                    activated = shield != null && shield.TryRaise();
                    break;
                default:
                    return false;
            }

            if (activated)
            {
                Activated?.Invoke(slot);
            }

            return activated;
        }

        bool IsSlamSlot(int slot) =>
            Definitions[slot] != null && Definitions[slot].kind == AbilityKind.GroundSlam;

        float CooldownOf(int slot)
        {
            if (_abilities[slot] != null)
            {
                return Mathf.Max(0.01f, _abilities[slot].Cooldown);
            }

            float cooldown = Definitions[slot] != null ? Definitions[slot].cooldown : 0f;
            return cooldown > 0f ? cooldown : 5f;
        }
    }
}
