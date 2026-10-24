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

        [Tooltip("Supplies the status multipliers (firing slowdown, Overclock, Bulwark).")]
        [SerializeField] PlayerStatus status;

        CharacterController _controller;
        Vector3 _planarVelocity;
        float _verticalVelocity;

        // Set by Launch: the impulse keeps its momentum until the hero lands or steers, which is what makes
        // the wall climb's exit jump actually leave the wall instead of being killed by ground acceleration.
        bool _launched;

        public bool IsSprinting { get; private set; }
        public float PlanarSpeed => _planarVelocity.magnitude;

        /// <summary>The gravity the motor actually uses (m/s², negative) — Slam derives its descent from it.</summary>
        public float Gravity => gravity;

        public bool IsGrounded => _controller != null && _controller.isGrounded;

        /// <summary>
        /// The one-mover rule: while this is true the motor does not call <see cref="CharacterController.Move"/>
        /// at all — no gravity, no input — because another component (Slam's driver, the wall climb) owns the
        /// controller. Whoever sets it must clear it when it is done.
        /// </summary>
        public bool MovementSuspended { get; set; }

        /// <summary>
        /// Hands the motor a velocity and returns control to it: used for the wall climb's exit jump and for
        /// anything that wants to throw the hero without owning the controller for a whole phase.
        /// </summary>
        public void Launch(Vector3 velocity)
        {
            _verticalVelocity = velocity.y;
            _planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _launched = true;
            MovementSuspended = false;
        }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (status == null)
            {
                status = GetComponent<PlayerStatus>();
            }
        }

        /// <summary>Applied by the loadout when a character is selected.</summary>
        public void Configure(float newWalkSpeed, float newSprintMultiplier)
        {
            walkSpeed = newWalkSpeed;
            sprintMultiplier = newSprintMultiplier;
        }

        void Update()
        {
            if (MovementSuspended)
            {
                return;
            }

            Vector2 input = InputReader.Move;
            Vector3 desired = CameraRelative(input);

            if (desired.sqrMagnitude > 1f)
            {
                desired.Normalize();
            }

            IsSprinting = InputReader.SprintHeld;
            float statusMultiplier = status != null ? status.SpeedMultiplier : 1f;
            Vector3 targetVelocity = desired * (walkSpeed * (IsSprinting ? sprintMultiplier : 1f) * statusMultiplier);

            bool steer = desired.sqrMagnitude > 0.01f;

            if (_launched && !steer)
            {
                // Keep the impulse's momentum while airborne: no input means "let the launch finish".
            }
            else
            {
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, acceleration * Time.deltaTime);
            }

            ApplyGravityAndJump();

            Vector3 motion = _planarVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            if (_controller.isGrounded)
            {
                _launched = false;
            }

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
