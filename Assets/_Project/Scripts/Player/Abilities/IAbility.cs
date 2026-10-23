using HeroShooter.Core;
using UnityEngine;

namespace HeroShooter.Player.Abilities
{
    /// <summary>Everything an ability is allowed to touch. Passed by reference, built once per match.</summary>
    public class AbilityContext
    {
        public Transform Player;
        public Transform CameraTransform;
        public Health Health;
        public PlayerStatus Status;
        public Weapons.Weapon Weapon;
        public MonoBehaviour Host;
        public LayerMask EnemyMask;
        public LayerMask WorldMask;
    }

    /// <summary>
    /// One activated ability. Cooldown and numbers come from the <c>AbilityDefinition</c>; the class
    /// implements only what happens on press.
    /// </summary>
    public interface IAbility
    {
        float Cooldown { get; }
        void Activate(AbilityContext context);
    }
}
