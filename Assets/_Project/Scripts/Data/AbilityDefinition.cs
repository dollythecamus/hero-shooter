using UnityEngine;

namespace HeroShooter.Data
{
    /// <summary>Which behaviour an ability runs. The numbers live in the definition, not in the class.</summary>
    public enum AbilityKind
    {
        None = 0,

        // Rock — tank
        GroundSlam = 10,
        Bulwark = 11,

        // Paper — sniper
        MarkTarget = 20,
        Snare = 21,

        // Scissors — skirmisher
        WallClimb = 30,
        Overclock = 31
    }

    /// <summary>
    /// One of a character's two abilities, as data: what it does, how often, how strong.
    /// The matching behaviour is chosen by <see cref="AbilityKind"/> in <c>AbilityFactory</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Hero Shooter/Ability Definition", fileName = "Ability_New")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Ability";
        public AbilityKind kind = AbilityKind.None;
        [Tooltip("Shown in the ability bar and used for the 30-second code explanation.")]
        public string description = "";

        [Header("Timing")]
        public float cooldown = 8f;
        public float duration = 0f;

        [Header("Effect")]
        public float magnitude = 0f;
        public float radius = 0f;
        public float range = 0f;

        [Header("Input")]
        [Tooltip("Key shown in the HUD: Q or E.")]
        public string slotLabel = "Q";
    }
}
