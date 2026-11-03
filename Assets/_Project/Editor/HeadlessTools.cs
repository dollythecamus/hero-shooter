using System.Text;
using HeroShooter.Core;
using HeroShooter.Systems;
using HeroShooter.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace HeroShooter.EditorTools
{
    /// <summary>
    /// Headless entry points used while the GUI editor is closed. Everything here runs through
    /// `Unity -batchmode -executeMethod`; none of it ships in a player build (Editor folder).
    /// Probe exists to answer questions about authored assets without a human looking at the editor.
    /// </summary>
    public static class HeadlessTools
    {
        const string HudPath = "Assets/_Project/Prefabs/UI/HUD.prefab";
        const string PlayerPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        public static void Probe()
        {
            var log = new StringBuilder();
            log.AppendLine($"[Probe] unity={Application.unityVersion} batch={Application.isBatchMode}");

            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            log.AppendLine($"[Probe] hud={hud != null}");
            if (hud != null)
            {
                Dump(log, hud.transform, string.Empty);
            }

            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            log.AppendLine($"[Probe] player={player != null}");
            if (player != null)
            {
                Dump(log, player.transform, string.Empty);
            }

            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    log.AppendLine($"[Probe] layer {i} = {name}");
                }
            }

            foreach (string scene in new[]
                     {
                         "Assets/_Project/Scenes/20_Map_Arena.unity",
                         "Assets/_Project/Scenes/21_Map_Alley.unity",
                         "Assets/_Project/Scenes/00_Boot.unity"
                     })
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene);
                log.AppendLine($"[Probe] scene {scene} = {asset != null}");
            }

            Debug.Log(log.ToString());
            EditorApplication.Exit(0);
        }

        const string MinimapFolder = "Assets/_Project/Data/Minimap";
        const string MinimapRtPath = MinimapFolder + "/MinimapRT.renderTexture";

        /// <summary>
        /// Authors the minimap (requirement #8) without a GUI editor: the RenderTexture asset, the camera on
        /// the hero prefab, and the frame + RawImage + facing arrow inside the authored HUD prefab. Every
        /// object it writes is saved into a prefab asset — nothing is built at runtime.
        /// </summary>
        public static void SetupMinimap()
        {
            var log = new StringBuilder();
            RenderTexture rt = EnsureMinimapTexture(log);
            BuildMinimapCamera(log, rt);
            BuildHudMinimapFrame(log, rt);
            VerifyMinimap(log);
            Debug.Log(log.ToString());
            EditorApplication.Exit(0);
        }

        static RenderTexture EnsureMinimapTexture(StringBuilder log)
        {
            RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapRtPath);

            if (existing != null)
            {
                log.AppendLine($"[Minimap] rt reusing {MinimapRtPath} {existing.width}x{existing.height}");
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(MinimapFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Minimap");
            }

            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                name = "MinimapRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false
            };

            AssetDatabase.CreateAsset(rt, MinimapRtPath);
            AssetDatabase.SaveAssets();
            log.AppendLine($"[Minimap] rt created {MinimapRtPath} 256x256");

            return AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapRtPath);
        }

        static void BuildMinimapCamera(StringBuilder log, RenderTexture rt)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPath);

            try
            {
                Transform old = root.transform.Find("MinimapCamera");

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var cameraObject = new GameObject("MinimapCamera");
                cameraObject.transform.SetParent(root.transform, false);

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 26f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 80f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
                // Default(0) + Structure(3) + Player(6) + Enemy(7). The UI layer is deliberately out, so
                // world-space enemy bars and the HUD canvas never bleed into the map.
                camera.cullingMask = (1 << 0) | (1 << 3) | (1 << 6) | (1 << 7);
                camera.targetTexture = rt;
                camera.depth = -10f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.useOcclusionCulling = false;

                var minimap = cameraObject.AddComponent<MinimapCamera>();
                var serialized = new SerializedObject(minimap);
                serialized.FindProperty("follow").objectReferenceValue = root.transform;
                serialized.FindProperty("height").floatValue = 30f;
                serialized.FindProperty("orthographicSize").floatValue = 26f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
                log.AppendLine("[Minimap] player camera authored on Player.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void BuildHudMinimapFrame(StringBuilder log, RenderTexture rt)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform content = root.transform.Find("Content");

                if (content == null)
                {
                    log.AppendLine("[Minimap] FAIL no Content node in HUD.prefab");
                    return;
                }

                Transform old = content.Find("MinimapFrame");

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var frame = new GameObject("MinimapFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frame.transform.SetParent(content, false);

                var frameRect = frame.GetComponent<RectTransform>();
                frameRect.anchorMin = new Vector2(1f, 1f);
                frameRect.anchorMax = new Vector2(1f, 1f);
                frameRect.pivot = new Vector2(1f, 1f);
                frameRect.anchoredPosition = new Vector2(-24f, -24f);
                frameRect.sizeDelta = new Vector2(232f, 232f);

                var background = frame.GetComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.55f);
                background.raycastTarget = false;

                var view = new GameObject("MinimapImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                view.transform.SetParent(frame.transform, false);

                var viewRect = view.GetComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.pivot = new Vector2(0.5f, 0.5f);
                viewRect.offsetMin = new Vector2(6f, 6f);
                viewRect.offsetMax = new Vector2(-6f, -6f);

                var raw = view.GetComponent<RawImage>();
                raw.texture = rt;
                raw.raycastTarget = false;

                var arrow = new GameObject("MinimapArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                arrow.transform.SetParent(view.transform, false);

                var arrowRect = arrow.GetComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
                arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
                arrowRect.pivot = new Vector2(0.5f, 0.5f);
                arrowRect.anchoredPosition = Vector2.zero;
                arrowRect.sizeDelta = new Vector2(11f, 11f);
                arrowRect.localEulerAngles = new Vector3(0f, 0f, 45f);

                var arrowImage = arrow.GetComponent<Image>();
                arrowImage.color = new Color(0.55f, 0.95f, 1f, 1f);
                arrowImage.raycastTarget = false;

                var hud = root.GetComponent<HudController>();

                if (hud != null)
                {
                    var serialized = new SerializedObject(hud);
                    serialized.FindProperty("minimapArrow").objectReferenceValue = arrowRect;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
                log.AppendLine("[Minimap] hud frame authored on HUD.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void VerifyMinimap(StringBuilder log)
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            Transform cameraTransform = player != null ? player.transform.Find("MinimapCamera") : null;
            var camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            var minimap = cameraTransform != null ? cameraTransform.GetComponent<MinimapCamera>() : null;

            log.AppendLine($"[Minimap] verify camera={(camera != null)} minimapScript={(minimap != null)}");
            if (camera != null)
            {
                log.AppendLine($"[Minimap] verify ortho={camera.orthographic} size={camera.orthographicSize} mask={camera.cullingMask} rt={(camera.targetTexture != null ? camera.targetTexture.name : "null")} depth={camera.depth}");
            }

            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            Transform content = hud != null ? hud.transform.Find("Content") : null;
            Transform frame = content != null ? content.Find("MinimapFrame") : null;
            Transform view = frame != null ? frame.Find("MinimapImage") : null;
            Transform arrow = view != null ? view.Find("MinimapArrow") : null;
            var raw = view != null ? view.GetComponent<RawImage>() : null;

            log.AppendLine($"[Minimap] verify frame={(frame != null)} view={(view != null)} arrow={(arrow != null)}");
            if (raw != null)
            {
                log.AppendLine($"[Minimap] verify rawTexture={(raw.texture != null ? raw.texture.name : "null")}");
            }

            if (frame != null)
            {
                var rect = frame.GetComponent<RectTransform>();
                log.AppendLine($"[Minimap] verify anchor={rect.anchorMin} pos={rect.anchoredPosition} size={rect.sizeDelta}");
            }

            var controller = hud != null ? hud.GetComponent<HudController>() : null;

            if (controller != null)
            {
                var serialized = new SerializedObject(controller);
                var field = serialized.FindProperty("minimapArrow");
                log.AppendLine($"[Minimap] verify hudControllerArrow={(field != null && field.objectReferenceValue != null)}");
            }
        }

        /// <summary>
        /// Runtime proof for the minimap without a GUI: a scratch scene, one hero prefab instance, a few
        /// frames of play mode, then the camera's world transform is read back. Nothing here is saved.
        /// </summary>
        public static void PlayProbe()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            GameObject hero = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            hero.transform.position = new Vector3(5f, 0.1f, -7f);
            hero.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            hero.SetActive(true);

            var log = new StringBuilder();
            int frames = 0;

            EditorApplication.update += Tick;

            void Tick()
            {
                frames++;

                if (frames < 60)
                {
                    return;
                }

                EditorApplication.update -= Tick;

                Transform cameraTransform = hero.transform.Find("MinimapCamera");
                var camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;

                log.AppendLine($"[PlayProbe] frames={frames} camera={(camera != null)}");
                if (cameraTransform != null)
                {
                    log.AppendLine($"[PlayProbe] hero pos={hero.transform.position} yaw={hero.transform.eulerAngles.y}");
                    log.AppendLine($"[PlayProbe] cam pos={cameraTransform.position} rot={cameraTransform.eulerAngles} active={cameraTransform.gameObject.activeInHierarchy}");
                }

                if (camera != null)
                {
                    log.AppendLine($"[PlayProbe] ortho={camera.orthographic} size={camera.orthographicSize} rt={(camera.targetTexture != null ? camera.targetTexture.name : "null")}");
                }

                Debug.Log(log.ToString());
                EditorApplication.ExitPlaymode();
                EditorApplication.Exit(0);
            }

            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        /// Authors the Tab frag board (requirement #9) into HUD.prefab: a centred panel with name / kills /
        /// deaths columns and an enemies-alive footer, plus <see cref="ScoreService"/> and
        /// <see cref="FragBoardUI"/> on the canvas root. Hidden by default; only Tab turns it on.
        /// </summary>
        public static void SetupFragBoard()
        {
            var log = new StringBuilder();
            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform content = root.transform.Find("Content");

                if (content == null)
                {
                    log.AppendLine("[FragBoard] FAIL no Content node");
                    Debug.Log(log.ToString());
                    EditorApplication.Exit(1);
                    return;
                }

                Transform old = content.Find("FragBoard");

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                Font font = root.GetComponentInChildren<Text>(true) != null
                    ? root.GetComponentInChildren<Text>(true).font
                    : null;

                var panel = new GameObject("FragBoard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(content, false);

                var panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(460f, 240f);

                var background = panel.GetComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.78f);
                background.raycastTarget = false;

                var title = CreateHudText(panel.transform, "Title", "FRAGS", 24, TextAnchor.MiddleCenter, font,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(440f, 30f), Color.white);

                var headerName = CreateHudText(panel.transform, "HeaderName", "HERO", 14, TextAnchor.MiddleLeft, font,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(18f, -48f), new Vector2(240f, 20f), new Color(0.65f, 0.68f, 0.72f, 1f));
                var headerKills = CreateHudText(panel.transform, "HeaderKills", "KILLS", 14, TextAnchor.MiddleCenter, font,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-110f, -48f), new Vector2(90f, 20f), new Color(0.65f, 0.68f, 0.72f, 1f));
                var headerDeaths = CreateHudText(panel.transform, "HeaderDeaths", "DEATHS", 14, TextAnchor.MiddleCenter, font,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-18f, -48f), new Vector2(90f, 20f), new Color(0.65f, 0.68f, 0.72f, 1f));

                var nameText = CreateHudText(panel.transform, "HeroName", "YOU", 20, TextAnchor.MiddleLeft, font,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(18f, -74f), new Vector2(240f, 26f), Color.white);
                var killsText = CreateHudText(panel.transform, "Kills", "0", 20, TextAnchor.MiddleCenter, font,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-110f, -74f), new Vector2(90f, 26f), Color.white);
                var deathsText = CreateHudText(panel.transform, "Deaths", "0", 20, TextAnchor.MiddleCenter, font,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-18f, -74f), new Vector2(90f, 26f), Color.white);

                var footer = CreateHudText(panel.transform, "Footer", "ENEMIES ALIVE  0", 14, TextAnchor.MiddleCenter, font,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 12f), new Vector2(440f, 22f), new Color(0.65f, 0.68f, 0.72f, 1f));

                panel.SetActive(false);

                var score = root.GetComponent<ScoreService>();

                if (score == null)
                {
                    score = root.AddComponent<ScoreService>();
                }

                var board = root.GetComponent<FragBoardUI>();

                if (board == null)
                {
                    board = root.AddComponent<FragBoardUI>();
                }

                var serialized = new SerializedObject(board);
                serialized.FindProperty("panel").objectReferenceValue = panel;
                serialized.FindProperty("nameText").objectReferenceValue = nameText;
                serialized.FindProperty("killsText").objectReferenceValue = killsText;
                serialized.FindProperty("deathsText").objectReferenceValue = deathsText;
                serialized.FindProperty("footerText").objectReferenceValue = footer;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
                log.AppendLine($"[FragBoard] authored font={(font != null ? font.name : "null")} columns=name/kills/deaths headerRows=2");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            Transform reloaded = hud != null ? hud.transform.Find("Content/FragBoard") : null;
            var boardComponent = hud != null ? hud.GetComponent<FragBoardUI>() : null;
            var scoreComponent = hud != null ? hud.GetComponent<ScoreService>() : null;

            log.AppendLine($"[FragBoard] verify panel={(reloaded != null)} activeSelf={(reloaded != null && reloaded.gameObject.activeSelf)} children={(reloaded != null ? reloaded.childCount : 0)}");
            log.AppendLine($"[FragBoard] verify score={(scoreComponent != null)} board={(boardComponent != null)}");

            if (boardComponent != null)
            {
                var serialized = new SerializedObject(boardComponent);
                log.AppendLine($"[FragBoard] verify refs panel={(serialized.FindProperty("panel").objectReferenceValue != null)} name={(serialized.FindProperty("nameText").objectReferenceValue != null)} kills={(serialized.FindProperty("killsText").objectReferenceValue != null)} deaths={(serialized.FindProperty("deathsText").objectReferenceValue != null)} footer={(serialized.FindProperty("footerText").objectReferenceValue != null)}");
            }

            Debug.Log(log.ToString());
            EditorApplication.Exit(0);
        }

        static Text CreateHudText(Transform parent, string name, string content, int fontSize, TextAnchor alignment,
            Font font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
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

        /// <summary>
        /// Runtime proof for the frag board (requirement #9): a real map scene in play mode, one enemy killed
        /// through <see cref="Health"/>, then Tab injected through a virtual keyboard device so
        /// <see cref="InputReader.FragsHeld"/> is exercised for real, not bypassed.
        /// </summary>
        public static void PlayProbeFrags()
        {
            var log = new StringBuilder();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/20_Map_Arena.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            var score = default(ScoreService);
            var keyboard = default(Keyboard);
            var board = default(FragBoardUI);
            int stageFrame = 0;
            int stage = 0;

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
                    log.AppendLine($"[Frags] EXCEPTION {exception.GetType().Name}: {exception.Message}");
                    Debug.Log(log.ToString());
                    EditorApplication.ExitPlaymode();
                    EditorApplication.Exit(0);
                }
            }

            void Step()
            {
                if (stage == 0 && Time.frameCount > 90)
                {
                    score = Object.FindFirstObjectByType<ScoreService>();
                    log.AppendLine($"[Frags] frame={Time.frameCount} A score={(score != null)} alive={(score != null ? score.EnemiesAlive : -1)} kills={(score != null ? score.Kills : -1)} deaths={(score != null ? score.Deaths : -1)}");

                    var brain = Object.FindFirstObjectByType<Enemies.EnemyBrain>();
                    GameObject enemy = brain != null ? brain.gameObject : null;
                    var health = enemy != null ? enemy.GetComponent<Health>() : null;
                    log.AppendLine($"[Frags] A enemy={(enemy != null)} layer={(enemy != null ? enemy.layer : -1)} hp={(health != null ? health.Current : -1f)}");

                    if (health != null)
                    {
                        health.TakeDamage(new DamageInfo(9999f, enemy.transform.position, Vector3.up, null));
                    }

                    stage = 1;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 1 && Time.frameCount > stageFrame + 5)
                {
                    log.AppendLine($"[Frags] frame={Time.frameCount} B after kill alive={(score != null ? score.EnemiesAlive : -1)} kills={(score != null ? score.Kills : -1)}");

                    keyboard = InputSystem.AddDevice<Keyboard>();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
                    InputSystem.Update();

                    stage = 2;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 2 && Time.frameCount > stageFrame + 5)
                {
                    var hud = GameObject.Find("HUD");
                    board = hud != null ? hud.GetComponent<FragBoardUI>() : null;
                    Transform panel = hud != null ? hud.transform.Find("Content/FragBoard") : null;

                    log.AppendLine($"[Frags] frame={Time.frameCount} C tabPressed={(keyboard != null && keyboard.tabKey.isPressed)} board={(board != null)} panelActive={(panel != null && panel.gameObject.activeSelf)} panelFound={(panel != null)}");
                    log.AppendLine($"[Frags] C keyboardCurrent={(Keyboard.current != null)} same={(Keyboard.current == keyboard)} fragsHeld={(bool)typeof(HeroShooter.Player.InputReader).GetProperty("FragsHeld").GetValue(null)}");
                    if (board != null)
                    {
                        var serializedPanel = new SerializedObject(board).FindProperty("panel").objectReferenceValue as GameObject;
                        log.AppendLine($"[Frags] C refPanel={(serializedPanel != null ? serializedPanel.name : "null")} refActive={(serializedPanel != null && serializedPanel.activeSelf)} contentActive={(hud != null && hud.transform.Find("Content") != null && hud.transform.Find("Content").gameObject.activeSelf)}");
                        log.AppendLine($"[Frags] C boardHost={board.gameObject.name} enabled={board.enabled} activeAndEnabled={board.isActiveAndEnabled} updates={FragBoardUI.UpdateCount}");

                        var hudController = hud != null ? hud.GetComponent<HudController>() : null;
                        if (hudController != null)
                        {
                            var hudSerialized = new SerializedObject(hudController);
                            var healthText = hudSerialized.FindProperty("healthText").objectReferenceValue as Text;
                            var arrow = hudSerialized.FindProperty("minimapArrow").objectReferenceValue as RectTransform;
                            log.AppendLine($"[Frags] C hudController health='{(healthText != null ? healthText.text : "?")}' arrowZ={(arrow != null ? arrow.localEulerAngles.z : -1f)}");
                        }

                        // Decisive: call Update once by hand. If the panel flips here but not on its own,
                        // the player loop is not invoking this component; if it still does nothing, the
                        // component's own read of the Tab key differs from InputReader's.
                        typeof(FragBoardUI).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(board, null);
                        log.AppendLine($"[Frags] C afterManualUpdate active={(serializedPanel != null && serializedPanel.activeSelf)} kills='{((Text)(new SerializedObject(board).FindProperty("killsText").objectReferenceValue)).text}'");
                    }

                    if (board != null)
                    {
                        var serialized = new SerializedObject(board);
                        var nameText = serialized.FindProperty("nameText").objectReferenceValue as Text;
                        var killsText = serialized.FindProperty("killsText").objectReferenceValue as Text;
                        var deathsText = serialized.FindProperty("deathsText").objectReferenceValue as Text;
                        var footerText = serialized.FindProperty("footerText").objectReferenceValue as Text;
                        log.AppendLine($"[Frags] C row='{(nameText != null ? nameText.text : "?")}' kills='{(killsText != null ? killsText.text : "?")}' deaths='{(deathsText != null ? deathsText.text : "?")}' footer='{(footerText != null ? footerText.text : "?")}'");
                    }

                    if (keyboard != null)
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        InputSystem.Update();
                    }

                    stage = 3;
                    stageFrame = Time.frameCount;
                    return;
                }

                if (stage == 3 && Time.frameCount > stageFrame + 5)
                {
                    EditorApplication.update -= Tick;
                    var hud = GameObject.Find("HUD");
                    Transform panel = hud != null ? hud.transform.Find("Content/FragBoard") : null;
                    log.AppendLine($"[Frags] frame={Time.frameCount} D after release panelActive={(panel != null && panel.gameObject.activeSelf)}");

                    Debug.Log(log.ToString());
                    EditorApplication.ExitPlaymode();
                    EditorApplication.Exit(0);
                }
            }
        }

        static void Dump(StringBuilder log, Transform t, string indent)
        {
            var rect = t as RectTransform;
            string layout = rect == null
                ? string.Empty
                : $" anchorMin={rect.anchorMin} anchorMax={rect.anchorMax} pivot={rect.pivot} pos={rect.anchoredPosition} size={rect.sizeDelta}";

            var components = new StringBuilder();
            foreach (Component component in t.GetComponents<Component>())
            {
                components.Append(component == null ? "MISSING" : component.GetType().Name).Append(',');
            }

            log.AppendLine($"[Probe] {indent}{t.name} layer={t.gameObject.layer} active={t.gameObject.activeSelf}{layout} comps={components}");

            for (int i = 0; i < t.childCount; i++)
            {
                Dump(log, t.GetChild(i), indent + "  ");
            }
        }
    }
}
