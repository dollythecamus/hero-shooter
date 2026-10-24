using HeroShooter.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// One authored card on the map select screen: it fills itself from a <see cref="MapDefinition"/> and raises
    /// the choice through its owner. Mirrors <see cref="CharacterCardUI"/> — no layout is built in code.
    /// </summary>
    public class MapCardUI : MonoBehaviour
    {
        [Header("Widgets")]
        [SerializeField] Image background;
        [SerializeField] Text nameText;
        [SerializeField] Text blurbText;
        [SerializeField] Text sizeText;
        [SerializeField] Button button;

        [Header("Authored wiring")]
        [Tooltip("Index of this card in the map catalog. Used by the button wired in the prefab.")]
        [SerializeField] int mapIndex;

        MapSelectUI _owner;

        /// <summary>Persistent listener on the prefab's Button; the card knows its own catalog index.</summary>
        public void Click()
        {
            if (_owner != null)
            {
                _owner.Select(mapIndex);
            }
        }

        public void Bind(MapDefinition map, int index, MapSelectUI owner)
        {
            if (map == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _owner = owner;
            mapIndex = index;
            gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.text = map.displayName.ToUpperInvariant();
            }

            if (blurbText != null)
            {
                blurbText.text = map.blurb;
            }

            if (sizeText != null)
            {
                sizeText.text = map.sizeLabel;
            }

            if (background != null)
            {
                Color tint = map.accentColor;
                background.color = new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.35f, 0.95f);
            }
        }
    }
}
