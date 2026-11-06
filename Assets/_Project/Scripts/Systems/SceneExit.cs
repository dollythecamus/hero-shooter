using HeroShooter.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroShooter.Systems
{
    /// <summary>
    /// The one way back to the main menu. GameFlow is the normal route, but it only exists when play started from
    /// the boot scene: a map or the tutorial opened straight from the editor has no instance, and every exit
    /// built on that null quietly did nothing — which is exactly what the 2026-11-06 tutorial pass hit when the
    /// completion button "didn't do anything".
    ///
    /// Callers: the tutorial's completion button and its timed exit, and the pause menu's Main Menu entry.
    /// </summary>
    public static class SceneExit
    {
        /// <param name="fallbackSceneName">Loaded when there is no GameFlow. Empty means "do nothing".</param>
        public static void ReturnToMenu(string fallbackSceneName)
        {
            // The menu is not a paused scene: the clock and the cursor are handed back before the load.
            Time.timeScale = 1f;
            InputReader.SetCursorLocked(false);

            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.ReturnToMenu();
                return;
            }

            if (!string.IsNullOrEmpty(fallbackSceneName))
            {
                SceneManager.LoadScene(fallbackSceneName);
            }
        }
    }
}
