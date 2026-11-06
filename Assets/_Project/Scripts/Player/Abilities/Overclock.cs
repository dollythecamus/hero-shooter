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
            // Marker only: the weapon reads this to fire green bullets and a green trail.
            context.Status.AddEffect(StatusEffect.Overclock, Duration, 1f);

            // The burst starts loaded (roster decision, 2026-11-06): the magazine is the limit on this ability,
            // and spending the first second of it reloading read as the ability misbehaving.
            if (context.Weapon != null)
            {
                context.Weapon.RefillMagazine();
            }

            return true;
        }
    }
}
