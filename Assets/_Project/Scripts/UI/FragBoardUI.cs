using HeroShooter.Player;
using HeroShooter.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Shows the authored frag panel while Tab is held (requirement #9). It displays and does nothing else —
    /// the counts come from <see cref="ScoreService"/>, the name from the hero that was picked, and the panel
    /// is a prefab object that is switched on, never built from code.
    /// </summary>
    public class FragBoardUI : MonoBehaviour
    {
        [Header("Authored panel")]
        [SerializeField] GameObject panel;
        [SerializeField] Text nameText;
        [SerializeField] Text killsText;
        [SerializeField] Text deathsText;
        [SerializeField] Text footerText;

        ScoreService _score;
        HeroLoadout _hero;

        /// <summary>Frames this board has updated. Read by the headless probe to prove the player loop reaches it.</summary>
        public static int UpdateCount { get; private set; }

        void Awake()
        {
            _score = GetComponent<ScoreService>();

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        void Update()
        {
            UpdateCount++;
            bool held = InputReader.FragsHeld;

            if (panel != null && panel.activeSelf != held)
            {
                panel.SetActive(held);
            }

            if (!held || _score == null)
            {
                return;
            }

            if (_hero == null)
            {
                _hero = FindFirstObjectByType<HeroLoadout>();
            }

            string heroName = _hero != null && _hero.Current != null ? _hero.Current.displayName.ToUpperInvariant() : "YOU";

            if (nameText != null)
            {
                nameText.text = heroName;
            }

            if (killsText != null)
            {
                killsText.text = _score.Kills.ToString();
            }

            if (deathsText != null)
            {
                deathsText.text = _score.Deaths.ToString();
            }

            if (footerText != null)
            {
                footerText.text = $"ENEMIES ALIVE  {_score.EnemiesAlive}";
            }
        }
    }
}
