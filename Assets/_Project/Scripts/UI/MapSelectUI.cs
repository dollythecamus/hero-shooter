using HeroShooter.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Controller for the authored map select screen (`Prefabs/UI/MapSelect.prefab`). Same shape as
    /// <see cref="CharacterSelectUI"/>: it fills the authored cards from the catalog and shows/hides the canvas,
    /// and a pick goes straight to <see cref="GameFlow.StartMatch"/>.
    /// </summary>
    public class MapSelectUI : MonoBehaviour
    {
        [Header("Authored widgets")]
        [SerializeField] Canvas canvas;
        [SerializeField] Text titleText;
        [SerializeField] Text heroText;
        [SerializeField] MapCardUI[] cards;

        MapDefinition[] _maps;
        GameFlow _flow;

        public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

        public void Show(MapDefinition[] maps, GameFlow flow, CharacterDefinition chosenHero)
        {
            _maps = maps;
            _flow = flow;

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null)
                    {
                        continue;
                    }

                    if (maps != null && i < maps.Length && maps[i] != null)
                    {
                        cards[i].Bind(maps[i], i, this);
                    }
                    else
                    {
                        cards[i].gameObject.SetActive(false);
                    }
                }
            }

            if (heroText != null)
            {
                heroText.text = chosenHero != null ? "FIGHTING AS " + chosenHero.displayName.ToUpperInvariant() : "";
            }

            if (titleText != null)
            {
                titleText.text = "CHOOSE THE MAP";
            }

            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>Called by a card's button (a persistent listener in the prefab).</summary>
        public void Select(int index)
        {
            if (_maps == null || index < 0 || index >= _maps.Length || _maps[index] == null || _flow == null)
            {
                return;
            }

            // Hide first: the scene load destroys this canvas anyway, but a failed load must not leave two
            // screens stacked on top of each other.
            Hide();
            _flow.SetMap(_maps[index]);

            if (!_flow.StartMatch())
            {
                Debug.LogWarning("[MapSelect] map '" + _maps[index].displayName + "' has no scene name — nothing loaded.");
            }
        }
    }
}
