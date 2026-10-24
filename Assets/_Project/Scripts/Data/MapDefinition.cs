using UnityEngine;

namespace HeroShooter.Data
{
    /// <summary>
    /// One playable map, as data: what the select screen shows and which scene it loads. The scene name is the
    /// only thing the game acts on — it must match an entry in Build Settings exactly.
    /// </summary>
    [CreateAssetMenu(menuName = "Hero Shooter/Map Definition", fileName = "Map_New")]
    public class MapDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Map";
        [TextArea] public string blurb = "";
        [Tooltip("Exact scene name from Build Settings — this is the argument SceneManager.LoadScene receives.")]
        public string sceneName = "";
        public string sizeLabel = "";
        public Color accentColor = Color.white;

        public bool IsLoadable => !string.IsNullOrEmpty(sceneName);
    }
}
