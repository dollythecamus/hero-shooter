using UnityEngine;

namespace HeroShooter.Data
{
    /// <summary>
    /// One of the three heroes: stats, body scale/colour, its own weapon and its two abilities.
    /// Selecting a character is nothing more than handing this asset to the spawner.
    /// </summary>
    [CreateAssetMenu(menuName = "Hero Shooter/Character Definition", fileName = "Hero_New")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Hero";
        [TextArea] public string pitch = "";

        [Header("Body")]
        [Tooltip("Uniform scale of the capsule body — Rock is bigger than Paper.")]
        public float bodyScale = 1f;
        public Color color = Color.white;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 6f;
        public float sprintMultiplier = 1.6f;
        [Tooltip("Movement multiplier while RMB aiming (roster: 0.9 for every hero, 0.6 for Paper).")]
        [Range(0.1f, 1f)] public float aimMoveMultiplier = 0.9f;
        [Tooltip("Paper's aim crosshair is a sniper reticle, not the shared one.")]
        public bool sniperReticle;

        [Header("Loadout")]
        public WeaponDefinition weapon;
        public AbilityDefinition abilityPrimary;
        public AbilityDefinition abilitySecondary;

        public AbilityDefinition AbilityAt(int index) => index == 0 ? abilityPrimary : abilitySecondary;
    }
}
