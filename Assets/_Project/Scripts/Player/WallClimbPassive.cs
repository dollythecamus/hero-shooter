using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Scissors' wall climb as a **passive trait**, not a button: hold jump while touching a wall you are facing
    /// and the hero climbs it at <see cref="climbSpeed"/> for as long as the jump key is held, up to
    /// <see cref="maxHeight"/> measured from where the climb started. Reaching the cap throws the hero away
    /// from the wall; a wall top below the cap is mantled instead. The allowance resets on floor contact only.
    /// It owns the controller while climbing (one-mover rule), so the motor's gravity cannot fight it.
    /// </summary>
    /// Runs before PlayerMotor: the jump key that starts a climb must not also fire the motor's jump, which
    /// would leave the ground and refuse the climb on the same frame.
    [DefaultExecutionOrder(-50)]
    public class WallClimbPassive : MonoBehaviour
    {
        [Header("Climb")]
        [Tooltip("Metres per second while climbing (roster: 3).")]
        [SerializeField] float climbSpeed = 3f;
        [Tooltip("How high one climb may go, from where it started (roster: 6 m). Overridden by the ability's magnitude.")]
        [SerializeField] float maxHeight = 6f;
        [SerializeField] float wallReach = 0.8f;
        [SerializeField] float eyeHeight = 1.2f;

        [Header("Exit jump (reaching the cap)")]
        [Tooltip("Outward speed along the wall normal.")]
        [SerializeField] float exitJumpOut = 4f;
        [Tooltip("Metres of rise from the exit jump (roster: 2 m).")]
        [SerializeField] float exitJumpUp = 2f;

        [Header("References")]
        [Tooltip("Structure layer (index 3) — docs/LAYERS.md. Only geometry on it counts as a wall.")]
        [SerializeField] LayerMask wallMask = 1 << 3;
        [SerializeField] PlayerMotor motor;

        CharacterController _controller;
        bool _climbing;
        float _climbed;
        Vector3 _wallNormal = Vector3.back;

        public bool IsClimbing => _climbing;
        public float Climbed => _climbed;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }
        }

        /// <summary>Called by the loadout: the definition's magnitude is the cap in metres.</summary>
        public void Configure(AbilityDefinition definition)
        {
            if (definition != null && definition.magnitude > 0f)
            {
                maxHeight = definition.magnitude;
            }

            _climbed = 0f;
        }

        void Update()
        {
            if (motor == null || _controller == null)
            {
                return;
            }

            if (!_climbing)
            {
                // One allowance per airborne period: the budget only comes back on floor contact.
                if (motor.IsGrounded && !motor.MovementSuspended)
                {
                    _climbed = 0f;
                }

                if (motor.IsGrounded && !motor.MovementSuspended && _climbed < maxHeight
                    && InputReader.JumpHeld && TryProbe(out _))
                {
                    Begin();
                }

                return;
            }

            Climb();
        }

        void Begin()
        {
            _climbing = true;
            _climbed = 0f;
            motor.MovementSuspended = true;
        }

        void Climb()
        {
            float dt = Time.deltaTime;

            if (!InputReader.JumpHeld)
            {
                Stop();
                return;
            }

            if (_climbed >= maxHeight)
            {
                ExitJump();
                return;
            }

            if (!TryProbe(out _wallNormal))
            {
                // The wall ended under us: mantle over it if there is a top to stand on, otherwise jump off.
                if (!TryMantle())
                {
                    ExitJump();
                }

                return;
            }

            float step = Mathf.Min(climbSpeed * dt, maxHeight - _climbed);
            _controller.Move(Vector3.up * step);
            _climbed += step;
        }

        /// <summary>True when a wall we are facing is within reach. Normal is flattened to horizontal.</summary>
        bool TryProbe(out Vector3 normal)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;

            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, wallReach, wallMask,
                    QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                normal.y = 0f;
                normal.Normalize();
                return true;
            }

            normal = Vector3.zero;
            return false;
        }

        /// <summary>Step onto a wall top that is at or below head height — the "climb over" case in the roster.</summary>
        bool TryMantle()
        {
            Vector3 intoWall = transform.forward;

            // The ledge may start anywhere from just past the face to deep into a thick block, so try a few
            // distances instead of one: the same code has to mantle a 1.5 m cover block and an arena wall.
            float[] offsets = { 0.5f, 1.0f, 1.5f };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 probe = transform.position + Vector3.up * (eyeHeight + 1.2f) + intoWall * offsets[i];

                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 4f, wallMask,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                // Only a top we have already reached is a mantle; something above head height or below our feet
                // is not a ledge to stand on.
                if (hit.point.y > transform.position.y + eyeHeight + 0.5f || hit.point.y < transform.position.y)
                {
                    continue;
                }

                _controller.enabled = false;
                transform.position = hit.point + Vector3.up * 0.05f + intoWall * 0.35f;
                _controller.enabled = true;

                Stop();
                return true;
            }

            return false;
        }

        void ExitJump()
        {
            // Wall normal plus the roster's 2 m of rise: away from the face, not straight up.
            float up = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * exitJumpUp);
            motor.Launch(_wallNormal * exitJumpOut + Vector3.up * up);
            _climbing = false;
        }

        void Stop()
        {
            _climbing = false;
            motor.MovementSuspended = false;
        }
    }
}
