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

        bool _wasHeld;

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

            bool held = InputReader.FireHeld;

            // Charge weapons (Paper) fire on release; automatic weapons (Rock, Scissors) fire while held.
            if (weapon.Definition != null && weapon.Definition.RequiresCharge)
            {
                if (InputReader.FirePressed)
                {
                    weapon.BeginTrigger();
                }

                if (!held && _wasHeld)
                {
                    weapon.EndTrigger();
                }
            }
            else if (held)
            {
                weapon.TryFire();
            }

            _wasHeld = held;
        }
    }
}
