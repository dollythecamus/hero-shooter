using HeroShooter.Core;
using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Applies a <see cref="CharacterDefinition"/> to the one hero on the field: health, movement, body, weapon
    /// and the two abilities. Selecting a character is exactly this one call — there is no per-character script.
    /// </summary>
    public class HeroLoadout : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] Health health;
        [SerializeField] PlayerMotor motor;
        [SerializeField] Weapons.Weapon weapon;
        [SerializeField] PlayerAbilities abilities;
        [SerializeField] PlayerAppearance appearance;

        public CharacterDefinition Current { get; private set; }

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (weapon == null) weapon = GetComponent<Weapons.Weapon>();
            if (abilities == null) abilities = GetComponent<PlayerAbilities>();
            if (appearance == null) appearance = GetComponent<PlayerAppearance>();
        }

        public void Apply(CharacterDefinition character)
        {
            if (character == null)
            {
                return;
            }

            Current = character;

            if (health != null)
            {
                health.ConfigureMaxHealth(character.maxHealth);
            }

            if (motor != null)
            {
                motor.Configure(character.moveSpeed, character.sprintMultiplier);
            }

            if (weapon != null)
            {
                weapon.SetDefinition(character.weapon);
            }

            if (abilities != null)
            {
                abilities.Configure(character);
            }

            if (appearance != null)
            {
                appearance.Apply(character);
            }
        }
    }
}
