using HeroShooter.Data;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Rock's E: temporarily take less damage at the cost of speed. Expressed purely as status effects,
    /// so it expires on its own and stacks with anything else (including Rock's firing slowdown).
    /// </summary>
    public class Bulwark : AbilityBase
    {
        public Bulwark(AbilityDefinition definition) : base(definition)
        {
        }

        public override void Activate(AbilityContext context)
        {
            context.Status.AddEffect(StatusEffect.DamageTaken, Duration, Magnitude);
            context.Status.AddEffect(StatusEffect.Speed, Duration, 0.75f);
        }
    }
}
