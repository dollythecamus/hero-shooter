using HeroShooter.Enemies.FSM;
using UnityEngine;

namespace HeroShooter.Enemies.States
{
    /// <summary>
    /// Answering another enemy's call: converge on the caller's target for a few seconds, then resume
    /// normal behaviour. Being in this state is what makes "chama amigos" visible in the demo.
    /// </summary>
    public class CallAlliesState : IState
    {
        readonly EnemyBrain _brain;
        float _endTime;

        public CallAlliesState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _endTime = Time.time + _brain.Stats.callAlliesDuration;
        }

        public void Tick(float deltaTime)
        {
            if (_brain.Target != null)
            {
                _brain.MoveTo(_brain.Target.position, _brain.Stats.chaseSpeed);
            }

            if (Time.time >= _endTime)
            {
                _brain.EndCallAllies();
            }
        }

        public void Exit()
        {
        }
    }
}
