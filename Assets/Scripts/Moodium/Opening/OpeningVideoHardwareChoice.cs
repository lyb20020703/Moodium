using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Moodium.Opening
{
    /// <summary>Hardware choice shown over the selected-language tutorial video.</summary>
    public sealed class OpeningVideoHardwareChoice : MonoBehaviour
    {
        public bool HasSelection { get; private set; }
        public bool HasHardware { get; private set; }
        GameObject m_Yes;
        GameObject m_No;
        readonly OpeningVideoSpatialChoiceGate m_TransitionGate = new OpeningVideoSpatialChoiceGate();

        public bool IsReadyForVideoTransition =>
            m_TransitionGate.IsReadyForVideoTransition(Time.frameCount, false);

        public void Configure(TMP_FontAsset font, Sprite roundedSprite, bool english)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.AddComponent<OpeningVideoSpatialUIInputManager>();
            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 340f);

            var question = CreateText("Hardware Question",
                english
                    ? "Do you have a Moodium sensory bag with you?"
                    : "你手边有 Moodium 的感官袋吗？",
                font, 30f);
            question.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            question.rectTransform.sizeDelta = new Vector2(860f, 130f);
            m_Yes = CreateButton("Hardware Button - Yes", new Vector2(-165f, -160f), english ? "bag_yes_en" : "bag_yes_zh", true);
            m_No = CreateButton("Hardware Button - No", new Vector2(165f, -160f), english ? "bag_no_en" : "bag_no_zh", false);
        }

        TextMeshProUGUI CreateText(string name, string value, TMP_FontAsset font, float size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(transform, false);
            var text = item.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.color = Color.white;
            text.outlineWidth = 0.08f;
            text.outlineColor = new Color(.08f, .02f, .16f, .85f);
            return text;
        }

        GameObject CreateButton(string name, Vector2 position, string assetId, bool hasHardware)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(transform, false);
            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(330f, 114f);
            rect.anchoredPosition = position;
            var image = item.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("MoodiumOpening/opening_btn_" + assetId + "_idle");
            image.type = Image.Type.Simple;
            var spatialButton = item.AddComponent<OpeningVideoSpatialButton>();
            spatialButton.Configure(touchId => { image.sprite = Resources.Load<Sprite>("MoodiumOpening/opening_btn_" + assetId + "_pressed"); SelectFromPress(hasHardware, touchId); });
            var button = item.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => spatialButton.OnPointerClick(null));
            var collider = item.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.size = new Vector3(330f, 114f, 2f);
            return item;
        }

        void SelectFromPress(bool hasHardware, int touchId)
        {
            if (HasSelection)
                return;
            HasHardware = hasHardware;
            Moodium.Flow.SensoryBagOwnership.SetHasBag(hasHardware);
            if (touchId >= 0)
                m_TransitionGate.SelectFromSpatialTouch(Time.frameCount, touchId);
            else
                m_TransitionGate.SelectAutomatically(Time.frameCount);
            HasSelection = true;
            Debug.Log($"[Moodium Opening] Hardware choice selected by {(touchId >= 0 ? "spatial pointer" : "UI click")}: {(hasHardware ? "available" : "not available")}.");
        }

    }
}
