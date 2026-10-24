using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Player;
using HeroShooter.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Pushes live game state into the authored `Prefabs/UI/HUD.prefab`. The widgets own layout and hold no
    /// logic; this controller owns no layout and holds no state beyond what it needs to avoid redundant writes.
    /// Everything it reads already exists on the hero — nothing here computes gameplay.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Scene parts")]
        [SerializeField] HeroLoadout hero;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] EnemyBarBinder enemyBars;

        [Header("Shown only after a hero is picked")]
        [SerializeField] GameObject contentRoot;

        [Header("Health")]
        [SerializeField] Image healthFill;
        [SerializeField] Text healthText;

        [Header("Ability slots")]
        [SerializeField] AbilitySlotUI slotQ;
        [SerializeField] AbilitySlotUI slotE;

        [Header("Crosshairs")]
        [SerializeField] GameObject hipCrosshair;
        [SerializeField] GameObject aimCrosshair;
        [Tooltip("Paper's fine sniper reticle — swaps in for the shared aim crosshair on that hero only.")]
        [SerializeField] GameObject sniperReticle;

        [Header("Hit marker")]
        [SerializeField] CanvasGroup hitMarker;
        [SerializeField] float hitMarkerSeconds = 0.12f;

        [Header("Status chips")]
        [SerializeField] GameObject markChip;
        [SerializeField] Text markChipText;
        [SerializeField] GameObject snareChip;
        [SerializeField] Text snareChipText;

        [Header("Weapon")]
        [Tooltip("Charge readout — only a charge weapon has anything to draw here.")]
        [SerializeField] GameObject chargeRoot;
        [SerializeField] Image chargeFill;

        readonly AbilityDefinition[] _boundDefinitions = new AbilityDefinition[PlayerAbilities.SlotCount];
        float _hitMarkerUntil;

        void OnEnable()
        {
            if (enemyBars != null)
            {
                enemyBars.EnemyDamaged += OnEnemyDamaged;
            }
        }

        void OnDisable()
        {
            if (enemyBars != null)
            {
                enemyBars.EnemyDamaged -= OnEnemyDamaged;
            }
        }

        void Update()
        {
            // The hero GameObject is switched off until a pick is made, so "a character is applied" is the
            // signal for the whole readout — not the hero object being active.
            CharacterDefinition character = hero != null ? hero.Current : null;
            SetActive(contentRoot, character != null);

            if (character == null)
            {
                SetActive(markChip, false);
                SetActive(snareChip, false);
                SetActive(hipCrosshair, false);
                SetActive(aimCrosshair, false);
                SetActive(sniperReticle, false);
                return;
            }

            PushHealth(hero.Health);
            PushSlots(hero.Abilities);
            PushCrosshairs(character);
            PushWeapon(hero.Weapon);
            PushChips();
            PushHitMarker();
        }

        void PushHealth(Health health)
        {
            if (health == null)
            {
                return;
            }

            if (healthFill != null)
            {
                healthFill.fillAmount = health.Normalized;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
            }
        }

        void PushSlots(PlayerAbilities abilities)
        {
            if (abilities == null)
            {
                return;
            }

            for (int slot = 0; slot < PlayerAbilities.SlotCount; slot++)
            {
                AbilitySlotUI widget = slot == 0 ? slotQ : slotE;

                if (widget == null)
                {
                    continue;
                }

                AbilityDefinition definition = abilities.Definitions[slot];

                if (_boundDefinitions[slot] != definition)
                {
                    _boundDefinitions[slot] = definition;
                    widget.Bind(definition, abilities.ShapeOf(slot));
                }

                widget.Push(abilities.CooldownRatio(slot), abilities.MeterNormalized(slot), abilities.IsReady(slot));
            }
        }

        void PushCrosshairs(CharacterDefinition character)
        {
            bool aiming = cameraRig != null && cameraRig.IsAiming;

            SetActive(hipCrosshair, !aiming);
            SetActive(aimCrosshair, aiming && !character.sniperReticle);
            SetActive(sniperReticle, aiming && character.sniperReticle);
        }

        void PushWeapon(Weapon weapon)
        {
            bool hasCharge = weapon != null && weapon.Definition != null && weapon.Definition.RequiresCharge;

            SetActive(chargeRoot, hasCharge);

            if (hasCharge && chargeFill != null)
            {
                chargeFill.fillAmount = weapon.ChargeRatio;
            }
        }

        void PushChips()
        {
            float mark = enemyBars != null ? enemyBars.MarkSecondsLeft : 0f;
            float snare = enemyBars != null ? enemyBars.SnareSecondsLeft : 0f;

            SetActive(markChip, mark > 0.01f);
            SetActive(snareChip, snare > 0.01f);

            if (markChipText != null)
            {
                markChipText.text = $"MARK {mark:0.0}s";
            }

            if (snareChipText != null)
            {
                snareChipText.text = $"SNARE {snare:0.0}s";
            }
        }

        void PushHitMarker()
        {
            if (hitMarker == null)
            {
                return;
            }

            hitMarker.alpha = Time.time < _hitMarkerUntil ? 1f : 0f;
        }

        void OnEnemyDamaged(DamageInfo info)
        {
            if (hero == null || info.Source == null)
            {
                return;
            }

            // Projectile.DamageInfo carries the firing GameObject; anything under the hero counts as its shot.
            Transform source = info.Source.transform;

            if (source == hero.transform || source.IsChildOf(hero.transform))
            {
                _hitMarkerUntil = Time.time + hitMarkerSeconds;
            }
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
