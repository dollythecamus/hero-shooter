using HeroShooter.Data;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Scissors' E: the burst window. Fire rate and speed both go up for a few seconds, which is what
    /// lets a skirmisher delete a sniper who is caught out of position.
    /// </summary>
    public class Overclock : AbilityBase
    {
        public Overclock(AbilityDefinition definition) : base(definition)
        {
        }

        public override bool Activate(AbilityContext context)
        {
            context.Status.AddEffect(StatusEffect.FireRate, Duration, Magnitude);
            context.Status.AddEffect(StatusEffect.Speed, Duration, 1.25f);
            return true;
        }
    }
}
