using HeroShooter.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// One authored card on the character select screen. It holds its own widget references and knows only how
    /// to fill itself from a <see cref="CharacterDefinition"/> and raise the choice — no layout built in code.
    /// </summary>
    public class CharacterCardUI : MonoBehaviour
    {
        [Header("Widgets")]
        [SerializeField] Image background;
        [SerializeField] Text nameText;
        [SerializeField] Text pitchText;
        [SerializeField] Text abilityText;
        [SerializeField] Button button;

        [Header("Authored wiring")]
        [Tooltip("Index of this card in the roster. Used by the button wired in the prefab.")]
        [SerializeField] int rosterIndex;

        MatchBootstrap _bootstrap;

        /// <summary>
        /// Wired as a persistent listener on the prefab's Button, so the click is visible in the inspector
        /// instead of being added by code. It carries no argument — the card knows its own roster index.
        /// </summary>
        public void Click()
        {
            if (_bootstrap != null)
            {
                _bootstrap.Select(rosterIndex);
            }
        }

        public void Bind(CharacterDefinition character, int index, MatchBootstrap bootstrap)
        {
            if (character == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _bootstrap = bootstrap;
            rosterIndex = index;
            gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.text = character.displayName.ToUpperInvariant();
            }

            if (pitchText != null)
            {
                pitchText.text = character.pitch;
            }

            if (abilityText != null)
            {
                abilityText.text = AbilityLine(character.abilityPrimary, "Q") + "        " +
                                   AbilityLine(character.abilitySecondary, "E");
            }

            if (background != null)
            {
                Color tint = character.color;
                background.color = new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.35f, 0.95f);
            }
        }

        static string AbilityLine(AbilityDefinition ability, string slot)
        {
            if (ability == null)
            {
                return slot + ": —";
            }

            return slot + ": " + ability.displayName + "  (" + ability.cooldown.ToString("0") + "s)";
        }
    }
}
