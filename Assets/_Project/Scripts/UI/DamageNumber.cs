using HeroShooter.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// Free-choice mechanic C: one floating damage number. It is a pooled HUD object — the number is a
    /// world point converted to canvas space every frame, so it sticks to where the hit landed while the
    /// camera moves, then rises and fades.
    /// </summary>
    public class DamageNumber : MonoBehaviour, IPooled
    {
        [SerializeField] Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField] float lifetime = 0.85f;
        [SerializeField] float riseInPixels = 55f;
        [SerializeField] Color hitColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("Heavy hits read in amber, so a Mark-boosted shot is visible at a glance.")]
        [SerializeField] Color heavyHitColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] float heavyHitThreshold = 35f;

        RectTransform _rect;
        Camera _camera;
        DamageNumberPool _pool;
        Vector3 _worldPoint;
        float _age;
        bool _playing;

        void Awake()
        {
            _rect = transform as RectTransform;

            if (label == null)
            {
                label = GetComponent<Text>();
            }

            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        public void Bind(DamageNumberPool pool)
        {
            _pool = pool;
        }

        public void Play(Vector3 worldPoint, float amount)
        {
            _worldPoint = worldPoint;
            _age = 0f;
            _playing = true;

            if (label != null)
            {
                label.text = Mathf.Max(1f, amount).ToString("0");
                label.color = amount >= heavyHitThreshold ? heavyHitColor : hitColor;
            }

            if (group != null)
            {
                group.alpha = 1f;
            }

            Place();
        }

        public void OnSpawned()
        {
            _age = 0f;
        }

        public void OnDespawned()
        {
            _playing = false;
        }

        void LateUpdate()
        {
            if (!_playing)
            {
                return;
            }

            _age += Time.deltaTime;

            if (_age >= lifetime)
            {
                _playing = false;

                if (_pool != null)
                {
                    _pool.Release(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            float normalized = _age / lifetime;

            if (group != null)
            {
                group.alpha = 1f - normalized * normalized;
            }

            Place(normalized * riseInPixels);
        }

        void Place(float rise = 0f)
        {
            if (_rect == null || _rect.parent == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            Vector3 screen = _camera.WorldToScreenPoint(_worldPoint) + new Vector3(0f, rise * Screen.height / 1080f, 0f);

            if (screen.z < 0f)
            {
                if (group != null)
                {
                    group.alpha = 0f;
                }

                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect.parent as RectTransform, screen, null, out Vector2 local))
            {
                _rect.anchoredPosition = local;
            }
        }
    }
}
