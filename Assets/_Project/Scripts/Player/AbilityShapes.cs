using HeroShooter.Data;

namespace HeroShooter.Player
{
    /// <summary>
    /// How a slot is driven. <see cref="PlayerAbilities"/> is the only router: an <c>AbilityKind</c> decides
    /// whether the slot is a button that fires once, a button that is held, or no button at all.
    /// </summary>
    public enum AbilityShape
    {
        None = 0,
        Instant = 1,
        Channel = 2,
        Passive = 3
    }

    /// <summary>
    /// Kind → shape. The three stateful abilities are components (they own per-frame state and, for Slam and
    /// Climb, the controller while the motor stands down), so they are not built by
    /// <c>AbilityFactory</c> — this table is what tells the router that.
    /// </summary>
    public static class AbilityShapes
    {
        public static AbilityShape Of(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.None:
                    return AbilityShape.None;

                // Components: Player/BulwarkShield, Player/SlamDriver, Player/WallClimbPassive.
                case AbilityKind.GroundSlam:
                    return AbilityShape.Instant;
                case AbilityKind.Bulwark:
                    return AbilityShape.Channel;
                case AbilityKind.WallClimb:
                    return AbilityShape.Passive;

                case AbilityKind.MarkTarget:
                case AbilityKind.Snare:
                case AbilityKind.Overclock:
                    return AbilityShape.Instant;

                default:
                    return AbilityShape.None;
            }
        }
    }
}
