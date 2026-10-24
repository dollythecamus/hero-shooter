using HeroShooter.Core;
using HeroShooter.Player;
using HeroShooter.Weapons;
using UnityEngine;

namespace HeroShooter.Systems
{
    /// <summary>
    /// The tutorial: an ordered list of steps, each one gated by something the player actually did in the scene.
    /// It only reads the existing systems (motor, camera yaw, weapon counter, ability event, target death) — no
    /// gameplay component knows the tutorial exists, so the tutorial cannot change how the game plays.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public enum Gate
        {
            Move = 0,
            Look = 1,
            Shoot = 2,
            Ability = 3,
            Kill = 4
        }

        [System.Serializable]
        public class Step
        {
            public Gate gate = Gate.Move;
            public string title = "";
            [TextArea] public string body = "";

            [Tooltip("Move: metres travelled. Look: degrees of camera yaw. Ignored by the other gates.")]
            public float threshold = 8f;
        }

        [Header("Scene parts")]
        [SerializeField] PlayerMotor motor;
        [SerializeField] Transform view;
        [SerializeField] Weapon weapon;
        [SerializeField] PlayerAbilities abilities;
        [SerializeField] Health[] targets;

        [Header("UI")]
        [SerializeField] TutorialPanelUI panel;

        [Header("Steps")]
        [SerializeField] Step[] steps;

        [Header("Completion")]
        [SerializeField] string doneTitle = "TRAINING COMPLETE";
        [SerializeField] string doneBody = "";
        [Tooltip("Seconds the completion panel stays up before returning to the menu. 0 = stay here.")]
        [SerializeField] float returnDelay = 2.5f;

        Vector3 _lastPosition;
        float _moved;
        float _yawSeen;
        float _previousYaw;
        int _shotsAtStepStart;
        bool _abilityUsed;
        bool _targetKilled;
        bool _returning;
        float _completedAt;

        public int StepIndex { get; private set; }
        public int StepCount => steps != null ? steps.Length : 0;
        public bool Completed { get; private set; }
        public float StepProgress { get; private set; }
        public Gate CurrentGate => steps != null && StepIndex < steps.Length ? steps[StepIndex].gate : Gate.Move;

        void Awake()
        {
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor>();
            if (abilities == null) abilities = FindFirstObjectByType<PlayerAbilities>();
            if (weapon == null) weapon = FindFirstObjectByType<Weapon>();

            if (view == null && Camera.main != null)
            {
                view = Camera.main.transform;
            }

            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] != null)
                    {
                        targets[i].Died += OnTargetDied;
                    }
                }
            }

            if (abilities != null)
            {
                abilities.Activated += OnAbilityActivated;
            }
        }

        void OnDestroy()
        {
            if (abilities != null)
            {
                abilities.Activated -= OnAbilityActivated;
            }

            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] != null)
                    {
                        targets[i].Died -= OnTargetDied;
                    }
                }
            }
        }

        void Start()
        {
            if (steps == null || steps.Length == 0)
            {
                Finish();
                return;
            }

            if (panel != null)
            {
                panel.Show();
            }

            EnterStep(0);
        }

        void Update()
        {
            if (Completed)
            {
                if (!_returning && returnDelay > 0f && Time.time - _completedAt >= returnDelay)
                {
                    _returning = true;

                    if (GameFlow.Instance != null)
                    {
                        GameFlow.Instance.ReturnToMenu();
                    }
                }

                return;
            }

            if (steps == null || StepIndex >= steps.Length)
            {
                return;
            }

            Step step = steps[StepIndex];
            StepProgress = 0f;
            AccumulateTravel();

            switch (step.gate)
            {
                case Gate.Move:
                    StepProgress = _moved / Mathf.Max(0.01f, step.threshold);
                    break;

                case Gate.Look:
                    StepProgress = _yawSeen / Mathf.Max(0.01f, step.threshold);
                    break;

                case Gate.Shoot:
                    StepProgress = weapon != null && weapon.ShotsFired > _shotsAtStepStart ? 1f : 0f;
                    break;

                case Gate.Ability:
                    StepProgress = _abilityUsed ? 1f : 0f;
                    break;

                case Gate.Kill:
                    StepProgress = _targetKilled ? 1f : 0f;
                    break;
            }

            if (StepProgress >= 1f)
            {
                EnterStep(StepIndex + 1);
            }
        }

        void EnterStep(int index)
        {
            StepIndex = index;
            StepProgress = 0f;
            _abilityUsed = false;
            _targetKilled = false;
            _yawSeen = 0f;
            _shotsAtStepStart = weapon != null ? weapon.ShotsFired : 0;
            _moved = 0f;
            _lastPosition = motor != null ? motor.transform.position : Vector3.zero;
            _previousYaw = view != null ? view.eulerAngles.y : 0f;

            if (index >= (steps != null ? steps.Length : 0))
            {
                Finish();
                return;
            }

            if (panel != null)
            {
                panel.SetStep(index, steps.Length, steps[index].title, steps[index].body);
            }
        }

        /// <summary>Leaves for the menu now — the done panel's button, and the same path the auto-return takes.</summary>
        public void ReturnNow()
        {
            if (_returning)
            {
                return;
            }

            _returning = true;

            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.ReturnToMenu();
            }
        }

        void Finish()
        {
            Completed = true;
            _completedAt = Time.time;

            if (panel != null)
            {
                panel.SetComplete(doneTitle, doneBody);
            }
        }

        /// <summary>
        /// Travel while the motor is actually moving under input. Raw position deltas are not used: the scene
        /// placement (spawn teleport) moves the hero before the player touches a key, and that would complete
        /// the step on its own.
        /// </summary>
        void AccumulateTravel()
        {
            if (motor == null)
            {
                return;
            }

            Vector3 delta = motor.transform.position - _lastPosition;
            delta.y = 0f;

            if (motor.PlanarSpeed > 0.3f)
            {
                _moved += delta.magnitude;
            }

            _lastPosition = motor.transform.position;
        }

        void LateUpdate()
        {
            if (view == null || Completed || CurrentGate != Gate.Look)
            {
                return;
            }

            float yaw = view.eulerAngles.y;
            _yawSeen += Mathf.Abs(Mathf.DeltaAngle(_previousYaw, yaw));
            _previousYaw = yaw;
        }

        void OnAbilityActivated(int slot)
        {
            if (!Completed && CurrentGate == Gate.Ability)
            {
                _abilityUsed = true;
            }
        }

        void OnTargetDied(DamageInfo info)
        {
            if (!Completed && CurrentGate == Gate.Kill)
            {
                _targetKilled = true;
            }
        }
    }
}
