using HeroShooter.Enemies.FSM;
using UnityEngine;

namespace HeroShooter.Enemies.States
{
    /// <summary>
    /// Low health: run away from the target for <c>fleeDuration</c>, then hand control back to the brain.
    /// The brain applies a cooldown so the enemy does not stutter between Flee and Chase.
    /// </summary>
    public class FleeState : IState
    {
        readonly EnemyBrain _brain;
        float _endTime;

        public FleeState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _endTime = Time.time + _brain.Stats.fleeDuration;
        }

        public void Tick(float deltaTime)
        {
            Transform target = _brain.Target;

            if (target != null)
            {
                Vector3 away = _brain.transform.position - target.position;
                away.y = 0f;

                if (away.sqrMagnitude < 0.01f)
                {
                    away = -_brain.transform.forward;
                }

                _brain.MoveTo(_brain.transform.position + away.normalized * 8f, _brain.Stats.fleeSpeed);
            }

            if (Time.time >= _endTime)
            {
                _brain.EndFlee();
            }
        }

        public void Exit()
        {
        }
    }
}
