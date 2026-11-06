using HeroShooter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Free-choice mechanic E. Esc freezes the match, unlocks the cursor and opens the authored panel;
    /// the same key (or Resume) puts it back. The two settings it exposes are the camera's own fields, so
    /// there is no second copy of "sensitivity" to drift: the slider writes straight into
    /// <see cref="ThirdPersonCameraRig"/>.
    ///
    /// Time.timeScale = 0 rather than a pause flag everywhere: a frozen clock already stops the FSM, the
    /// wave loop and the weapon, and no system needs to know a menu exists.
    ///
    /// The cursor is **owned by the pause state**, not by the key press. The second 2026-11-06 gate found the
    /// mouse free after resuming and Esc behaving like it toggled the wrong way, so the state is now one
    /// boolean that every path (Esc, the Resume button, a scene load) writes through the same setter, and the
    /// cursor is re-derived from it every frame. There is no event left that can miss.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Authored panel")]
        [SerializeField] GameObject panel;
        [SerializeField] Slider sensitivity;
        [SerializeField] Toggle invertY;
        [SerializeField] Button resumeButton;
        [SerializeField] Button quitButton;

        [Header("Sensitivity range, degrees per pixel")]
        [SerializeField] float minSensitivity = 0.03f;
        [SerializeField] float maxSensitivity = 0.35f;

        [Tooltip("Seconds a toggle is immune to the next one: a held key must not open and close the panel.")]
        [SerializeField] float toggleCooldown = 0.15f;

        [Header("Exit")]
        [Tooltip("Loaded by the Main Menu button when there is no GameFlow — e.g. a map played straight from the editor.")]
        [SerializeField] string menuSceneName = "00_Boot";

        ThirdPersonCameraRig _rig;
        bool _open;
        float _nextToggle;
        float _restoreTimeScale = 1f;

        public bool IsOpen => _open;

        void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Quit);
            }

            if (sensitivity != null)
            {
                sensitivity.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (invertY != null)
            {
                invertY.onValueChanged.AddListener(OnInvertChanged);
            }
        }

        void Update()
        {
            if (InputReader.PausePressed && Time.unscaledTime >= _nextToggle)
            {
                _nextToggle = Time.unscaledTime + Mathf.Max(0.02f, toggleCooldown);
                SetPaused(!_open);
                return;
            }

            EnforceCursor();
        }

        public void Pause()
        {
            SetPaused(true);
        }

        public void Resume()
        {
            SetPaused(false);
        }

        /// <summary>The only place the pause state changes, so the panel, the clock and the cursor agree.</summary>
        void SetPaused(bool paused)
        {
            _open = paused;

            if (panel != null && panel.activeSelf != paused)
            {
                panel.SetActive(paused);
            }

            if (paused)
            {
                // Remember what the clock was: the tutorial's completion screen freezes it too, and resuming must
                // not quietly restart a game that something else stopped.
                _restoreTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                PullSettingsIntoWidgets();
            }
            else
            {
                Time.timeScale = _restoreTimeScale > 0f ? _restoreTimeScale : 0f;
            }

            EnforceCursor();
        }

        /// <summary>
        /// Cursor state follows the pause state every frame. It claims the cursor only while the game is running:
        /// any other system that freezes the clock — the tutorial's completion panel — needs a free pointer to be
        /// clickable (2026-11-06: force-locking it made that button unreachable).
        /// </summary>
        void EnforceCursor()
        {
            bool wantFree = _open || Time.timeScale <= 0f;
            CursorLockMode wanted = wantFree ? CursorLockMode.None : CursorLockMode.Locked;

            if (Cursor.lockState != wanted)
            {
                InputReader.SetCursorLocked(!wantFree);
            }
        }

        /// <summary>
        /// The pause panel's Main Menu entry: leaves from any scene, whether or not there is a GameFlow to route
        /// through, so there is always a way out — including out of a tutorial whose own exit has gone wrong.
        /// </summary>
        public void ReturnToMenu()
        {
            _open = false;

            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }

            SceneExit.ReturnToMenu(menuSceneName);
        }

        public void Quit()
        {
            // The editor ignores this on purpose — quitting play mode is the editor's job, not the game's.
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void PullSettingsIntoWidgets()
        {
            ThirdPersonCameraRig rig = ResolveRig();

            if (sensitivity != null && rig != null)
            {
                sensitivity.SetValueWithoutNotify(
                    Mathf.InverseLerp(minSensitivity, maxSensitivity, rig.MouseSensitivity));
            }

            if (invertY != null && rig != null)
            {
                invertY.SetIsOnWithoutNotify(rig.InvertY);
            }
        }

        void OnSensitivityChanged(float value)
        {
            ThirdPersonCameraRig rig = ResolveRig();

            if (rig != null)
            {
                rig.MouseSensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, value);
            }
        }

        void OnInvertChanged(bool value)
        {
            ThirdPersonCameraRig rig = ResolveRig();

            if (rig != null)
            {
                rig.InvertY = value;
            }
        }

        ThirdPersonCameraRig ResolveRig()
        {
            if (_rig == null)
            {
                _rig = FindFirstObjectByType<ThirdPersonCameraRig>();
            }

            return _rig;
        }
    }
}
