using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Scissors' Q: climb straight up for a moment, up to the definition's duration. Implemented as a
    /// status effect, so the motor and the camera need to know nothing about climbing.
    /// </summary>
    public class WallClimb : AbilityBase
    {
        public WallClimb(AbilityDefinition definition) : base(definition)
        {
        }

        public override void Activate(AbilityContext context)
        {
            // Magnitude is the climb speed in m/s (PlayerStatus), so the asset owns the number.
            context.Status.AddEffect(StatusEffect.Climb, Duration, Magnitude);
        }
    }
}
