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

        [Header("Cursor")]
        [SerializeField] bool lockCursor = true;

        float _yaw;
        float _pitch;

        /// <summary>The camera this rig drives (weapon aim uses it).</summary>
        public Camera Cam { get; private set; }

        void Awake()
        {
            Cam = GetComponent<Camera>();

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

            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 desiredPosition = pivot - orbit * Vector3.forward * distance;

            transform.position = Vector3.Lerp(transform.position, ResolveCollision(pivot, desiredPosition),
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = orbit;
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
