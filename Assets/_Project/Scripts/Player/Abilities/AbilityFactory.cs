using HeroShooter.Data;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Kind → behaviour, for the **instant** abilities only. The three stateful ones are components
    /// (<c>BulwarkShield</c>, <c>SlamDriver</c>, <c>WallClimbPassive</c>) and are routed by
    /// <c>PlayerAbilities</c> from <c>AbilityShapes</c> — a definition of one of those kinds returns null here.
    /// </summary>
    public static class AbilityFactory
    {
        public static IAbility Create(AbilityDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            switch (definition.kind)
            {
                case AbilityKind.MarkTarget:
                    return new MarkTarget(definition);
                case AbilityKind.Snare:
                    return new Snare(definition);
                case AbilityKind.Overclock:
                    return new Overclock(definition);
                default:
                    return null;
            }
        }
    }
}
