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

        float m_SelectedAt = float.NegativeInfinity;
        public bool IsReadyForVideoTransition =>
            Time.unscaledTime-m_SelectedAt >= .3f && m_TransitionGate.IsReadyForVideoTransition(Time.frameCount, false);

        public void Configure(TMP_FontAsset font, Sprite roundedSprite)
        {
            gameObject.AddComponent<Unity.PolySpatial.VisionOSSortingGroup>();
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
            m_Chinese = CreateButton("Language Button - Chinese", new Vector2(-179f, -160f), "中文", "lang_zh");
            m_English = CreateButton("Language Button - English", new Vector2(179f, -160f), "English", "lang_en");
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

        GameObject CreateButton(string name, Vector2 position, string value, string assetId)
        {
            var renderer=Moodium.UI.UIRefreshVisuals.Sprite(transform,name,"MoodiumOpening/opening_btn_"+assetId+"_idle",Vector3.zero,10);
            var item=renderer.gameObject;
            Moodium.UI.UIRefreshVisuals.UnitScale(item.transform);
            var display=transform.parent;
            item.transform.position=display!=null
                // Keep the buttons just inside the video's bottom edge, clear of the window move bar.
                ? display.TransformPoint(new Vector3(0,-.5f,0))+display.right*(position.x<0?-.1f:.1f)+display.up*.04f-display.forward*.003f
                : transform.position+transform.right*(position.x<0?-.1f:.1f);
            var feedback=item.AddComponent<OpeningChoiceSpriteFeedback>();feedback.Configure(assetId);
            var spatialButton=item.AddComponent<OpeningVideoSpatialButton>();
            spatialButton.Configure(touchId=>{if(HasSelection)return;feedback.SetPressed();SelectFromPress(value, touchId);});
            var button=item.AddComponent<Button>();button.transition=Selectable.Transition.None;
            button.onClick.AddListener(()=>spatialButton.OnPointerClick(null));
            item.AddComponent<BoxCollider>().size=new Vector3(.178f,.061f,.01f);
            item.AddComponent<Unity.PolySpatial.VisionOSHoverEffect>().Type=Unity.PolySpatial.VisionOSHoverEffect.EffectType.Highlight;
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
            m_SelectedAt=Time.unscaledTime;
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
