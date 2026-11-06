using HeroShooter.Core;
using HeroShooter.Enemies.States;
using HeroShooter.Enemies.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace HeroShooter.Enemies
{
    /// <summary>
    /// One brain, two bodies. Owns the NavMeshAgent, the health, the perception and the state machine,
    /// registers the five states, and decides every transition in one place (<see cref="EvaluateTransitions"/>).
    /// The only thing an archetype adds is its <see cref="IEnemyAttack"/> strategy.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBrain : MonoBehaviour, IPooled
    {
        [Header("Data")]
        [SerializeField] EnemyStats stats;

        [Header("Perception")]
        [SerializeField] Spyglass spyglass;

        [Header("Debug")]
        [Tooltip("Prints every state transition to the console — the S2 verification gate.")]
        [SerializeField] bool logTransitions = true;

        [Header("Knockback")]
        [Tooltip("Seconds the impulse owns the body. Until it ends no state may re-path the agent.")]
        [SerializeField] float knockbackDuration = 0.3f;

        readonly StateMachine _machine = new StateMachine();

        NavMeshAgent _agent;
        Health _health;
        IEnemyAttack _attack;
        Vector3 _spawnPoint;
        float _nextFleeTime;
        float _nextAllyCallTime;
        bool _isDead;
        Vector3 _knockback;
        float _knockbackUntil;

        // Death pose: the capsule child that is tipped over, the face that is hidden, and the values to restore.
        Transform _body;
        Transform _face;
        Vector3 _bodyOrigin;
        Quaternion _bodyRotation = Quaternion.identity;

        public EnemyStats Stats => stats;
        public StateMachine Machine => _machine;
        public Health Health => _health;
        public NavMeshAgent Agent => _agent;
        public IEnemyAttack Attack => _attack;
        public Transform Target => spyglass != null ? spyglass.Target : null;

        /// <summary>
        /// The target the last call we answered pointed at. Set on hearing a call, cleared when the
        /// CallAllies state ends; <see cref="CallAlliesState"/> converges on it instead of on our own
        /// Spyglass target, which an ally behind cover may never have seen.
        /// </summary>
        public Transform CallTarget { get; private set; }
        public Vector3 SpawnPoint => _spawnPoint;
        public bool HasTarget => spyglass != null && spyglass.HasTarget;

        /// <summary>Multiplier on every agent speed the states ask for (Paper's Snare drives this to 0.25).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        float _speedMultiplierUntil;

        /// <summary>Seconds of Snare left, 0 when not snared. The enemy bar and the HUD chip read this.</summary>
        public float SpeedMultiplierSecondsLeft => Mathf.Max(0f, _speedMultiplierUntil - Time.time);

        public bool IsSnared => SpeedMultiplierSecondsLeft > 0f;

        /// <summary>Paper's Snare, on the same deadline scheme as Mark — refreshable and readable.</summary>
        public void ApplySpeedMultiplier(float magnitude, float duration)
        {
            SpeedMultiplier = magnitude;
            _speedMultiplierUntil = Time.time + Mathf.Max(0.01f, duration);
        }

        public float DistanceToTarget => spyglass != null ? spyglass.DistanceToTarget : float.PositiveInfinity;
        public float TimeInState => _machine.TimeInState;

        /// <summary>True while a knockback impulse owns the body — <see cref="MoveTo"/> refuses to path.</summary>
        public bool IsKnockedBack => Time.time < _knockbackUntil;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _attack = GetComponent<IEnemyAttack>();
            _spawnPoint = transform.position;

            _body = transform.Find("Body");
            _face = transform.Find("Face");

            if (_body != null)
            {
                _bodyOrigin = _body.localPosition;
                _bodyRotation = _body.localRotation;
            }

            if (spyglass == null)
            {
                spyglass = GetComponent<Spyglass>();
            }

            if (stats != null)
            {
                _agent.speed = stats.searchSpeed;
                _agent.stoppingDistance = Mathf.Max(0.1f, stats.stopDistance);
                SetHealthToStats();

                if (spyglass != null)
                {
                    spyglass.Initialise(stats.sightRange, stats.loseSightGrace);
                }
            }

            RegisterStates();

            _machine.Transitioned += OnTransitioned;
            AllyCall.Broadcasted += OnAllyCallHeard;
            _health.Died += OnDied;
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.ResetHealth();
            }
        }

        void OnDestroy()
        {
            _machine.Transitioned -= OnTransitioned;
            AllyCall.Broadcasted -= OnAllyCallHeard;

            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        /// <summary>
        /// Re-entry from the pool. Everything OnDied switched off is switched back on here, and the state
        /// machine is dropped back to Search so a recycled body cannot resume the chase that killed it.
        /// </summary>
        public void OnSpawned()
        {
            _isDead = false;
            _spawnPoint = transform.position;
            _knockback = Vector3.zero;
            _knockbackUntil = 0f;
            SpeedMultiplier = 1f;
            _speedMultiplierUntil = 0f;
            _nextFleeTime = 0f;
            _nextAllyCallTime = 0f;
            CallTarget = null;
            RestorePose();

            if (_health != null)
            {
                _health.ResetHealth();
            }

            if (_agent != null)
            {
                // Warp needs an enabled agent, so the order matters: switch it on, then place it.
                _agent.enabled = true;
                _agent.isStopped = false;
                _agent.Warp(transform.position);
            }

            _machine.ChangeState(EnemyState.Search);
        }

        /// <summary>Back into the pool: stop thinking and stop moving, keep the transform where it died.</summary>
        public void OnDespawned()
        {
            _isDead = true;

            if (_agent != null && _agent.enabled)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }
        }

        void Start()
        {
            _machine.ChangeState(EnemyState.Search);
        }

        void Update()
        {
            if (_isDead || stats == null)
            {
                return;
            }

            if (_speedMultiplierUntil > 0f && Time.time >= _speedMultiplierUntil)
            {
                _speedMultiplierUntil = 0f;
                SpeedMultiplier = 1f;
            }

            spyglass.Refresh(stats.sightRange);
            _machine.Tick(Time.deltaTime);
            EvaluateTransitions();

            // The impulse owns the body for its whole duration: agent.Move is the only thing that moves it,
            // and MoveTo refuses to path, so the FSM cannot cancel the displacement on the next frame.
            if (IsKnockedBack)
            {
                if (_agent.enabled)
                {
                    _agent.Move(_knockback * Time.deltaTime);
                }
            }
            else if (_knockback.sqrMagnitude > 0.01f)
            {
                _knockback = Vector3.zero;
            }
        }

        /// <summary>
        /// A corpse must not keep thinking. The FSM is frozen, the agent is switched off so the body stops
        /// moving, and the brain ignores further ally calls. Recycling/destroying the body belongs to the
        /// wave director (S6), not here.
        ///
        /// The body also **falls over** (2026-11-06: an upright body with no health read as a dummy standing
        /// in the way). The pose is restored by <see cref="OnSpawned"/>, so a recycled body comes back upright.
        /// </summary>
        void OnDied(DamageInfo info)
        {
            _isDead = true;
            _agent.isStopped = true;
            _agent.enabled = false;
            StopAllCoroutines();
            TipOver();

            if (logTransitions)
            {
                Debug.Log($"[EnemyBrain] {name}: died in {_machine.Current} (source: " +
                          $"{(info.Source != null ? info.Source.name : "unknown")})", this);
            }
        }

        /// <summary>
        /// Lays the body down: the capsule is tipped onto its side and dropped so it rests on the ground at its
        /// own radius. Measured from the collider rather than the renderer, because renderer bounds update a
        /// frame late after a rotation and the body would visibly sink first.
        /// </summary>
        void TipOver()
        {
            if (_body == null)
            {
                return;
            }

            _body.localRotation = Quaternion.Euler(0f, 0f, 90f);

            CapsuleCollider capsule = _body.GetComponent<CapsuleCollider>();
            float radius = capsule != null ? capsule.radius * _body.lossyScale.x : 0.4f;

            _body.localPosition = new Vector3(_bodyOrigin.x, radius, _bodyOrigin.z);

            if (_face != null)
            {
                // A face floating upright over a fallen body looks worse than no face.
                _face.gameObject.SetActive(false);
            }
        }

        void RestorePose()
        {
            if (_body != null)
            {
                _body.localPosition = _bodyOrigin;
                _body.localRotation = _bodyRotation;
            }

            if (_face != null)
            {
                _face.gameObject.SetActive(true);
            }
        }

        void RegisterStates()
        {
            _machine.Register(EnemyState.Search, new SearchState(this));
            _machine.Register(EnemyState.Chase, new ChaseState(this));
            _machine.Register(EnemyState.Attack, new AttackState(this));
            _machine.Register(EnemyState.Flee, new FleeState(this));
            _machine.Register(EnemyState.CallAllies, new CallAlliesState(this));
        }

        /// <summary>
        /// The whole decision table, in priority order. Reading this method out loud is the S2 explanation.
        /// </summary>
        void EvaluateTransitions()
        {
            if (_machine.Current == EnemyState.Flee || _machine.Current == EnemyState.CallAllies)
            {
                // These two are time boxed and end themselves through EndFlee / EndCallAllies.
                return;
            }

            if (!HasTarget)
            {
                _machine.ChangeState(EnemyState.Search);
                return;
            }

            bool hurtEnoughToRun = _health.Normalized <= stats.fleeHealthPercent && Time.time >= _nextFleeTime;
            if (hurtEnoughToRun)
            {
                _machine.ChangeState(EnemyState.Flee);
                return;
            }

            if (_attack != null && DistanceToTarget <= _attack.Range)
            {
                _machine.ChangeState(EnemyState.Attack);
                return;
            }

            _machine.ChangeState(EnemyState.Chase);
        }

        // ---- services the states use -------------------------------------------------------------

        public void MoveTo(Vector3 worldPosition, float speed)
        {
            if (IsKnockedBack)
            {
                return;
            }

            _agent.speed = speed * SpeedMultiplier;
            _agent.isStopped = false;
            _agent.SetDestination(worldPosition);
        }

        /// <summary>
        /// Impulse away from a hit (Rock's Ground Slam). The brain owns the displacement for
        /// <see cref="knockbackDuration"/>: the path is cleared and the agent stopped, so states cannot
        /// re-path and cancel it — the bug that made knockback invisible in the S2 play-test.
        /// </summary>
        public void Push(Vector3 direction, float force)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _knockback = direction.normalized * force;
            _knockbackUntil = Time.time + knockbackDuration;

            if (_agent.enabled)
            {
                _agent.ResetPath();
                _agent.isStopped = true;
            }
        }

        public void HoldPosition()
        {
            _agent.isStopped = true;
        }

        public void FaceTarget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 flat = target.position - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat),
                    12f * Time.deltaTime);
            }
        }

        /// <summary>Called by the attack states/strategies so nearby enemies come running.</summary>
        public void RequestAllyCall()
        {
            if (stats == null || Target == null || Time.time < _nextAllyCallTime)
            {
                return;
            }

            _nextAllyCallTime = Time.time + stats.allyCallCooldown;
            AllyCall.Broadcast(this, Target);
        }

        void OnAllyCallHeard(EnemyBrain caller, Transform target)
        {
            if (caller == this || _isDead || stats == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, caller.transform.position) > stats.allyCallRadius)
            {
                return;
            }

            if (_machine.Current == EnemyState.Flee || _machine.Current == EnemyState.Attack)
            {
                return;
            }

            CallTarget = target;
            _machine.ChangeState(EnemyState.CallAllies);
        }

        public void EndFlee()
        {
            if (stats != null)
            {
                _nextFleeTime = Time.time + stats.fleeDuration * 2f;
            }

            _machine.ChangeState(HasTarget ? EnemyState.Chase : EnemyState.Search);
    }

        public void EndCallAllies()
        {
            CallTarget = null;
            _machine.ChangeState(HasTarget ? EnemyState.Chase : EnemyState.Search);
        }

        void SetHealthToStats()
        {
            _health.ConfigureMaxHealth(stats.maxHealth);
        }

        void OnTransitioned(EnemyState from, EnemyState to)
        {
            if (!logTransitions)
            {
                return;
            }

            Debug.Log($"[EnemyBrain] {name}: {from} → {to} (dist {DistanceToTarget:0.0})", this);
        }
    }
}
