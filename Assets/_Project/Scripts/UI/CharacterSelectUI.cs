using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Controller for the authored character select screen (`Prefabs/UI/CharacterSelect.prefab`).
    /// It owns no layout: the canvas, title and the three cards are authored assets with their own widgets,
    /// and this script only fills those cards from the roster and shows/hides the canvas.
    /// Rule for the whole project: UI is authored as prefabs, never built at runtime.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("Authored widgets")]
        [SerializeField] Canvas canvas;
        [SerializeField] CharacterCardUI[] cards;

        public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

        public void Show(CharacterDefinition[] roster, ICharacterPicker picker)
        {
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null)
                    {
                        continue;
                    }

                    if (roster != null && i < roster.Length && roster[i] != null)
                    {
                        cards[i].Bind(roster[i], i, picker);
                    }
                    else
                    {
                        cards[i].gameObject.SetActive(false);
                    }
                }
            }

            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
            }

            InputReader.SetCursorLocked(false);
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }
    }
}
