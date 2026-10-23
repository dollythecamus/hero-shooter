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
        [SerializeField] Weapon weapon;

        public Weapon Weapon => weapon;

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
