using System.Collections.Generic;
using HeroShooter.Core;
using HeroShooter.Data;
using HeroShooter.Enemies;
using HeroShooter.Player;
using HeroShooter.Systems;
using UnityEngine;

namespace HeroShooter.VFX
{
    /// <summary>
    /// Says on the enemy body what the HUD chips say in the corner. The 2026-11-06 gate: *"Snare ability
    /// doesn't seem to do anything. Marked ability is not clear to use at all, i don't know when an enemy is
    /// marked"* — both effects were wired, but the only evidence was a health bar that hides at full health.
    ///
    /// Three states, all drawn under the enemy's feet so the read does not depend on a bar being visible:
    /// a white ring on the enemy a ready Mark would take (before the cast), amber while marked, cyan while
    /// snared, plus a tether from the hero to a snared body.
    ///
    /// Geometry is built in code — a LineRenderer circle needs no imported art, and the prefab stays free of
    /// per-enemy authoring.
    /// </summary>
    public class EnemyStatusRings : MonoBehaviour
    {
        [Header("Scene parts (found at runtime when empty)")]
        [SerializeField] HeroLoadout hero;
        [SerializeField] EnemyBarBinder binder;

        [Header("Rings")]
        [Tooltip("Metres above the enemy's feet, enough to clear the floor without floating.")]
        [SerializeField] float groundOffset = 0.06f;
        [SerializeField] float markedRadius = 0.55f;
        [SerializeField] float snaredRadius = 0.75f;
        [Tooltip("The pre-cast candidate's ring: the largest, because it is the one being decided.")]
        [SerializeField] float previewRadius = 0.95f;
        [SerializeField] int segments = 40;
        [SerializeField] float width = 0.055f;

        [Header("Colours")]
        [SerializeField] Color markedColor = new Color(1f, 0.72f, 0.2f, 0.9f);
        [SerializeField] Color snaredColor = new Color(0.45f, 0.85f, 1f, 0.9f);
        [SerializeField] Color previewColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color tetherColor = new Color(0.45f, 0.85f, 1f, 0.55f);

        /// <summary>Height on the hero the snare tether leaves from — chest-ish.</summary>
        [SerializeField] float tetherHeight = 1.2f;

        readonly List<Ring> _rings = new List<Ring>();
        Material _material;
        Vector3[] _unitCircle;

        void LateUpdate()
        {
            Resolve();

            if (hero == null || binder == null || hero.Abilities == null)
            {
                HideFrom(0);
                return;
            }

            EnemyBrain preview = ResolvePreviewTarget();
            IReadOnlyList<EnemyBrain> enemies = binder.Enemies;
            Vector3 tetherOrigin = transform.position + Vector3.up * tetherHeight;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBrain brain = enemies[i];

                if (brain == null || !brain.gameObject.activeInHierarchy)
                {
                    RingAt(i).Hide();
                    continue;
                }

                Health health = brain.Health;
                bool marked = health != null && health.IsMarked;
                bool snared = brain.IsSnared;
                bool isPreview = brain == preview;

                if (!marked && !snared && !isPreview)
                {
                    RingAt(i).Hide();
                    continue;
                }

                Ring ring = RingAt(i);
                Color colour = isPreview ? previewColor : marked ? markedColor : snaredColor;
                float radius = isPreview ? previewRadius : marked ? markedRadius : snaredRadius;

                ring.Show(brain.transform.position + Vector3.up * groundOffset, radius, colour);
                ring.SetTether(snared, tetherOrigin,
                    brain.transform.position + Vector3.up * tetherHeight, tetherColor);
            }

            HideFrom(enemies.Count);
        }

        /// <summary>Late binding: the hero and the binder can wake after this component.</summary>
        void Resolve()
        {
            if (hero == null)
            {
                hero = FindFirstObjectByType<HeroLoadout>();
            }

            if (binder == null)
            {
                binder = FindFirstObjectByType<EnemyBarBinder>();
            }
        }

        /// <summary>
        /// The enemy a Mark would take right now: aiming, the marking ability off cooldown, and that enemy
        /// under the crosshair with nothing solid in the way — the same rule <c>AimRaycast</c> applies when
        /// the cast actually happens, so the preview cannot promise a target the cast would refuse.
        /// </summary>
        EnemyBrain ResolvePreviewTarget()
        {
            if (!InputReader.AimHeld)
            {
                return null;
            }

            int slot = MarkSlot();

            if (slot < 0 || !hero.Abilities.IsReady(slot))
            {
                return null;
            }

            Camera camera = Camera.main;

            if (camera == null)
            {
                return null;
            }

            float range = Mathf.Max(5f, hero.Abilities.Definitions[slot].range);
            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            // A wall closer than the enemy is what the raycast returns, and then there is no preview.
            return hit.collider.GetComponentInParent<EnemyBrain>();
        }

        int MarkSlot()
        {
            for (int slot = 0; slot < PlayerAbilities.SlotCount; slot++)
            {
                AbilityDefinition definition = hero.Abilities.Definitions[slot];

                if (definition != null && definition.kind == AbilityKind.MarkTarget)
                {
                    return slot;
                }
            }

            return -1;
        }

        Ring RingAt(int index)
        {
            while (_rings.Count <= index)
            {
                _rings.Add(new Ring(transform, Material(), Circle(), segments, width));
            }

            return _rings[index];
        }

        void HideFrom(int firstUnused)
        {
            for (int i = firstUnused; i < _rings.Count; i++)
            {
                _rings[i].Hide();
            }
        }

        Vector3[] Circle()
        {
            if (_unitCircle == null)
            {
                _unitCircle = new Vector3[segments];

                for (int i = 0; i < segments; i++)
                {
                    float angle = i / (float)segments * Mathf.PI * 2f;
                    _unitCircle[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                }
            }

            return _unitCircle;
        }

        Material Material()
        {
            if (_material == null)
            {
                // Same shader family as the projectile trail, which is the one material this URP project
                // already renders: a built-in sprite shader can report "supported" and still draw nothing.
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader != null)
                {
                    _material = new Material(shader);
                }
            }

            return _material;
        }

        /// <summary>One enemy's ring plus its optional tether; both are LineRenderers built once.</summary>
        sealed class Ring
        {
            readonly LineRenderer _ring;
            readonly LineRenderer _tether;
            readonly Vector3[] _circle;
            bool _ringVisible;

            public Ring(Transform owner, Material material, Vector3[] circle, int segments, float width)
            {
                _circle = circle;
                _ring = NewLine("StatusRing", owner, material, segments, width);
                _ring.loop = true;
                _ring.useWorldSpace = false;

                for (int i = 0; i < circle.Length; i++)
                {
                    _ring.SetPosition(i, circle[i]);
                }

                _tether = NewLine("StatusTether", owner, material, 2, width * 0.6f);
                _tether.useWorldSpace = true;
                _tether.gameObject.SetActive(false);
            }

            public void Show(Vector3 position, float radius, Color colour)
            {
                if (!_ring.gameObject.activeSelf)
                {
                    _ring.gameObject.SetActive(true);
                }

                _ring.transform.position = position;
                _ring.transform.localScale = new Vector3(radius, 1f, radius);
                _ring.startColor = colour;
                _ring.endColor = colour;
                _ringVisible = true;
            }

            public void SetTether(bool visible, Vector3 from, Vector3 to, Color colour)
            {
                if (_tether.gameObject.activeSelf != visible)
                {
                    _tether.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    return;
                }

                _tether.SetPosition(0, from);
                _tether.SetPosition(1, to);
                _tether.startColor = colour;
                _tether.endColor = colour;
            }

            public void Hide()
            {
                if (_ringVisible)
                {
                    _ring.gameObject.SetActive(false);
                    _ringVisible = false;
                }

                if (_tether.gameObject.activeSelf)
                {
                    _tether.gameObject.SetActive(false);
                }
            }

            static LineRenderer NewLine(string name, Transform owner, Material material, int positions, float width)
            {
                var go = new GameObject(name);
                go.transform.SetParent(owner, false);

                var line = go.AddComponent<LineRenderer>();
                line.positionCount = positions;
                line.loop = false;
                line.widthMultiplier = width;
                line.numCapVertices = 2;
                line.textureMode = LineTextureMode.Stretch;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                if (material != null)
                {
                    line.material = material;
                }

                return line;
            }
        }
    }
}
