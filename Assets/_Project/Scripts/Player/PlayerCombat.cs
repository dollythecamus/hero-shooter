using HeroShooter.Weapons;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Connects player input to the weapon. Abilities join this file in S3, where the
    /// character's <c>AbilityDefinition</c> data decides what Q/E actually do.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Left empty, the Weapon on this same GameObject is used.")]
        [SerializeField] Weapon weapon;

        public Weapon Weapon => weapon;

        void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<Weapon>();
            }
        }

        void Update()
        {
            if (weapon == null)
            {
                return;
            }

            if (InputReader.FireHeld)
            {
                weapon.TryFire();
            }
        }
    }
}
