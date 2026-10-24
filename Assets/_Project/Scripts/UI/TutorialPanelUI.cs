using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Controller for the authored tutorial panel (`Prefabs/UI/TutorialPanel.prefab`). It receives the current
    /// step from <see cref="TutorialDirector"/> and draws it; it owns no layout and no step logic.
    /// </summary>
    public class TutorialPanelUI : MonoBehaviour
    {
        [Header("Authored widgets")]
        [SerializeField] Canvas canvas;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Text progressText;
        [SerializeField] GameObject doneRoot;

        [Tooltip("Scene-wired: the panel lives in a prefab, the director lives in the scene, so the done button forwards through here.")]
        [SerializeField] TutorialDirector director;

        public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

        public void Show()
        {
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

        /// <summary>Persistent listener on the done panel's button.</summary>
        public void ReturnNow()
        {
            if (director != null)
            {
                director.ReturnNow();
            }
        }

        public void SetStep(int index, int total, string title, string body)
        {
            if (doneRoot != null)
            {
                doneRoot.SetActive(false);
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (progressText != null)
            {
                progressText.text = "STEP " + Mathf.Min(index + 1, total) + " / " + total;
            }
        }

        public void SetComplete(string title, string body)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (progressText != null)
            {
                progressText.text = "";
            }

            if (doneRoot != null)
            {
                doneRoot.SetActive(true);
            }
        }
    }
}
