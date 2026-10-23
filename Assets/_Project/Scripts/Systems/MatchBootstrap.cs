using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Match start: hold the roster, freeze the hero until a character is picked, hand the choice to the
    /// loadout, then release the mouse. Also the place S5 will hang map choice off.
    /// </summary>
    public class MatchBootstrap : MonoBehaviour
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

            Selected = roster[index];
            _selectionOpen = false;

            if (hero != null)
            {
                hero.gameObject.SetActive(true);
                hero.Apply(Selected);

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
