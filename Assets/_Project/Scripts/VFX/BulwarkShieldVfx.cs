using UnityEngine;

namespace HeroShooter.VFX
{
    /// <summary>
    /// Rock's Bulwark made visible: an authored translucent shell that is shown only while the shield is up,
    /// shrinks and fades as the reservoir drains, so the meter is readable on the body and not only on the HUD.
    /// </summary>
    public class BulwarkShieldVfx : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Left empty, the BulwarkShield on the hero this object is parented to is used.")]
        [SerializeField] Player.BulwarkShield shield;
        [SerializeField] Renderer shell;

        [Header("Look")]
        [SerializeField] Color shellColor = new Color(0.45f, 0.7f, 1f, 0.35f);
        [Tooltip("Scale at a full reservoir; the shell shrinks toward 60 % of this as it drains.")]
        [SerializeField] float fullScale = 1.6f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock _block;

        void Awake()
        {
            if (shield == null)
            {
                shield = GetComponentInParent<Player.BulwarkShield>();
            }

            if (shell == null)
            {
                shell = GetComponent<Renderer>();
            }

            _block = new MaterialPropertyBlock();
            Show(false);
        }

        void Update()
        {
            if (shield == null || shell == null)
            {
                return;
            }

            bool raised = shield.IsRaised;
            Show(raised);

            if (!raised)
            {
                return;
            }

            float meter = shield.Normalized;
            transform.localScale = Vector3.one * Mathf.Lerp(fullScale * 0.6f, fullScale, meter);

            Color tint = shellColor;
            tint.a *= Mathf.Lerp(0.25f, 1f, meter);
            _block.SetColor(BaseColorId, tint);
            shell.SetPropertyBlock(_block);
        }

        void Show(bool visible)
        {
            if (shell != null && shell.enabled != visible)
            {
                shell.enabled = visible;
            }
        }
    }
}
