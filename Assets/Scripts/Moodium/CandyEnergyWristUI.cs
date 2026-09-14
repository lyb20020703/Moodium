using Moodium.Audio;
using Moodium.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.CandyWorld
{
    public sealed class CandyEnergyWristUI : MonoBehaviour
    {
        static readonly Color LowColor = FromHex(0xD8B4FE);
        static readonly Color HighColor = FromHex(0xF0ABFC);

        readonly string[] m_Encouragements =
        {
            "Nice!", "Sweet moment", "Keep going", "Beautiful", "Almost blooming"
        };

        [SerializeField] Vector3 m_WristOffset = new(-0.035f, 0.055f, 0.015f);
        [SerializeField, Min(1f)] float m_FollowResponsiveness = 14f;

        Camera m_Camera;
        XRHandSubsystem m_HandSubsystem;
        CanvasGroup m_CanvasGroup;
        RectTransform m_CanvasRect;
        RectTransform m_FillRect;
        Image m_Background;
        Image m_Glow;
        Image m_Fill;
        TMP_Text m_Percentage;
        TMP_Text m_Feedback;
        Image[] m_Sparkles;
        Sprite m_RoundedSprite;
        Sprite m_CircleSprite;

        float m_TargetProgress;
        float m_DisplayedProgress;
        float m_TargetEnergy;
        float m_DisplayedEnergy;
        float m_LastTargetEnergy;
        float m_FeedbackTime = -1f;
        bool m_Completed;
        bool m_Built;

        public float DisplayedProgress => m_DisplayedProgress;
        public bool HandTracked { get; private set; }

        public void Build(Camera camera, TMP_FontAsset font, Sprite roundedSprite, Sprite circleSprite)
        {
            m_Camera = camera != null ? camera : Camera.main;
            m_RoundedSprite = roundedSprite;
            m_CircleSprite = circleSprite != null ? circleSprite : roundedSprite;

            var canvasObject = new GameObject(
                "Candy Energy Wrist World Space Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            m_CanvasRect = (RectTransform)canvasObject.transform;
            m_CanvasRect.sizeDelta = new Vector2(430f, 108f);
            m_CanvasRect.localScale = Vector3.one * 0.00029f;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            m_CanvasGroup = canvasObject.GetComponent<CanvasGroup>();

            m_Glow = CreateImage("Soft Purple Glow", m_CanvasRect, Vector2.zero, new Vector2(438f, 116f), roundedSprite,
                new Color(0.68f, 0.34f, 1f, 0.17f));
            m_Background = CreateImage("Glass Capsule", m_CanvasRect, Vector2.zero, new Vector2(420f, 96f), roundedSprite,
                new Color(0.10f, 0.065f, 0.18f, 0.78f));

            CreateImage("Avatar Placeholder", m_CanvasRect, new Vector2(-170f, 0f), new Vector2(64f, 64f), m_CircleSprite,
                new Color(0.83f, 0.71f, 1f, 0.95f));
            CreateText("M", m_CanvasRect, new Vector2(-170f, 0f), new Vector2(54f, 54f), 25f, font, Color.white);
            CreateText("Candy Energy", m_CanvasRect, new Vector2(-53f, 21f), new Vector2(210f, 28f), 19f, font,
                new Color(1f, 1f, 1f, 0.93f));

            CreateImage("Progress Track", m_CanvasRect, new Vector2(39f, -17f), new Vector2(250f, 30f), roundedSprite,
                new Color(0.05f, 0.035f, 0.09f, 0.78f));
            m_Fill = CreateImage("Progress Fill", m_CanvasRect, new Vector2(-83f, -17f), new Vector2(6f, 26f), roundedSprite, LowColor);
            m_FillRect = m_Fill.rectTransform;
            m_FillRect.pivot = new Vector2(0f, 0.5f);
            m_Percentage = CreateText("0%", m_CanvasRect, new Vector2(39f, -17f), new Vector2(245f, 30f), 17f, font,
                Color.white);

            CreateImage("Completion Badge", m_CanvasRect, new Vector2(184f, 0f), new Vector2(45f, 45f), m_CircleSprite,
                new Color(0.66f, 0.46f, 0.88f, 0.62f));
            CreateText("✦", m_CanvasRect, new Vector2(184f, 1f), new Vector2(38f, 38f), 22f, font, Color.white);

            m_Feedback = CreateText(string.Empty, m_CanvasRect, new Vector2(0f, 67f), new Vector2(360f, 70f), 20f, font,
                new Color(0.94f, 0.72f, 1f, 0f));
            m_Feedback.alignment = TextAlignmentOptions.Center;

            m_Sparkles = new Image[3];
            m_Sparkles[0] = CreateImage("Sparkle 1", m_CanvasRect, new Vector2(-115f, 48f), new Vector2(10f, 10f), m_CircleSprite, Color.clear);
            m_Sparkles[1] = CreateImage("Sparkle 2", m_CanvasRect, new Vector2(115f, 45f), new Vector2(7f, 7f), m_CircleSprite, Color.clear);
            m_Sparkles[2] = CreateImage("Sparkle 3", m_CanvasRect, new Vector2(150f, -40f), new Vector2(6f, 6f), m_CircleSprite, Color.clear);

            m_Built = true;
            m_CanvasGroup.alpha = 0f;
            SetProgress(0f, 0f);
        }

        public void SetProgress(float energy, float normalized)
        {
            m_TargetProgress = Mathf.Clamp01(normalized);
            m_TargetEnergy = Mathf.Max(0f, energy);
            var gained = m_TargetEnergy - m_LastTargetEnergy;
            if (m_Built && gained > 0.01f)
                ShowFeedbackText(gained);
            m_LastTargetEnergy = m_TargetEnergy;

            if (!m_Completed && m_TargetProgress >= 0.999f)
            {
                m_Completed = true;
                ShowFeedbackText(gained, true);
                MoodiumAudioManager.Play(MoodiumAudioCue.CandyEnergyComplete, transform.position);
                MoodiumInteractionVFXManager.PlayInteractionEffect(transform.position, transform.rotation, gameObject);
            }
            else if (m_TargetProgress < 0.999f)
            {
                m_Completed = false;
            }
        }

        public void ShowFeedbackText(float gained, bool completed = false)
        {
            if (m_Feedback == null)
                return;
            if (completed)
                m_Feedback.text = "Wonderful!\nYour candy world is blooming";
            else
            {
                var index = Mathf.Clamp(Mathf.FloorToInt(m_TargetProgress * m_Encouragements.Length), 0, m_Encouragements.Length - 1);
                m_Feedback.text = $"{m_Encouragements[index]}\n+{Mathf.RoundToInt(gained)} Candy Energy";
            }
            m_FeedbackTime = 0f;
        }

        void LateUpdate()
        {
            if (!m_Built)
                return;
            UpdateWristPose();
            UpdateProgressAnimation();
            UpdateFeedbackAnimation();
            UpdateStateVisuals();
        }

        void UpdateWristPose()
        {
            var tracked = TryGetLeftWristPose(out var wristPose);
#if UNITY_EDITOR
            if (!tracked && m_Camera != null)
            {
                var forward = Vector3.ProjectOnPlane(m_Camera.transform.forward, Vector3.up).normalized;
                wristPose = new Pose(m_Camera.transform.position + forward * 0.65f - Vector3.up * 0.20f, Quaternion.identity);
                tracked = true;
            }
#endif
            HandTracked = tracked;
            m_CanvasGroup.alpha = Mathf.MoveTowards(m_CanvasGroup.alpha, tracked ? 1f : 0f, Time.deltaTime * 8f);
            if (!tracked)
                return;

            var targetPosition = wristPose.position + wristPose.rotation * m_WristOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-m_FollowResponsiveness * Time.deltaTime));
            if (m_Camera != null)
            {
                var awayFromUser = Vector3.ProjectOnPlane(targetPosition - m_Camera.transform.position, Vector3.up).normalized;
                if (awayFromUser.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(awayFromUser, Vector3.up),
                        1f - Mathf.Exp(-m_FollowResponsiveness * Time.deltaTime));
            }
        }

        bool TryGetLeftWristPose(out Pose pose)
        {
            pose = Pose.identity;
            if (m_HandSubsystem == null)
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (m_HandSubsystem == null || !m_HandSubsystem.running)
                return false;
            var flags = m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
            if ((flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) == 0 && !m_HandSubsystem.leftHand.isTracked)
                return false;
            var wrist = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.Wrist);
            return wrist.trackingState != XRHandJointTrackingState.None && wrist.TryGetPose(out pose);
        }

        void UpdateProgressAnimation()
        {
            m_DisplayedProgress = Mathf.Lerp(m_DisplayedProgress, m_TargetProgress, 1f - Mathf.Exp(-5.5f * Time.deltaTime));
            m_DisplayedEnergy = Mathf.MoveTowards(m_DisplayedEnergy, m_TargetEnergy, Time.deltaTime * 24f);
            var width = Mathf.Lerp(6f, 244f, m_DisplayedProgress);
            m_FillRect.sizeDelta = new Vector2(width, 26f);
            var color = Color.Lerp(LowColor, HighColor, Mathf.SmoothStep(0f, 1f, m_DisplayedProgress));
            color.a = 0.96f;
            m_Fill.color = color;
            m_Percentage.text = $"{Mathf.RoundToInt(m_DisplayedEnergy)}%";
        }

        void UpdateFeedbackAnimation()
        {
            if (m_FeedbackTime < 0f)
                return;
            m_FeedbackTime += Time.deltaTime;
            var progress = Mathf.Clamp01(m_FeedbackTime / 1.35f);
            var alpha = progress < 0.18f ? progress / 0.18f : 1f - Mathf.InverseLerp(0.62f, 1f, progress);
            var color = new Color(0.94f, 0.72f, 1f, Mathf.Clamp01(alpha));
            m_Feedback.color = color;
            m_Feedback.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(48f, 92f, progress));
            if (progress >= 1f)
                m_FeedbackTime = -1f;
        }

        void UpdateStateVisuals()
        {
            var enhanced = Mathf.InverseLerp(0.5f, 1f, m_DisplayedProgress);
            var anticipation = Mathf.InverseLerp(0.8f, 1f, m_DisplayedProgress);
            m_Glow.color = new Color(0.69f, 0.35f, 1f, Mathf.Lerp(0.12f, 0.35f, enhanced));
            m_Background.color = new Color(0.10f, 0.065f, 0.18f, Mathf.Lerp(0.72f, 0.88f, enhanced));
            var pulse = 1f + anticipation * (Mathf.Sin(Time.unscaledTime * 4.2f) * 0.018f);
            m_CanvasRect.localScale = Vector3.one * (0.00029f * pulse);
            for (var i = 0; i < m_Sparkles.Length; i++)
            {
                var sparkle = Mathf.Clamp01(enhanced * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (2.1f + i * 0.35f) + i)));
                m_Sparkles[i].color = new Color(0.95f, 0.76f, 1f, sparkle * 0.75f);
            }
        }

        static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text CreateText(string value, Transform parent, Vector2 position, Vector2 size, float fontSize,
            TMP_FontAsset font, Color color)
        {
            var item = new GameObject($"Text - {value}", typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = item.GetComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        static Color FromHex(uint rgb) => new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            1f);
    }
}
