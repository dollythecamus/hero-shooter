using HeroShooter.Enemies.FSM;

namespace HeroShooter.Enemies.States
{
    /// <summary>Target known but out of attack range: path straight at it at chase speed.</summary>
    public class ChaseState : IState
    {
        readonly EnemyBrain _brain;

        public ChaseState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            if (_brain.Target == null)
            {
                return;
            }

            _brain.MoveTo(_brain.Target.position, _brain.Stats.chaseSpeed);
        }

        public void Exit()
        {
        }
    }
}
