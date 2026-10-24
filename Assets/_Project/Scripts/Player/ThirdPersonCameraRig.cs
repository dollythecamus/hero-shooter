using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Third-person orbit camera: yaw/pitch around a point above the target, exponentially
    /// smoothed follow, and a sphere-cast pull-in so geometry never ends up between the camera
    /// and the player. This is requirement "camera do player em third person".
    /// </summary>
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Orbit")]
        [SerializeField] float distance = 4.5f;
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] Vector2 pitchLimits = new Vector2(-35f, 70f);
        [SerializeField] float followSharpness = 20f;

        [Header("Collision")]
        [SerializeField] LayerMask collisionMask = ~0;
        [SerializeField] float collisionRadius = 0.25f;

        [Header("Aim (roster: hip 60 → 42, blend 8/s, +0.55 right / −0.20 down)")]
        [Tooltip("Field of view while RMB is held.")]
        [SerializeField] float aimFov = 42f;
        [Tooltip("Exponential blend rate toward the aim state, per second.")]
        [SerializeField] float aimBlendPerSecond = 8f;
        [Tooltip("Shoulder offset applied in camera space while aiming.")]
        [SerializeField] Vector3 aimOffset = new Vector3(0.55f, -0.20f, 0f);

        [Header("Cursor")]
        [SerializeField] bool lockCursor = true;

        float _yaw;
        float _pitch;
        float _aimBlend;
        float _hipFov = 60f;

        /// <summary>The camera this rig drives (weapon aim uses it).</summary>
        public Camera Cam { get; private set; }

        /// <summary>0 at the hip, 1 fully aiming. The HUD swaps crosshairs on this, not on the raw button.</summary>
        public float AimBlend => _aimBlend;

        public bool IsAiming => _aimBlend > 0.5f;

        void Awake()
        {
            Cam = GetComponent<Camera>();

            // The scene's camera value is the hip FOV: authoring it once beats two numbers that can disagree.
            if (Cam != null)
            {
                _hipFov = Cam.fieldOfView;
            }

            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        void Start()
        {
            if (lockCursor)
            {
                InputReader.SetCursorLocked(true);
            }
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 look = InputReader.Look;
            _yaw += look.x * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, pitchLimits.x, pitchLimits.y);

            _aimBlend = Mathf.Lerp(_aimBlend, InputReader.AimHeld ? 1f : 0f,
                1f - Mathf.Exp(-aimBlendPerSecond * Time.deltaTime));

            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = target.position + pivotOffset + orbit * (aimOffset * _aimBlend);
            Vector3 desiredPosition = pivot - orbit * Vector3.forward * distance;

            transform.position = Vector3.Lerp(transform.position, ResolveCollision(pivot, desiredPosition),
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = orbit;

            if (Cam != null)
            {
                Cam.fieldOfView = Mathf.Lerp(_hipFov, aimFov, _aimBlend);
            }
        }

        /// <summary>Shorten the boom when something solid sits between the pivot and the wanted position.</summary>
        Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 toCamera = desiredPosition - pivot;
            float length = toCamera.magnitude;

            if (length <= 0.01f)
            {
                return desiredPosition;
            }

            Vector3 direction = toCamera / length;

            if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit, length, collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return pivot + direction * Mathf.Max(0.2f, hit.distance - collisionRadius);
            }

            return desiredPosition;
        }
    }
}
