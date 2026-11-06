using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// One authored ability readout on the HUD. It holds no <c>Update</c> and reads nothing itself — the
    /// <see cref="HudController"/> binds the definition once and pushes a ratio every frame, which is the
    /// project's "values are pushed, never polled by widgets" rule.
    /// </summary>
    public class AbilitySlotUI : MonoBehaviour
    {
        [Header("Authored widgets")]
        [SerializeField] Text nameText;
        [SerializeField] Text keyText;

        [Tooltip("Instant slots: 0 right after use, 1 when ready.")]
        [SerializeField] GameObject cooldownRoot;
        [SerializeField] Image cooldownFill;

        [Tooltip("Channelled slots only (Bulwark's 5 s reservoir).")]
        [SerializeField] GameObject meterRoot;
        [SerializeField] Image meterFill;

        [SerializeField] Color readyColor = Color.white;
        [SerializeField] Color notReadyColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        /// <summary>Called when the hero (and so the slot's definition) changes, not every frame.</summary>
        public void Bind(AbilityDefinition definition, AbilityShape shape)
        {
            bool present = definition != null && shape != AbilityShape.None;
            gameObject.SetActive(present);

            if (!present)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = definition.displayName;
            }

            // A passive has no key to press: the hint is the word, not "Q".
            if (keyText != null)
            {
                keyText.text = shape == AbilityShape.Passive ? "passive" : definition.slotLabel;
            }

            SetActive(cooldownRoot, shape == AbilityShape.Instant);
            SetActive(meterRoot, shape == AbilityShape.Channel);
        }

        /// <param name="cooldownRatio">1 = ready, 0 = just used.</param>
        /// <param name="meter">0..1 reservoir; 1 for every shape that has none.</param>
        public void Push(float cooldownRatio, float meter, bool ready)
        {
            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = cooldownRatio;
            }

            if (meterFill != null)
            {
                meterFill.fillAmount = meter;
            }

            if (nameText != null)
            {
                nameText.color = ready ? readyColor : notReadyColor;
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
