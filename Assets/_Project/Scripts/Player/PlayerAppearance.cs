using HeroShooter.Data;
using UnityEngine;

namespace HeroShooter.Player
{
    /// <summary>
    /// The only place that changes how the hero looks. With no imported art, a character is a body scale and
    /// a colour — applied through a MaterialPropertyBlock so no material asset is instanced or leaked.
    /// </summary>
    public class PlayerAppearance : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Vector3 baseBodyScale = new Vector3(0.7f, 0.9f, 0.7f);

        public void Apply(CharacterDefinition character)
        {
            if (body == null || character == null)
            {
                return;
            }

            body.localScale = baseBodyScale * Mathf.Max(0.1f, character.bodyScale);

            MeshRenderer renderer = body.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", character.color);
            block.SetColor("_Color", character.color);
            renderer.SetPropertyBlock(block);
        }
    }
}
