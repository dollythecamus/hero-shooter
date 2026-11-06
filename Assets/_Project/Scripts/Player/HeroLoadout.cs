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

        [Tooltip("The scene camera rig, for the per-character aim FOV. Found at runtime when left empty.")]
        [SerializeField] ThirdPersonCameraRig cameraRig;

        public CharacterDefinition Current { get; private set; }

        // Read by the HUD: it pushes values into authored widgets, so it needs the parts, not a second lookup.
        public Health Health => health;
        public PlayerMotor Motor => motor;
        public Weapons.Weapon Weapon => weapon;
        public PlayerAbilities Abilities => abilities;

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
                motor.Configure(character.moveSpeed, character.sprintMultiplier, character.aimMoveMultiplier);
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

            if (character.aimFov > 0f)
            {
                ThirdPersonCameraRig rig = ResolveCameraRig();

                if (rig != null)
                {
                    rig.AimFov = character.aimFov;
                }
            }
        }

        /// <summary>The camera rig lives on the scene camera, not on the hero prefab — resolve it there.</summary>
        ThirdPersonCameraRig ResolveCameraRig()
        {
            if (cameraRig == null)
            {
                cameraRig = FindFirstObjectByType<ThirdPersonCameraRig>();
            }

            return cameraRig;
        }
    }
}
