using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Controller for the authored main menu (`Prefabs/UI/MainMenu.prefab`) in `00_Boot`. It owns the boot-side
    /// sequence only — menu → character select → map select — and holds no layout: every widget is authored and
    /// every button listener is a persistent listener visible in the prefab.
    /// </summary>
    public class MainMenuUI : MonoBehaviour, ICharacterPicker
    {
        [Header("Authored widgets")]
        [SerializeField] Canvas mainCanvas;
        [SerializeField] Button playButton;

        [Header("Screens")]
        [SerializeField] CharacterSelectUI characterSelect;
        [SerializeField] MapSelectUI mapSelect;

        [Header("Flow")]
        [SerializeField] GameFlow flow;

        void Awake()
        {
            RefreshFlow();
        }

        void Start()
        {
            RefreshFlow();
            InputReader.SetCursorLocked(false);

            if (characterSelect != null)
            {
                characterSelect.Hide();
            }

            if (mapSelect != null)
            {
                mapSelect.Hide();
            }

            ShowMain(true);
        }

        public void Play()
        {
            if (flow == null || flow.Roster == null)
            {
                return;
            }

            ShowMain(false);

            if (characterSelect != null)
            {
                characterSelect.Show(flow.Roster, this);
            }
        }

        /// <summary>The character select card's callback; it hands over the roster index, not the asset.</summary>
        public void PickCharacter(int index)
        {
            if (flow == null || flow.Roster == null || index < 0 || index >= flow.Roster.Length)
            {
                return;
            }

            CharacterDefinition picked = flow.Roster[index];
            flow.SetCharacter(picked);

            if (characterSelect != null)
            {
                characterSelect.Hide();
            }

            if (mapSelect != null)
            {
                mapSelect.Show(flow.Maps, flow, picked);
            }
        }

        public void Tutorial()
        {
            if (flow != null)
            {
                flow.StartTutorial();
            }
        }

        public void Quit()
        {
            if (flow != null)
            {
                flow.Quit();
            }
        }

        void ShowMain(bool visible)
        {
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// The scene's own GameFlow destroys itself when one survived a scene load, so the serialized reference
        /// is a dead object every time we come back from a map. The live singleton always wins.
        /// </summary>
        void RefreshFlow()
        {
            GameFlow live = GameFlow.Instance;

            if (live != null)
            {
                flow = live;
            }
        }
    }
}
