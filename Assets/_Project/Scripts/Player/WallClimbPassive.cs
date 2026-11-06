using HeroShooter.Data;
using HeroShooter.VFX;
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
        [Tooltip("Metres per second while climbing (roster: 3, raised to 5 after the 2026-11-06 gate found it too slow).")]
        [SerializeField] float climbSpeed = 5f;
        [Tooltip("How high one climb may go, from where it started (roster: 6 m). Overridden by the ability's magnitude.")]
        [SerializeField] float maxHeight = 6f;
        [SerializeField] float wallReach = 0.8f;
        [SerializeField] float eyeHeight = 1.2f;

        [Header("Mantle")]
        [Tooltip("Metres per second of the pull-over at the top. It is a move, not a teleport.")]
        [SerializeField] float mantleSpeed = 6f;

        [Header("Exit jump (reaching the cap)")]
        [Tooltip("Outward speed along the wall normal.")]
        [SerializeField] float exitJumpOut = 4f;
        [Tooltip("Metres of rise from the exit jump (roster: 2 m).")]
        [SerializeField] float exitJumpUp = 2f;

        [Header("References")]
        [Tooltip("Structure layer (index 3) — docs/LAYERS.md. Only geometry on it counts as a wall.")]
        [SerializeField] LayerMask wallMask = 1 << 3;
        [SerializeField] PlayerMotor motor;

        [Header("Feedback")]
        [Tooltip("Seconds between dust puffs while climbing.")]
        [SerializeField] float dustInterval = 0.22f;
        [SerializeField] Color dustColor = new Color(0.86f, 0.83f, 0.76f, 0.85f);

        CharacterController _controller;
        bool _climbing;
        bool _mantling;
        float _climbed;
        float _nextDust;
        Vector3 _wallNormal = Vector3.back;
        Vector3 _mantleFrom;
        Vector3 _mantleTo;
        float _mantleStart;
        float _mantleSeconds = 0.2f;

        /// <summary>True while climbing *or* being pulled over the top — both are the trait doing its work.</summary>
        public bool IsClimbing => _climbing || _mantling;

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
            if (motor == null || _controller == null || Time.timeScale <= 0f)
            {
                return;
            }

            if (_mantling)
            {
                StepMantle();
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

            if (Time.time >= _nextDust)
            {
                _nextDust = Time.time + dustInterval;
                VfxPool.Spawn(transform.position + Vector3.up * 0.25f, 0.5f, dustColor);
            }
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

                // Animate the pull-over instead of teleporting (2026-11-06 gate: the hero "snapped to the top").
                // The controller stays enabled and is walked to the ledge, so collisions still apply and the
                // movement reads as climbing over the lip.
                _mantleFrom = transform.position;
                _mantleTo = hit.point + Vector3.up * 0.05f + intoWall * 0.35f;
                _mantleSeconds = Mathf.Clamp(
                    Vector3.Distance(_mantleFrom, _mantleTo) / Mathf.Max(0.5f, mantleSpeed), 0.08f, 0.4f);
                _mantleStart = Time.time;
                _mantling = true;
                _climbing = false;

                return true;
            }

            return false;
        }

        /// <summary>Walks the hero from the last held position onto the ledge over a fraction of a second.</summary>
        void StepMantle()
        {
            float t = Mathf.Clamp01((Time.time - _mantleStart) / Mathf.Max(0.05f, _mantleSeconds));
            Vector3 target = Vector3.Lerp(_mantleFrom, _mantleTo, Mathf.SmoothStep(0f, 1f, t));
            _controller.Move(target - transform.position);

            if (t >= 1f)
            {
                _mantling = false;
                Stop();
            }
        }

        void ExitJump()
        {
            // Wall normal plus the roster's 2 m of rise: away from the face, not straight up.
            float up = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * exitJumpUp);
            motor.Launch(_wallNormal * exitJumpOut + Vector3.up * up);
            _climbing = false;
            VfxPool.Spawn(transform.position + Vector3.up * 0.2f, 0.7f, dustColor);
        }

        void Stop()
        {
            _climbing = false;
            motor.MovementSuspended = false;
            // A puff marks the end of the climb: letting go, mantling over, or running out of wall.
            VfxPool.Spawn(transform.position + Vector3.up * 0.2f, 0.7f, dustColor);
        }
    }
}
