using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Match start: hold the roster, farm the hero's configuration out to the loadout, place it on the spawn
    /// point and lock the cursor. Two entry paths — a pick made on the select screen inside this scene
    /// (`90_TestArena`, the dev scene), or a pick carried in from `00_Boot` by <see cref="GameFlow"/>.
    /// </summary>
    public class MatchBootstrap : MonoBehaviour, ICharacterPicker
    {
        [Header("Roster")]
        [SerializeField] CharacterDefinition[] roster;

        [Header("Scene parts")]
        [SerializeField] HeroLoadout hero;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] CharacterSelectUI selectUI;

        [Header("Spawn")]
        [SerializeField] Transform spawnPoint;

        bool _selectionOpen;

        public CharacterDefinition[] Roster => roster;
        public CharacterDefinition Selected { get; private set; }
        public bool SelectionOpen => _selectionOpen;

        void Start()
        {
            GameFlow flow = GameFlow.Instance;

            if (flow != null && flow.SelectedCharacter != null)
            {
                ApplySelection(flow.SelectedCharacter);
                return;
            }

            // No flow and no select screen: the scene was opened standalone in the editor (a map scene has no
            // select canvas), so play it with the first hero instead of freezing the player behind a menu that
            // is not there.
            if (selectUI == null && roster != null && roster.Length > 0)
            {
                ApplySelection(roster[0]);
                return;
            }

            BeginSelection();
        }

        public void BeginSelection()
        {
            _selectionOpen = true;
            Selected = null;

            if (cameraRig != null)
            {
                cameraRig.enabled = false;
            }

            if (hero != null)
            {
                // Freeze the hero: no shooting, no movement input, while the panel is up.
                hero.gameObject.SetActive(false);
            }

            InputReader.SetCursorLocked(false);

            if (selectUI != null)
            {
                selectUI.Show(roster, this);
            }
        }

        /// <summary>Called by the selection panel with the index of the chosen hero.</summary>
        public void Select(int index)
        {
            if (roster == null || index < 0 || index >= roster.Length)
            {
                return;
            }

            ApplySelection(roster[index]);
        }

        public void PickCharacter(int index)
        {
            Select(index);
        }

        void ApplySelection(CharacterDefinition character)
        {
            if (character == null)
            {
                return;
            }

            Selected = character;
            _selectionOpen = false;

            if (hero != null)
            {
                hero.gameObject.SetActive(true);
                hero.Apply(character);

                if (spawnPoint != null)
                {
                    hero.transform.position = spawnPoint.position;
                    hero.transform.rotation = spawnPoint.rotation;
                }
            }

            if (selectUI != null)
            {
                selectUI.Hide();
            }

            if (cameraRig != null)
            {
                cameraRig.enabled = true;
            }

            InputReader.SetCursorLocked(true);
        }
    }
}
