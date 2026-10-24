using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Enemies;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Rock's Slam in three phases: a fixed-height jump, a forced faster descent, then a landing burst.
    /// It owns the controller while it runs — <see cref="PlayerMotor.MovementSuspended"/> — so there is never a
    /// second <c>CharacterController.Move</c> in the frame, and no steering is possible mid-air by design.
    /// </summary>
    public class SlamDriver : MonoBehaviour
    {
        enum Phase
        {
            Idle,
            Rise,
            Fall
        }

        [Header("Phases")]
        [Tooltip("Apex height above the take-off point (roster: 4 m).")]
        [SerializeField] float riseHeight = 4f;
        [Tooltip("Descent gravity as a multiple of the motor's (roster: 2.5× — it must look forced).")]
        [SerializeField] float descentGravityMultiplier = 2.5f;

        [Header("Impact")]
        [SerializeField] float radius = 5f;
        [SerializeField] float damage = 40f;
        [Tooltip("Impulse handed to every enemy in radius; the brain owns the displacement.")]
        [SerializeField] float knockbackForce = 8f;

        [Header("References")]
        [SerializeField] PlayerMotor motor;
        [SerializeField] LayerMask enemyMask = 1 << 7;

        Phase _phase;
        CharacterController _controller;
        float _verticalVelocity;
        float _takeOffHeight;

        public bool IsActive => _phase != Phase.Idle;
        public float Radius => radius;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }
        }

        /// <summary>Called by the loadout; the definition carries the numbers the roster decided.</summary>
        public void Configure(AbilityDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.radius > 0f)
            {
                radius = definition.radius;
            }

            if (definition.magnitude > 0f)
            {
                damage = definition.magnitude;
            }
        }

        /// <summary>Press. Refused unless the hero is standing on something — this is a grounded start only.</summary>
        public bool TryStart()
        {
            if (_phase != Phase.Idle || motor == null || _controller == null || !motor.IsGrounded)
            {
                return false;
            }

            _phase = Phase.Rise;
            _takeOffHeight = transform.position.y;
            _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * riseHeight);
            motor.MovementSuspended = true;
            return true;
        }

        void Update()
        {
            if (_phase == Phase.Idle)
            {
                return;
            }

            float dt = Time.deltaTime;
            float gravity = Mathf.Abs(motor.Gravity);

            if (_phase == Phase.Rise)
            {
                _verticalVelocity -= gravity * dt;

                if (_verticalVelocity <= 0f)
                {
                    _phase = Phase.Fall;
                }
            }
            else
            {
                _verticalVelocity -= gravity * descentGravityMultiplier * dt;
            }

            _controller.Move(Vector3.up * (_verticalVelocity * dt));

            bool landed = _phase == Phase.Fall && _verticalVelocity < 0f
                          && (transform.position.y <= _takeOffHeight + 0.05f || _controller.isGrounded);

            if (landed)
            {
                Impact();
            }
        }

        void Impact()
        {
            _phase = Phase.Idle;
            _verticalVelocity = 0f;
            motor.MovementSuspended = false;

            Vector3 origin = transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, radius, enemyMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    Vector3 point = hits[i].ClosestPoint(origin);
                    Vector3 normal = (hits[i].transform.position - origin).normalized;
                    damageable.TakeDamage(new DamageInfo(damage, point, normal, gameObject));
                }

                EnemyBrain brain = hits[i].GetComponentInParent<EnemyBrain>();

                if (brain != null)
                {
                    brain.Push((brain.transform.position - origin).normalized, knockbackForce);
                }
            }
        }
    }
}
