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
            // The caller's target, not our own: an ally that has not spotted the player itself would
            // otherwise stand still for the whole call.
            Transform destination = _brain.CallTarget != null ? _brain.CallTarget : _brain.Target;

            if (destination != null)
            {
                _brain.MoveTo(destination.position, _brain.Stats.chaseSpeed);
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
