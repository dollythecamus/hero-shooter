using HeroShooter.Data;
using HeroShooter.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HeroShooter.Systems
{
    /// <summary>
    /// The character select screen. It builds itself at runtime (canvas, event system, one card per roster
    /// entry) so selecting a character needs no authored UI scene — the scene only holds this component and a
    /// reference to the bootstrap. Requirement: "selecionar entre 3 personagens".
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _cardRoot;
        Font _font;

        public bool IsVisible => _canvas != null && _canvas.gameObject.activeSelf;

        public void Show(CharacterDefinition[] roster, MatchBootstrap bootstrap)
        {
            EnsureCanvas();
            ClearCards();

            if (roster == null || roster.Length == 0)
            {
                return;
            }

            for (int i = 0; i < roster.Length; i++)
            {
                if (roster[i] != null)
                {
                    CreateCard(roster[i], i, bootstrap);
                }
            }

            _canvas.gameObject.SetActive(true);
            InputReader.SetCursorLocked(false);
        }

        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasGo = new GameObject("CharacterSelectCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventGo = new GameObject("EventSystem");
                eventGo.transform.SetParent(transform, false);
                eventGo.AddComponent<EventSystem>();
                eventGo.AddComponent<InputSystemUIInputModule>();
            }

            // Dim the game behind the panel.
            RectTransform background = CreateRect("Background", canvasGo.transform);
            Stretch(background);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);

            Text title = CreateText(background, "HERO SHOOTER — choose your hero", 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 380f), new Vector2(1600f, 70f));
            title.fontStyle = FontStyle.Bold;

            Text subtitle = CreateText(background, "rock beats scissors · scissors beats paper · paper beats rock",
                24, TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(1600f, 40f));
            subtitle.color = new Color(0.7f, 0.75f, 0.8f);

            _cardRoot = CreateRect("Cards", background);
            Stretch(_cardRoot);
        }

        void ClearCards()
        {
            if (_cardRoot == null)
            {
                return;
            }

            for (int i = _cardRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardRoot.GetChild(i).gameObject);
            }
        }

        void CreateCard(CharacterDefinition character, int index, MatchBootstrap bootstrap)
        {
            float y = 190f - index * 190f;

            RectTransform card = CreateRect("Card_" + character.displayName, _cardRoot);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, y);
            card.sizeDelta = new Vector2(900f, 160f);

            Image cardImage = card.gameObject.AddComponent<Image>();
            Color tint = character.color;
            cardImage.color = new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.35f, 0.95f);

            Button button = card.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.35f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.6f);
            button.colors = colors;

            int captured = index;
            button.onClick.AddListener(() => bootstrap.Select(captured));

            Text name = CreateText(card, character.displayName.ToUpperInvariant(), 40, TextAnchor.UpperLeft,
                new Vector2(0f, 34f), new Vector2(820f, 50f));
            name.fontStyle = FontStyle.Bold;
            name.color = Color.white;

            Text pitch = CreateText(card, character.pitch, 20, TextAnchor.UpperLeft, new Vector2(0f, -8f),
                new Vector2(820f, 40f));
            pitch.color = new Color(0.85f, 0.88f, 0.92f);

            string abilities = AbilityLine(character.abilityPrimary, "Q") + "    " + AbilityLine(character.abilitySecondary, "E");
            Text abilityLine = CreateText(card, abilities, 19, TextAnchor.LowerLeft, new Vector2(0f, -46f),
                new Vector2(820f, 36f));
            abilityLine.color = new Color(1f, 0.9f, 0.6f);
        }

        static string AbilityLine(AbilityDefinition ability, string slot)
        {
            if (ability == null)
            {
                return slot + ": —";
            }

            return slot + ": " + ability.displayName + " (" + ability.cooldown.ToString("0") + "s)";
        }

        RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Text CreateText(RectTransform parent, string content, int size, TextAnchor anchor, Vector2 position,
            Vector2 boxSize)
        {
            RectTransform rect = CreateRect("Text", parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = content;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
