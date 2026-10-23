using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// CharacterController-based movement: camera-relative direction, planar acceleration,
    /// a separate vertical velocity for gravity/jump, and body rotation toward the move direction.
    /// Ported from the shootshoot prototype's PlayerControl, split so input and camera are not its job.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 6f;
        [SerializeField] float sprintMultiplier = 1.6f;
        [SerializeField] float acceleration = 45f;
        [SerializeField] float rotationSpeed = 14f;

        [Header("Jump / gravity")]
        [SerializeField] float jumpHeight = 1.4f;
        [SerializeField] float gravity = -25f;

        [Header("References")]
        [Tooltip("Camera transform used to make movement camera-relative. Falls back to Camera.main.")]
        [SerializeField] Transform cameraTransform;

        CharacterController _controller;
        Vector3 _planarVelocity;
        float _verticalVelocity;

        public bool IsSprinting { get; private set; }
        public float PlanarSpeed => _planarVelocity.magnitude;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        void Update()
        {
            Vector2 input = InputReader.Move;
            Vector3 desired = CameraRelative(input);

            if (desired.sqrMagnitude > 1f)
            {
                desired.Normalize();
            }

            IsSprinting = InputReader.SprintHeld;
            Vector3 targetVelocity = desired * (walkSpeed * (IsSprinting ? sprintMultiplier : 1f));
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, acceleration * Time.deltaTime);

            ApplyGravityAndJump();

            Vector3 motion = _planarVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            if (desired.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        Vector3 CameraRelative(Vector2 input)
        {
            Vector3 forward = cameraTransform != null ? Flatten(cameraTransform.forward) : Flatten(transform.forward);
            Vector3 right = cameraTransform != null ? Flatten(cameraTransform.right) : Flatten(transform.right);
            return forward * input.y + right * input.x;
        }

        void ApplyGravityAndJump()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // Small downward bias keeps the controller glued to slopes and stairs.
                _verticalVelocity = -2f;
            }

            if (InputReader.JumpPressed && _controller.isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _verticalVelocity += gravity * Time.deltaTime;
        }

        static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }
    }
}
