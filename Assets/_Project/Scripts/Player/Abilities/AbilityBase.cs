using HeroShooter.Data;

namespace HeroShooter.Player.Abilities
{
    /// <summary>
    /// Shared plumbing for every ability: keep the definition, expose its cooldown, force subclasses to
    /// implement only <see cref="Activate"/>. No MonoBehaviour — abilities hold no scene state.
    /// </summary>
    public abstract class AbilityBase : IAbility
    {
        protected readonly AbilityDefinition Definition;

        protected AbilityBase(AbilityDefinition definition)
        {
            Definition = definition;
        }

        public float Cooldown => Definition != null && Definition.cooldown > 0f ? Definition.cooldown : 5f;

        protected float Magnitude => Definition != null ? Definition.magnitude : 0f;
        protected float Duration => Definition != null ? Definition.duration : 0f;
        protected float Radius => Definition != null ? Definition.radius : 0f;
        protected float Range => Definition != null ? Definition.range : 0f;

        public abstract bool Activate(AbilityContext context);
    }
}
