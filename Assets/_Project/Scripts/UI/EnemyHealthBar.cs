using HeroShooter.Core;
using HeroShooter.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// World-space bar over one enemy. Pooled: the pool builds them at start and this component is never
    /// created per hit. It draws while the enemy is below full health, and always while it is marked —
    /// a mark at full health used to show nothing at all. It never draws through geometry, and tints for
    /// the two states the roster wants visible without reading the console — amber when marked,
    /// blue-grey when snared.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour, IPooled
    {
        [Header("Authored widgets")]
        [SerializeField] Canvas canvas;
        [SerializeField] Image fill;
        [SerializeField] Text statusText;

        [Header("Placement")]
        [Tooltip("Metres above the enemy's pivot.")]
        [SerializeField] float headOffset = 1.9f;

        [Tooltip("Layers that hide the bar. Level geometry only — the roster forbids showing enemies through walls.")]
        [SerializeField] LayerMask blockerMask = 9;

        [Header("Colours")]
        [SerializeField] Color healthyColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        [SerializeField] Color markedColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] Color snaredColor = new Color(0.55f, 0.66f, 0.78f, 1f);

        Health _health;
        EnemyBrain _brain;
        Camera _camera;

        public void Bind(Health health, EnemyBrain brain)
        {
            _health = health;
            _brain = brain;
            _camera = Camera.main;

            if (canvas != null && _camera != null)
            {
                canvas.worldCamera = _camera;
            }
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            _health = null;
            _brain = null;
        }

        void LateUpdate()
        {
            if (_health == null || _camera == null)
            {
                Show(false);
                return;
            }

            // Placement and billboard run even while hidden, so the bar does not slide in late when it appears.
            transform.position = _health.transform.position + Vector3.up * headOffset;
            transform.rotation = _camera.transform.rotation;

            if (!_health.IsAlive || (_health.Normalized >= 0.999f && !_health.IsMarked))
            {
                Show(false);
                return;
            }

            if (Physics.Linecast(_camera.transform.position, transform.position, blockerMask,
                    QueryTriggerInteraction.Ignore))
            {
                Show(false);
                return;
            }

            Show(true);

            if (fill != null)
            {
                fill.fillAmount = _health.Normalized;
                fill.color = _health.IsMarked ? markedColor
                    : _brain != null && _brain.IsSnared ? snaredColor
                    : healthyColor;
            }

            if (statusText != null)
            {
                statusText.text = _health.IsMarked
                    ? $"MARK {_health.DamageMultiplierSecondsLeft:0.0}"
                    : _brain != null && _brain.IsSnared
                        ? $"SNARE {_brain.SpeedMultiplierSecondsLeft:0.0}"
                        : string.Empty;
            }
        }

        void Show(bool visible)
        {
            if (canvas != null && canvas.enabled != visible)
            {
                canvas.enabled = visible;
            }
        }
    }
}
