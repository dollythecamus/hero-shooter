using HeroShooter.Data;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Kind → behaviour. This is the whole "abilities are data" claim in one switch: adding an ability means
    /// adding an enum value, a definition asset and one class — no edit to the player prefab or the input code.
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
                case AbilityKind.GroundSlam:
                    return new GroundSlam(definition);
                case AbilityKind.Bulwark:
                    return new Bulwark(definition);
                case AbilityKind.MarkTarget:
                    return new MarkTarget(definition);
                case AbilityKind.Snare:
                    return new Snare(definition);
                case AbilityKind.WallClimb:
                    return new WallClimb(definition);
                case AbilityKind.Overclock:
                    return new Overclock(definition);
                default:
                    return null;
            }
        }
    }
}
