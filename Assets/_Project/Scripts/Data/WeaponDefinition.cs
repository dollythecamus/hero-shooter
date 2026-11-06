using UnityEngine;

namespace HeroShooter.Data
{
    /// <summary>
    /// Everything that makes one gun different from another. Data only — the <c>Weapon</c> component
    /// reads this, so a character's weapon is a definition reference, never a copy of numbers on a prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Hero Shooter/Weapon Definition", fileName = "Weapon_New")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Weapon";

        [Header("Rate of fire")]
        public float shotsPerSecond = 4f;

        [Header("Ballistics")]
        public float damage = 12f;
        public float projectileSpeed = 70f;
        public float spreadDegrees = 0.6f;
        [Tooltip("Seconds before a fired bullet despawns — this is the weapon's effective range.")]
        public float projectileLifetime = 3f;
        [Tooltip("Bullet radius for the swept collision check.")]
        public float projectileRadius = 0.12f;

        [Header("Charge (hold to charge, release to fire)")]
        [Tooltip("0 = instant fire. Above 0 = the trigger charges for this many seconds.")]
        public float chargeTime = 0f;
        [Tooltip("Damage at full charge. Interpolated from damage when chargeTime > 0.")]
        public float chargedDamage = 0f;
        [Tooltip("Projectile speed at full charge.")]
        public float chargedProjectileSpeed = 0f;

        [Header("Ammunition")]
        [Tooltip("Rounds in one magazine. Every trigger pull spends one round, whatever it fires.")]
        public int magazineSize = 12;
        [Tooltip("Rounds carried outside the magazine. Reloading draws from here.")]
        public int reserveAmmo = 96;
        [Tooltip("Seconds a reload takes.")]
        public float reloadTime = 1.4f;
        [Tooltip("Projectiles per trigger pull. Rock fires eight small ones like a shotgun.")]
        public int pelletsPerShot = 1;
        [Tooltip("Cone for a multi-pellet shot, in degrees. Ignored when pelletsPerShot is 1.")]
        public float pelletSpreadDegrees = 3.5f;

        [Header("Movement while firing")]
        [Tooltip("1 = no slowdown. Rock fires heavy shots and slows to this fraction while shooting.")]
        [Range(0f, 1f)] public float firingSpeedMultiplier = 1f;
        [Tooltip("How long the slowdown lingers after a shot.")]
        public float firingSlowDuration = 0.4f;

        public bool RequiresCharge => chargeTime > 0f;
    }
}
