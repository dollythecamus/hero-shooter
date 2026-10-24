using HeroShooter.Weapons;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// Connects player input to the weapon. Firing is refused while a channelled shield is raised — Bulwark's
    /// "no shooting while shielded" rule lives here because the weapon must not know about abilities.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Left empty, the Weapon on this same GameObject is used.")]
        [SerializeField] Weapon weapon;

        [Tooltip("Left empty, the BulwarkShield on this same GameObject is used.")]
        [SerializeField] BulwarkShield shield;

        public Weapon Weapon => weapon;

        bool _wasHeld;

        void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<Weapon>();
            }

            if (shield == null)
            {
                shield = GetComponent<BulwarkShield>();
            }
        }

        void Update()
        {
            if (weapon == null)
            {
                return;
            }

            if (shield != null && shield.IsRaised)
            {
                // A raised shield cancels a charge instead of holding it: releasing mid-shield must not fire.
                weapon.CancelCharge();
                _wasHeld = false;
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
