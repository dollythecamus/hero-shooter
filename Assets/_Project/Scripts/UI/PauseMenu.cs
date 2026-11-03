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

        ThirdPersonCameraRig _rig;
        bool _open;

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
            if (InputReader.PausePressed)
            {
                if (_open)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            _open = true;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            Time.timeScale = 0f;
            InputReader.SetCursorLocked(false);
            PullSettingsIntoWidgets();
        }

        public void Resume()
        {
            _open = false;

            if (panel != null)
            {
                panel.SetActive(false);
            }

            Time.timeScale = 1f;
            InputReader.SetCursorLocked(true);
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
