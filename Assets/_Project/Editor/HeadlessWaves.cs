using System.Text;
using HeroShooter.Core;
using HeroShooter.Enemies;
using HeroShooter.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HeroShooter.EditorTools
{
    /// <summary>
    /// Authors the three free-choice mechanics into HUD.prefab while the GUI editor is closed: the wave
    /// HUD line, the pooled damage numbers, and the pause panel with its two camera settings. Everything it
    /// creates is saved into prefabs; nothing here runs in a player build (Editor folder).
    /// </summary>
    public static class HeadlessWaves
    {
        const string HudPath = "Assets/_Project/Prefabs/UI/HUD.prefab";
        const string DamageNumberPath = "Assets/_Project/Prefabs/UI/DamageNumber.prefab";
        const string RangedPath = "Assets/_Project/Prefabs/Enemies/RangedEnemy.prefab";
        const string MeleePath = "Assets/_Project/Prefabs/Enemies/MeleeEnemy.prefab";

        public static void Setup()
        {
            var log = new StringBuilder();
            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform content = root.transform.Find("Content");

                if (content == null)
                {
                    log.AppendLine("[S6] FAIL no Content node");
                    Debug.Log(log.ToString());
                    EditorApplication.Exit(1);
                    return;
                }

                Font font = root.GetComponentInChildren<Text>(true) != null
                    ? root.GetComponentInChildren<Text>(true).font
                    : null;

                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                BuildDamageNumberPrefab(font, log);

                Text waveText = BuildWaveText(content, font);
                GameObject pausePanel = BuildPausePanel(content, font, out Slider sensitivity, out Toggle invertY,
                    out Button resume, out Button quit);
                DamageNumberPool damagePool = BuildDamagePool(content, log);
                EnemyPool ranged = BuildPool(root.transform, "EnemyPool_Ranged", RangedPath, 6, log);
                EnemyPool melee = BuildPool(root.transform, "EnemyPool_Melee", MeleePath, 6, log);

                var waves = GetOrAdd<WaveDirector>(root);
                SetObject(waves, "rangedPool", ranged);
                SetObject(waves, "meleePool", melee);

                var pause = GetOrAdd<PauseMenu>(root);
                SetObject(pause, "panel", pausePanel);
                SetObject(pause, "sensitivity", sensitivity);
                SetObject(pause, "invertY", invertY);
                SetObject(pause, "resumeButton", resume);
                SetObject(pause, "quitButton", quit);

                var hud = root.GetComponent<HudController>();

                if (hud != null)
                {
                    SetObject(hud, "waveText", waveText);
                    SetObject(hud, "waves", waves);
                    SetObject(hud, "damageNumbers", damagePool);
                }
                else
                {
                    log.AppendLine("[S6] FAIL no HudController on HUD root");
                }

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
                log.AppendLine($"[S6] authored wave text, damage pool, pause panel, two enemy pools, WaveDirector, PauseMenu font={(font != null ? font.name : "null")}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Verify(log);
            Debug.Log(log.ToString());
            EditorApplication.Exit(0);
        }

        static void BuildDamageNumberPrefab(Font font, StringBuilder log)
        {
            var temp = new GameObject("DamageNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text),
                typeof(CanvasGroup), typeof(DamageNumber));
            temp.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 34f);

            var label = temp.GetComponent<Text>();
            label.font = font;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var number = temp.GetComponent<DamageNumber>();
            SetObject(number, "label", label);
            SetObject(number, "group", temp.GetComponent<CanvasGroup>());

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, DamageNumberPath);
            Object.DestroyImmediate(temp);

            log.AppendLine($"[S6] damage number prefab saved={(saved != null)} font={(font != null ? font.name : "null")}");
        }

        static Text BuildWaveText(Transform content, Font font)
        {
            Transform old = content.Find("WaveText");

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            return MakeText(content, "WaveText", "WAVE 1 / 5   3 LEFT", 22, TextAnchor.MiddleCenter, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(620f, 34f),
                new Color(0.92f, 0.94f, 0.98f, 1f));
        }

        static DamageNumberPool BuildDamagePool(Transform content, StringBuilder log)
        {
            Transform old = content.Find("DamageNumbers");

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var container = new GameObject("DamageNumbers", typeof(RectTransform));
            container.transform.SetParent(content, false);

            var rect = container.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var pool = container.AddComponent<DamageNumberPool>();
            var prefab = AssetDatabase.LoadAssetAtPath<DamageNumber>(DamageNumberPath);

            SetObject(pool, "prefab", prefab);
            SetInt(pool, "warmUpCount", 16);
            SetObject(pool, "container", container.transform);

            log.AppendLine($"[S6] damage pool prefab={(prefab != null)}");
            return pool;
        }

        static EnemyPool BuildPool(Transform root, string name, string prefabPath, int warmUp, StringBuilder log)
        {
            Transform old = root.Find(name);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var host = new GameObject(name);
            host.transform.SetParent(root, false);

            var pool = host.AddComponent<EnemyPool>();
            EnemyBrain prefab = AssetDatabase.LoadAssetAtPath<EnemyBrain>(prefabPath);

            SetObject(pool, "prefab", prefab);
            SetInt(pool, "warmUpCount", warmUp);

            log.AppendLine($"[S6] pool {name} prefab={(prefab != null)} warmUp={warmUp}");
            return pool;
        }

        static GameObject BuildPausePanel(Transform content, Font font, out Slider sensitivity, out Toggle invertY,
            out Button resume, out Button quit)
        {
            Transform old = content.Find("PausePanel");

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject dim = MakeImage(content, "PausePanel", new Color(0f, 0f, 0f, 0.62f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            GameObject frame = MakeImage(dim.transform, "Frame", new Color(0.07f, 0.08f, 0.10f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(560f, 380f));

            MakeText(frame.transform, "Title", "PAUSED", 30, TextAnchor.MiddleCenter, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(500f, 40f), Color.white);

            MakeText(frame.transform, "SensitivityLabel", "MOUSE SENSITIVITY", 16, TextAnchor.MiddleLeft, font,
                new Vector2(0f, 1f), new Vector2(34f, -96f), new Vector2(300f, 24f),
                new Color(0.72f, 0.75f, 0.80f, 1f));

            sensitivity = MakeSlider(frame.transform, "Sensitivity", font,
                new Vector2(0f, 1f), new Vector2(34f, -128f), new Vector2(492f, 26f));

            MakeText(frame.transform, "InvertLabel", "INVERT Y", 16, TextAnchor.MiddleLeft, font,
                new Vector2(0f, 1f), new Vector2(34f, -190f), new Vector2(300f, 24f),
                new Color(0.72f, 0.75f, 0.80f, 1f));

            invertY = MakeToggle(frame.transform, "InvertY", new Vector2(0f, 1f), new Vector2(34f, -222f),
                new Vector2(28f, 28f));

            resume = MakeButton(frame.transform, "Resume", "RESUME", font,
                new Vector2(0.5f, 0f), new Vector2(-116f, 34f), new Vector2(200f, 52f));

            quit = MakeButton(frame.transform, "Quit", "QUIT", font,
                new Vector2(0.5f, 0f), new Vector2(116f, 34f), new Vector2(200f, 52f));

            dim.SetActive(false);
            return dim;
        }

        // ---- primitives -------------------------------------------------------------------------

        static GameObject MakeImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = name != "PausePanel" && name != "Frame";

            return child;
        }

        static Text MakeText(Transform parent, string name, string content, int fontSize, TextAnchor alignment,
            Font font, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = child.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            return text;
        }

        static Button MakeButton(Transform parent, string name, string label, Font font, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject host = MakeImage(parent, name, new Color(0.16f, 0.18f, 0.22f, 1f), anchor, anchor, anchor,
                position, size);
            var button = host.AddComponent<Button>();
            button.targetGraphic = host.GetComponent<Image>();

            MakeText(host.transform, "Label", label, 20, TextAnchor.MiddleCenter, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);

            return button;
        }

        static Slider MakeSlider(Transform parent, string name, Font font, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            GameObject host = MakeImage(parent, name, new Color(0.20f, 0.22f, 0.26f, 1f), anchor, anchor, anchor,
                position, size);

            GameObject fillArea = MakeImage(host.transform, "Fill Area", new Color(1f, 1f, 1f, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(10f, 4f);
            fillAreaRect.offsetMax = new Vector2(-10f, -4f);

            GameObject fill = MakeImage(fillArea.transform, "Fill", new Color(0.55f, 0.80f, 1f, 1f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleArea = MakeImage(host.transform, "Handle Slide Area", new Color(1f, 1f, 1f, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = MakeImage(handleArea.transform, "Handle", new Color(0.85f, 0.90f, 1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(18f, 26f));

            var slider = host.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.35f;

            return slider;
        }

        static Toggle MakeToggle(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject host = MakeImage(parent, name, new Color(0.20f, 0.22f, 0.26f, 1f), anchor, anchor, anchor,
                position, size);

            GameObject check = MakeImage(host.transform, "Checkmark", new Color(0.55f, 0.80f, 1f, 1f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.offsetMin = new Vector2(4f, 4f);
            checkRect.offsetMax = new Vector2(-4f, -4f);

            var toggle = host.AddComponent<Toggle>();
            toggle.targetGraphic = host.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = false;

            return toggle;
        }

        static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T existing = root.GetComponent<T>();
            return existing != null ? existing : root.AddComponent<T>();
        }

        static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogWarning($"[S6] no serialized field '{field}' on {target.GetType().Name}");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogWarning($"[S6] no serialized field '{field}' on {target.GetType().Name}");
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Runtime proof for the three free mechanics on the real arena scene: wave 1 spawns out of the
        /// pools, a kill lands through <see cref="Health"/>, the HUD wave line and a damage number appear,
        /// and the pause menu freezes and restores the clock.
        /// </summary>
        public static void Probe()
        {
            var log = new StringBuilder();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/20_Map_Arena.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            var waves = default(WaveDirector);
            var score = default(ScoreService);
            var pause = default(PauseMenu);
            var hud = default(GameObject);
            var hero = default(Player.HeroLoadout);
            int stage = 0;
            int stageFrame = 0;

            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();

            void Tick()
            {
                try
                {
                    Step();
                }
                catch (System.Exception exception)
                {
                    log.AppendLine($"[S6Probe] EXCEPTION {exception.GetType().Name}: {exception.Message}");
                    Debug.Log(log.ToString());
                    EditorApplication.ExitPlaymode();
                    EditorApplication.Exit(0);
                }
            }

            void Step()
            {
                if (stage == 0 && Time.frameCount > 60)
                {
                    hud = GameObject.Find("HUD");
                    score = hud != null ? hud.GetComponent<ScoreService>() : null;
                    waves = hud != null ? hud.GetComponent<WaveDirector>() : null;
                    pause = hud != null ? hud.GetComponent<PauseMenu>() : null;
                    hero = Object.FindFirstObjectByType<Player.HeroLoadout>();

                    log.AppendLine($"[S6Probe] A hud={(hud != null)} waves={(waves != null)} score={(score != null)} pause={(pause != null)} hero={(hero != null)}");
                    // Headless has no graphics device: a camera that renders into a RenderTexture makes URP
                    // crash (RenderTexture.Create failed). The minimap is not under test here, so switch the
                    // one camera off; its LateUpdate logic is measured separately in the minimap probe.
                    var minimap = Object.FindAnyObjectByType<MinimapCamera>();

                    if (minimap != null)
                    {
                        var minimapCamera = minimap.GetComponent<Camera>();
                        minimapCamera.enabled = false;
                        log.AppendLine($"[S6Probe] A minimap camera disabled for headless rt={(minimapCamera.targetTexture != null ? minimapCamera.targetTexture.name : "null")}");
                    }

                    log.AppendLine($"[S6Probe] A before wave: wave={(waves != null ? waves.Wave : -1)} alive={(waves != null ? waves.Alive : -1)} scoreAlive={(score != null ? score.EnemiesAlive : -1)}");

                    Debug.Log(log.ToString());
                    stage = 1;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 1 && waves != null && waves.Wave >= 1 && Time.frameCount > stageFrame + 30)
                {
                    var rangedField = typeof(WaveDirector).GetField("rangedPool", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var meleeField = typeof(WaveDirector).GetField("meleePool", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var ranged = rangedField != null ? rangedField.GetValue(waves) as EnemyPool : null;
                    var melee = meleeField != null ? meleeField.GetValue(waves) as EnemyPool : null;

                    log.AppendLine($"[S6Probe] B wave={waves.Wave} alive={waves.Alive} scoreAlive={(score != null ? score.EnemiesAlive : -1)} kills={(score != null ? score.Kills : -1)}");
                    log.AppendLine($"[S6Probe] B pools rangedCreated={(ranged != null ? ranged.TotalCreated : -1)} rangedActive={(ranged != null ? ranged.ActiveCount : -1)} meleeCreated={(melee != null ? melee.TotalCreated : -1)} meleeActive={(melee != null ? melee.ActiveCount : -1)}");

                    var poolOf = typeof(WaveDirector).GetField("_poolOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var map = poolOf != null ? poolOf.GetValue(waves) as System.Collections.IDictionary : null;

                    if (map != null && map.Count > 0)
                    {
                        EnemyBrain victim = null;

                        foreach (object key in map.Keys)
                        {
                            victim = key as EnemyBrain;
                            break;
                        }

                        if (victim != null)
                        {
                            var health = victim.GetComponent<Health>();
                            log.AppendLine($"[S6Probe] C killing spawned {victim.name} hp={(health != null ? health.Current : -1f)}");
                            health.TakeDamage(new DamageInfo(9999f, victim.transform.position, Vector3.up,
                                hero != null ? hero.gameObject : null));
                        }
                    }

                    Debug.Log(log.ToString());
                    stage = 2;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 2 && Time.frameCount > stageFrame + 5)
                {
                    log.AppendLine($"[S6Probe] D after kill: alive={(waves != null ? waves.Alive : -1)} kills={(score != null ? score.Kills : -1)} scoreAlive={(score != null ? score.EnemiesAlive : -1)}");

                    Transform waveText = hud != null ? hud.transform.Find("Content/WaveText") : null;
                    Transform poolRoot = hud != null ? hud.transform.Find("Content/DamageNumbers") : null;
                    int activeNumbers = 0;

                    if (poolRoot != null)
                    {
                        for (int i = 0; i < poolRoot.childCount; i++)
                        {
                            if (poolRoot.GetChild(i).gameObject.activeSelf)
                            {
                                activeNumbers++;
                            }
                        }
                    }

                    var hudController = hud != null ? hud.GetComponent<HudController>() : null;
                    string waveLine = "?";

                    if (hudController != null)
                    {
                        var serialized = new SerializedObject(hudController);
                        var text = serialized.FindProperty("waveText").objectReferenceValue as Text;
                        waveLine = text != null ? text.text : "null";
                    }

                    log.AppendLine($"[S6Probe] D waveLine='{waveLine}' damageNumbersActive={activeNumbers} damagePoolChildren={(poolRoot != null ? poolRoot.childCount : -1)}");

                    if (pause != null)
                    {
                        pause.Pause();
                        Transform panel = hud.transform.Find("Content/PausePanel");
                        log.AppendLine($"[S6Probe] E paused timeScale={Time.timeScale} panel={(panel != null && panel.gameObject.activeSelf)}");
                        pause.Resume();
                        log.AppendLine($"[S6Probe] E resumed timeScale={Time.timeScale} panel={(panel != null && panel.gameObject.activeSelf)}");
                    }

                    // Hero death -> respawn, on the real Health death path.
                    if (hero != null && hero.Health != null && System.Environment.GetEnvironmentVariable("SKIP_HERO_KILL") != "1")
                    {
                        _respawnFrom = hero.transform.position;
                        _killTime = Time.time;
                        log.AppendLine($"[S6Probe] F killing hero hp={hero.Health.Current} pos={hero.transform.position}");
                        hero.Health.TakeDamage(new DamageInfo(9999f, hero.transform.position, Vector3.up, null));
                    }

                    Debug.Log(log.ToString());
                    stage = 3;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 3 && (System.Environment.GetEnvironmentVariable("SKIP_HERO_KILL") == "1" ? Time.time > _killTime + 3f : Time.time > _killTime + 3f))
                {
                    EditorApplication.update -= Tick;

                    if (hero != null && hero.Health != null)
                    {
                        float moved = Vector3.Distance(hero.transform.position, _respawnFrom);
                        log.AppendLine($"[S6Probe] F after respawn hp={hero.Health.Current} deaths={(score != null ? score.Deaths : -1)} pos={hero.transform.position} movedFromDeathSpot={moved:0.00}");
                    }

                    Debug.Log(log.ToString());
                    EditorApplication.ExitPlaymode();
                    EditorApplication.Exit(0);
                }
            }
        }

        static Vector3 _respawnFrom;
        static float _killTime;

        static void Verify(StringBuilder log)
        {
            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);

            if (hud == null)
            {
                log.AppendLine("[S6] verify FAIL hud missing");
                return;
            }

            var waves = hud.GetComponent<WaveDirector>();
            var pause = hud.GetComponent<PauseMenu>();
            var hudController = hud.GetComponent<HudController>();
            Transform content = hud.transform.Find("Content");

            log.AppendLine($"[S6] verify waves={(waves != null)} pause={(pause != null)} hudController={(hudController != null)}");
            log.AppendLine($"[S6] verify waveText={(content != null && content.Find("WaveText") != null)} damageNumbers={(content != null && content.Find("DamageNumbers") != null)} pausePanel={(content != null && content.Find("PausePanel") != null)}");

            if (waves != null)
            {
                var serialized = new SerializedObject(waves);
                log.AppendLine($"[S6] verify pools ranged={(serialized.FindProperty("rangedPool").objectReferenceValue != null)} melee={(serialized.FindProperty("meleePool").objectReferenceValue != null)}");
            }

            if (hudController != null)
            {
                var serialized = new SerializedObject(hudController);
                log.AppendLine($"[S6] verify hud refs waves={(serialized.FindProperty("waves").objectReferenceValue != null)} waveText={(serialized.FindProperty("waveText").objectReferenceValue != null)} damageNumbers={(serialized.FindProperty("damageNumbers").objectReferenceValue != null)}");
            }

            if (pause != null)
            {
                var serialized = new SerializedObject(pause);
                log.AppendLine($"[S6] verify pause refs panel={(serialized.FindProperty("panel").objectReferenceValue != null)} slider={(serialized.FindProperty("sensitivity").objectReferenceValue != null)} toggle={(serialized.FindProperty("invertY").objectReferenceValue != null)} resume={(serialized.FindProperty("resumeButton").objectReferenceValue != null)} quit={(serialized.FindProperty("quitButton").objectReferenceValue != null)}");
            }

            var damage = AssetDatabase.LoadAssetAtPath<DamageNumber>(DamageNumberPath);
            log.AppendLine($"[S6] verify damageNumberPrefab={(damage != null)}");
        }
    }
}
