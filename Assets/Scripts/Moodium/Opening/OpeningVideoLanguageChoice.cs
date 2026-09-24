using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Moodium.Opening
{
    /// <summary>World-space language picker shown during the configured video segment.</summary>
    public sealed class OpeningVideoLanguageChoice : MonoBehaviour
    {
        public bool HasSelection { get; private set; }
        public string SelectedLanguage { get; private set; }
        GameObject m_Chinese;
        GameObject m_English;
        readonly OpeningVideoSpatialChoiceGate m_TransitionGate = new OpeningVideoSpatialChoiceGate();

        public bool IsReadyForVideoTransition =>
            m_TransitionGate.IsReadyForVideoTransition(Time.frameCount, false);

        public void Configure(TMP_FontAsset font, Sprite roundedSprite)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.AddComponent<OpeningVideoSpatialUIInputManager>();
            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 340f);

            var question = CreateText("Language Question", "你更希望 Moodi 用哪种语言陪你探索？\nWhich language would you like Moodi to use?", font, 36f);
            question.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            question.rectTransform.sizeDelta = new Vector2(860f, 115f);
            m_Chinese = CreateButton("Language Button - Chinese", "中文", new Vector2(-190f, -160f), font, roundedSprite, "中文");
            m_English = CreateButton("Language Button - English", "English", new Vector2(190f, -160f), font, roundedSprite, "English");
        }

        public void SelectChineseWhenTimedOut() => SelectAutomatically("中文");

        TextMeshProUGUI CreateText(string name, string value, TMP_FontAsset font, float size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(transform, false);
            var text = item.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.outlineWidth = 0.08f;
            text.outlineColor = new Color(.08f, .02f, .16f, .85f);
            return text;
        }

        GameObject CreateButton(string name, string label, Vector2 position, TMP_FontAsset font, Sprite roundedSprite, string value)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(transform, false);
            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(285f, 94f);
            rect.anchoredPosition = position;
            var image = item.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = roundedSprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.color = new Color(.055f, .08f, .16f, .9f);
            var outline = item.AddComponent<Outline>();
            outline.effectColor = new Color(.42f, .58f, .95f, .65f);
            outline.effectDistance = new Vector2(1f, -1f);
            var spatialButton = item.AddComponent<OpeningVideoSpatialButton>();
            spatialButton.Configure(touchId => SelectFromPress(value, touchId));
            var button = item.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => spatialButton.OnPointerClick(null));
            var collider = item.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.size = new Vector3(285f, 94f, 2f);
            var text = CreateText("Label", label, font, 36f);
            text.transform.SetParent(item.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return item;
        }

        void SelectFromPress(string value, int touchId)
        {
            if (HasSelection)
                return;
            SelectedLanguage = value;
            if (touchId >= 0)
                m_TransitionGate.SelectFromSpatialTouch(Time.frameCount, touchId);
            else
                m_TransitionGate.SelectAutomatically(Time.frameCount);
            HasSelection = true;
            Debug.Log($"[Moodium Opening] Video language selected by {(touchId >= 0 ? "spatial pointer" : "UI click")}: {value}.");
        }

        void SelectAutomatically(string value)
        {
            if (HasSelection)
                return;
            SelectedLanguage = value;
            m_TransitionGate.SelectAutomatically(Time.frameCount);
            HasSelection = true;
            Debug.Log($"[Moodium Opening] Video language selected automatically: {value}.");
        }

    }
}
