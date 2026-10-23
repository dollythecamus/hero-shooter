namespace HeroShooter.Enemies.FSM
{
    /// <summary>Every state an enemy brain can be in. This is the FSM's key set.</summary>
    public enum EnemyState
    {
        None,
        Search,
        Chase,
        Attack,
        Flee,
        CallAllies
    }
}
