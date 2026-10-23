using HeroShooter.Enemies.FSM;

namespace HeroShooter.Enemies.States
{
    /// <summary>
    /// In range: stop moving, face the target, and let the archetype's attack strategy decide whether
    /// a shot/swing is allowed right now (it owns the cooldown). Ranged enemies call friends from here.
    /// </summary>
    public class AttackState : IState
    {
        readonly EnemyBrain _brain;

        public AttackState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _brain.HoldPosition();
        }

        public void Tick(float deltaTime)
        {
            if (_brain.Target == null || _brain.Attack == null)
            {
                return;
            }

            _brain.FaceTarget(_brain.Target);
            _brain.Attack.TryAttack(_brain.Target);
        }

        public void Exit()
        {
            _brain.Agent.isStopped = false;
        }
    }
}
