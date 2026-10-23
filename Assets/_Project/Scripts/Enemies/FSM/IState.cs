namespace HeroShooter.Enemies.FSM
{
    /// <summary>
    /// One node of the state machine. States are plain classes, not MonoBehaviours:
    /// they hold no scene data, so the brain stays the only owner of the enemy's dependencies.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
