using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroShooter.Systems
{
    /// <summary>
    /// The one object that survives a scene load: it holds the roster, the map catalog and the player's two
    /// choices, and it is the only place that calls <see cref="SceneManager.LoadScene"/>. Scene routing lives
    /// here so no screen needs to know another screen's scene name.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [Header("Catalog")]
        [SerializeField] CharacterDefinition[] roster;
        [SerializeField] MapDefinition[] maps;

        [Header("Scenes")]
        [SerializeField] string bootSceneName = "00_Boot";
        [SerializeField] string tutorialSceneName = "10_Tutorial";

        public CharacterDefinition[] Roster => roster;
        public MapDefinition[] Maps => maps;
        public CharacterDefinition SelectedCharacter { get; private set; }
        public MapDefinition SelectedMap { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetCharacter(CharacterDefinition character)
        {
            SelectedCharacter = character;
        }

        public void SetMap(MapDefinition map)
        {
            SelectedMap = map;
        }

        public bool StartMatch()
        {
            if (SelectedCharacter == null || SelectedMap == null || !SelectedMap.IsLoadable)
            {
                return false;
            }

            SceneManager.LoadScene(SelectedMap.sceneName);
            return true;
        }

        public bool StartTutorial()
        {
            // A tutorial has to run with some hero; the first of the roster is the default until one is picked.
            if (SelectedCharacter == null && roster != null && roster.Length > 0)
            {
                SelectedCharacter = roster[0];
            }

            SceneManager.LoadScene(tutorialSceneName);
            return true;
        }

        public void ReturnToMenu()
        {
            InputReader.SetCursorLocked(false);
            SceneManager.LoadScene(bootSceneName);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
