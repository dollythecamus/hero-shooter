using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Player.Abilities;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Owns the two ability slots: builds them from the selected <see cref="CharacterDefinition"/>, runs the
    /// cooldown clocks, reads Q/E, and exposes cooldown ratios for the HUD.
    /// </summary>
    public class PlayerAbilities : MonoBehaviour
    {
        public const int SlotCount = 2;

        [Header("References")]
        [SerializeField] PlayerStatus status;
        [SerializeField] Health health;
        [SerializeField] Weapons.Weapon weapon;

        [Header("Targeting")]
        [Tooltip("Layers an ability may aim at — the Enemy layer.")]
        [SerializeField] LayerMask enemyMask;
        [Tooltip("Layers that block an ability's line of sight — walls and cover.")]
        [SerializeField] LayerMask worldMask;

        readonly IAbility[] _abilities = new IAbility[SlotCount];
        readonly float[] _readyAt = new float[SlotCount];
        AbilityContext _context;

        public AbilityDefinition[] Definitions { get; } = new AbilityDefinition[SlotCount];

        public bool IsReady(int slot) => _abilities[slot] != null && Time.time >= _readyAt[slot];

        /// <summary>Fill amount for the ability bar: 1 = fully ready, 0 = just used.</summary>
        public float CooldownRatio(int slot)
        {
            if (_abilities[slot] == null)
            {
                return 0f;
            }

            float cooldown = Mathf.Max(0.01f, _abilities[slot].Cooldown);
            return 1f - Mathf.Clamp01((_readyAt[slot] - Time.time) / cooldown);
        }

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

            for (int slot = 0; slot < SlotCount; slot++)
            {
                AbilityDefinition definition = character != null ? character.AbilityAt(slot) : null;
                Definitions[slot] = definition;
                _abilities[slot] = AbilityFactory.Create(definition);
                _readyAt[slot] = 0f;
            }
        }

        void Update()
        {
            if (InputReader.Ability1Pressed)
            {
                TryActivate(0);
            }

            if (InputReader.Ability2Pressed)
            {
                TryActivate(1);
            }
        }

        public bool TryActivate(int slot)
        {
            if (slot < 0 || slot >= SlotCount || _abilities[slot] == null || !IsReady(slot) || _context == null)
            {
                return false;
            }

            _abilities[slot].Activate(_context);
            _readyAt[slot] = Time.time + _abilities[slot].Cooldown;
            return true;
        }
    }
}
