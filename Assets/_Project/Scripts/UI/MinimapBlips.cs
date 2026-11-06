using System.Collections.Generic;
using HeroShooter.Enemies;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// One dot per living enemy on the minimap. The gate on 2026-11-06 found the map showed the hero only.
    ///
    /// The minimap is a RenderTexture of a world-aligned top-down camera, so a blip is a plain world offset
    /// divided by that camera's orthographic half-extent — no second projection that could drift from the
    /// map underneath it. An enemy outside the map square is pinned to the rim, so "where" survives even
    /// when "how far" does not.
    ///
    /// Dots are built at runtime rather than authored: the count follows <see cref="WaveDirector"/>, and a
    /// prefab full of hand-placed dots would have to be re-authored for every map.
    /// </summary>
    public class MinimapBlips : MonoBehaviour
    {
        [Header("Scene parts (found at runtime when left empty)")]
        [SerializeField] HeroLoadout hero;
        [SerializeField] EnemyBarBinder binder;
        [SerializeField] MinimapCamera map;

        [Header("Authored widget")]
        [Tooltip("The minimap image. Dots are children of it, so a dot at its edge is at the map's edge.")]
        [SerializeField] RectTransform mapRect;

        [Header("Look")]
        [SerializeField] float dotSize = 6f;
        [SerializeField] Color enemyColor = new Color(1f, 0.35f, 0.35f, 1f);

        [Tooltip("Half-extent in metres used until a MinimapCamera answers. Matches that camera's 26.")]
        [SerializeField] float fallbackHalfExtent = 26f;

        [Tooltip("Dots are moved every frame; the enemy list is re-read this often.")]
        [SerializeField] float rebindSeconds = 0.5f;

        readonly List<Image> _dots = new List<Image>();
        int _liveCount;
        float _halfExtent = 26f;
        float _nextRebind;

        void Update()
        {
            Resolve();

            if (hero == null || mapRect == null || binder == null)
            {
                HideFrom(0);
                return;
            }

            if (Time.time >= _nextRebind)
            {
                _nextRebind = Time.time + Mathf.Max(0.05f, rebindSeconds);
                _halfExtent = map != null ? Mathf.Max(0.01f, map.HalfExtent) : Mathf.Max(0.01f, fallbackHalfExtent);
                RefreshEnemyCount();
            }

            PlaceDots();
        }

        /// <summary>Late binding: the hero and the binder may wake after this component.</summary>
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

            if (map == null)
            {
                map = FindFirstObjectByType<MinimapCamera>();
            }
        }

        /// <summary>Grow the dot pool to fit the living enemies, once per rebind — never per frame.</summary>
        void RefreshEnemyCount()
        {
            IReadOnlyList<EnemyBrain> enemies = binder.Enemies;
            int live = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (IsVisible(enemies[i]))
                {
                    live++;
                }
            }

            while (_dots.Count < live)
            {
                _dots.Add(CreateDot());
            }

            _liveCount = live;
        }

        void PlaceDots()
        {
            Vector3 origin = hero.transform.position;
            Vector2 half = mapRect.rect.size * 0.5f;

            // A dot pinned to the rim must still be fully inside it, hence the inset.
            Vector2 limit = new Vector2(
                Mathf.Max(0f, half.x - dotSize * 0.5f - 2f),
                Mathf.Max(0f, half.y - dotSize * 0.5f - 2f));

            int used = 0;
            IReadOnlyList<EnemyBrain> enemies = binder.Enemies;

            for (int i = 0; i < enemies.Count && used < _dots.Count; i++)
            {
                EnemyBrain brain = enemies[i];

                if (!IsVisible(brain))
                {
                    continue;
                }

                Vector3 offset = brain.transform.position - origin;

                // The map camera looks straight down with world +Z up the screen, so x→x and z→y, no rotation.
                Vector2 local = new Vector2(
                    offset.x / _halfExtent * half.x,
                    offset.z / _halfExtent * half.y);

                Image dot = _dots[used];
                dot.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Clamp(local.x, -limit.x, limit.x),
                    Mathf.Clamp(local.y, -limit.y, limit.y));
                dot.gameObject.SetActive(true);
                used++;
            }

            HideFrom(used);
        }

        void HideFrom(int firstUnused)
        {
            for (int i = firstUnused; i < _dots.Count; i++)
            {
                if (_dots[i].gameObject.activeSelf)
                {
                    _dots[i].gameObject.SetActive(false);
                }
            }
        }

        static bool IsVisible(EnemyBrain brain)
        {
            return brain != null && brain.gameObject.activeInHierarchy
                && brain.Health != null && brain.Health.IsAlive;
        }

        Image CreateDot()
        {
            var go = new GameObject("Blip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(mapRect, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(dotSize, dotSize);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = enemyColor;

            return image;
        }
    }
}
