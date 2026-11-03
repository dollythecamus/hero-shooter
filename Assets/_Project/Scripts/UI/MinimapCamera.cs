using UnityEngine;

namespace HeroShooter.UI
{
    /// <summary>
    /// A top-down orthographic camera that rides the hero prefab and writes to a RenderTexture, which the
    /// authored HUD shows through a RawImage (requirement #8, minimap).
    ///
    /// It is parented to the hero so every map gets one for free — no per-scene wiring — but it writes
    /// world position and world rotation every LateUpdate, so the parent's facing never tips the map. The
    /// camera stays bolt upright with world +Z up the screen, which is what makes a single fixed arrow in
    /// the HUD corner the only thing that has to rotate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        [Tooltip("Defaults to the parent (the hero root) when left empty.")]
        [SerializeField] Transform follow;

        [Tooltip("Metres above the hero the map is rendered from. Above the wall tops, below nothing.")]
        [SerializeField] float height = 30f;

        [Tooltip("Half the visible width in metres — 26 shows roughly the arena's middle third.")]
        [SerializeField] float orthographicSize = 26f;

        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();

            if (follow == null && transform.parent != null)
            {
                follow = transform.parent;
            }

            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
        }

        void LateUpdate()
        {
            if (follow == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                follow.position + Vector3.up * height,
                Quaternion.Euler(90f, 0f, 0f));
        }
    }
}
