using System.Collections.Generic;
using Moodium.Audio;
using Moodium.Flow;
using Moodium.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace Moodium.CandyWorld
{
    /// <summary>Wrist-tracked energy UI whose progress is a physical tube-shaped ring.</summary>
    public sealed class CandyEnergyWristUI : MonoBehaviour
    {
        const float FullCircleDegrees = 360f;
        static readonly Color BackgroundRingColor = new(1f, 1f, 1f, .48f);
        static readonly Color CandyRingColor = new(1f, 91f / 255f, 157f / 255f, 1f);
        static readonly Color NatureRingColor = new(141f / 255f, 220f / 255f, 135f / 255f, 1f);

        [Header("Wrist Ring Layout")]
        [SerializeField] Vector3 m_WristPositionOffset = new(0f, .018f, 0f);
        [SerializeField] Vector3 m_RingRotationOffset = Vector3.zero;
        [SerializeField, Min(.02f)] float m_RingRadius = .055f;
        [SerializeField, Min(.002f)] float m_RingThickness = .010f;
        [SerializeField, Range(.05f, 1f)] float m_RingOpacity = .9f;
        [SerializeField, Range(20, 96)] int m_RingSegments = 56;
        [SerializeField, Range(4, 16)] int m_TubeSides = 8;
        [SerializeField, Min(1f)] float m_FollowResponsiveness = 14f;

        [Header("Wrist Overlay Layout")]
        [SerializeField, Min(0f)] float m_OverlayClearance = .025f;
        [SerializeField] float m_TitleVerticalOffset = .012f;
        [SerializeField] float m_PercentageVerticalOffset = 0f;
        [SerializeField] float m_ReleaseVerticalOffset = -.017f;
        [SerializeField] float m_ExitVerticalOffset = -.042f;

        Camera m_Camera;
        XRHandSubsystem m_HandSubsystem;
        XROrigin m_XROrigin;
        CanvasGroup m_CanvasGroup;
        RectTransform m_CanvasRect;
        TMP_Text m_Percentage;
        TMP_Text m_Title;
        TMP_Text m_ReleaseLabel;
        TMP_Text m_ExitLabel;
        GameObject m_ReleaseButtonRoot;
        GameObject m_ReleaseKnobRoot;
        GameObject m_ExitButtonRoot;
        Sprite m_RoundedSprite;
        Sprite m_CircleSprite;
        Transform m_RingRoot;
        Mesh m_BackgroundRingMesh;
        Mesh m_ProgressRingMesh;
        Material m_BackgroundMaterial;
        Material m_ProgressMaterial;
        float m_TargetProgress;
        float m_DisplayedProgress;
        float m_TargetEnergy;
        float m_DisplayedEnergy;
        float m_LastBuiltArcDegrees = -1f;
        float m_VisualAlpha;
        bool m_Completed;
        bool m_ReleaseAvailable;
        bool m_ReleaseEnabled;
        bool m_ExitAvailable;
        bool m_Built;
        bool m_NatureWorld;
        string m_WorldEnergyTitle = "Candy Energy";

        public float DisplayedProgress => m_DisplayedProgress;
        public bool HandTracked { get; private set; }
        public event System.Action ReleasePressed;
        public event System.Action ExitPressed;

        public static float ProgressToArcDegrees(float normalizedProgress) => Mathf.Clamp01(normalizedProgress) * FullCircleDegrees;
        public static Color GetProgressRingColor(bool natureWorld) => natureWorld ? NatureRingColor : CandyRingColor;
        public static float CalculateOverlayVerticalAnchor(float ringRadius, float ringThickness, float clearance, float canvasScale)
        {
            return (Mathf.Max(0f, ringRadius) + Mathf.Max(0f, ringThickness) * .5f + Mathf.Max(0f, clearance)) /
                   Mathf.Max(.000001f, canvasScale);
        }

        public static string GetExitButtonLabel(bool english)
        {
            return english ? "Exit World" : "退出世界";
        }

        public void ConfigureLanguageFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            var english = PlayerPrefs.GetString("Moodium.Language", "Chinese")
                .Equals("English", System.StringComparison.OrdinalIgnoreCase);
            var selected = english ? englishFont : chineseFont;
            if (selected == null) return;
            if (m_ExitLabel != null) m_ExitLabel.font = selected;
            if (m_ReleaseLabel != null) m_ReleaseLabel.font = selected;
        }

        public void Build(Camera camera, TMP_FontAsset font, Sprite roundedSprite, Sprite circleSprite)
        {
            m_Camera = camera != null ? camera : Camera.main;
            m_RoundedSprite = roundedSprite;
            m_CircleSprite = circleSprite;
            BuildRingVisual();
            BuildTextAndReleaseOverlay(font);
            m_Built = true;
            m_CanvasGroup.alpha = 0f;
            SetProgress(0f, 0f);
            ApplyRingColors();
        }

        public void SetReleaseAvailable(bool available)
        {
            m_ReleaseAvailable = available;
            if (!available) m_ReleaseEnabled = false;
            if (m_ReleaseButtonRoot != null) m_ReleaseButtonRoot.SetActive(available);
            UpdateReleaseToggleVisual();
        }

        public void SetReleaseToggleState(bool enabled)
        {
            m_ReleaseEnabled = enabled;
            UpdateReleaseToggleVisual();
        }

        public void SetExitAvailable(bool available)
        {
            m_ExitAvailable = available;
            if (m_ExitButtonRoot != null)
                m_ExitButtonRoot.SetActive(available);
            UpdateExitButtonLabel();
        }

        public void SetWorldTheme(bool natureWorld)
        {
            m_NatureWorld = natureWorld;
            m_WorldEnergyTitle = natureWorld ? "Nature Energy" : "Candy Energy";
            if (m_Title != null) m_Title.text = m_WorldEnergyTitle;
            ApplyRingColors();
        }

        public void SetProgress(float energy, float normalized)
        {
            m_TargetProgress = Mathf.Clamp01(normalized);
            m_TargetEnergy = Mathf.Max(0f, energy);
            if (!m_Completed && m_TargetProgress >= .999f)
            {
                m_Completed = true;
                MoodiumAudioManager.Play(MoodiumAudioCue.CandyEnergyComplete, transform.position);
                MoodiumInteractionVFXManager.PlayInteractionEffect(transform.position, transform.rotation, gameObject);
            }
            else if (m_TargetProgress < .999f) m_Completed = false;
        }

        void BuildRingVisual()
        {
            var root = new GameObject("Wrist 3D Ring Progress");
            root.transform.SetParent(transform, false);
            m_RingRoot = root.transform;
            m_BackgroundRingMesh = new Mesh { name = "Wrist Ring Background Mesh" };
            m_ProgressRingMesh = new Mesh { name = "Wrist Ring Progress Mesh" };
            CreateRingRenderer("Background Ring", m_BackgroundRingMesh, out m_BackgroundMaterial);
            CreateRingRenderer("Progress Ring", m_ProgressRingMesh, out m_ProgressMaterial);
            BuildTubeArc(m_BackgroundRingMesh, FullCircleDegrees);
            BuildTubeArc(m_ProgressRingMesh, 0f);
        }

        void CreateRingRenderer(string name, Mesh mesh, out Material material)
        {
            var ring = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(m_RingRoot, false);
            ring.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = ring.GetComponent<MeshRenderer>();
            material = CreateTransparentRingMaterial(name);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Material CreateTransparentRingMaterial(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        void BuildTextAndReleaseOverlay(TMP_FontAsset font)
        {
            var canvasObject = new GameObject("Wrist Ring Text and Release Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            m_CanvasRect = (RectTransform)canvasObject.transform;
            m_CanvasRect.sizeDelta = new Vector2(420f, 800f);
            m_CanvasRect.localScale = Vector3.one * .00023f;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            m_CanvasGroup = canvasObject.GetComponent<CanvasGroup>();
            var canvasScale = m_CanvasRect.localScale.y;
            var anchor = CalculateOverlayVerticalAnchor(m_RingRadius, m_RingThickness, m_OverlayClearance, canvasScale);
            m_Title = CreateText(m_WorldEnergyTitle, m_CanvasRect,
                new Vector2(0f, anchor + m_TitleVerticalOffset / canvasScale), new Vector2(220f, 28f), 16f, font, Color.white);
            m_Percentage = CreateText("0%", m_CanvasRect,
                new Vector2(0f, anchor + m_PercentageVerticalOffset / canvasScale), new Vector2(135f, 45f), 27f, font, Color.white);
            CreateReleaseButton(font, anchor + m_ReleaseVerticalOffset / canvasScale);
            CreateExitButton(font, anchor + m_ExitVerticalOffset / canvasScale);
        }

        void CreateReleaseButton(TMP_FontAsset font, float verticalPosition)
        {
            m_ReleaseButtonRoot = new GameObject("Energy Release Button", typeof(RectTransform), typeof(Image), typeof(BoxCollider));
            m_ReleaseButtonRoot.transform.SetParent(m_CanvasRect, false);
            var rect = (RectTransform)m_ReleaseButtonRoot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0f, verticalPosition);
            rect.sizeDelta = new Vector2(136f, 48f);
            var image = m_ReleaseButtonRoot.GetComponent<Image>();
            image.sprite = m_RoundedSprite;
            image.type = m_RoundedSprite != null && m_RoundedSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(.96f, .96f, 1f, .96f);
            image.raycastTarget = false;
            m_ReleaseButtonRoot.GetComponent<BoxCollider>().size = new Vector3(136f, 48f, 20f);
            m_ReleaseLabel = CreateText("Release", m_ReleaseButtonRoot.transform, Vector2.zero, new Vector2(126f, 40f), 18f,
                font, new Color(.08f, .08f, .12f, 1f));
            m_ReleaseKnobRoot = new GameObject("Release Toggle Knob", typeof(RectTransform), typeof(Image));
            m_ReleaseKnobRoot.transform.SetParent(m_ReleaseButtonRoot.transform, false);
            var knobRect = (RectTransform)m_ReleaseKnobRoot.transform;
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(1f, .5f);
            knobRect.sizeDelta = new Vector2(32f, 32f);
            var knobImage = m_ReleaseKnobRoot.GetComponent<Image>();
            knobImage.sprite = m_CircleSprite;
            knobImage.color = Color.white;
            knobImage.raycastTarget = false;
            var button = m_ReleaseButtonRoot.AddComponent<MoodiumSpatialButton>();
            button.Configure(m_ReleaseLabel, null, PressRelease);
            m_ReleaseButtonRoot.SetActive(false);
        }

        void CreateExitButton(TMP_FontAsset font, float verticalPosition)
        {
            m_ExitButtonRoot = new GameObject("Exit World Button", typeof(RectTransform), typeof(Image), typeof(BoxCollider));
            m_ExitButtonRoot.transform.SetParent(m_CanvasRect, false);
            var rect = (RectTransform)m_ExitButtonRoot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0f, verticalPosition);
            rect.sizeDelta = new Vector2(136f, 42f);
            var image = m_ExitButtonRoot.GetComponent<Image>();
            image.sprite = m_RoundedSprite;
            image.type = m_RoundedSprite != null && m_RoundedSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(.34f, .12f, .20f, .94f);
            image.raycastTarget = false;
            m_ExitButtonRoot.GetComponent<BoxCollider>().size = new Vector3(136f, 42f, 20f);
            m_ExitLabel = CreateText("退出世界", m_ExitButtonRoot.transform, Vector2.zero, new Vector2(126f, 36f), 16f,
                font, Color.white);
            var button = m_ExitButtonRoot.AddComponent<MoodiumSpatialButton>();
            button.Configure(m_ExitLabel, null, PressExit);
            m_ExitButtonRoot.SetActive(false);
        }

        void UpdateReleaseToggleVisual()
        {
            if (m_ReleaseLabel != null)
                m_ReleaseLabel.text = m_ReleaseEnabled ? "Release On" : "Release Off";
            var image = m_ReleaseButtonRoot != null ? m_ReleaseButtonRoot.GetComponent<Image>() : null;
            if (image != null)
                image.color = m_ReleaseEnabled ? new Color(.55f, .86f, .55f, .96f) : new Color(.96f, .96f, 1f, .96f);
            if (m_ReleaseKnobRoot != null)
            {
                var knobRect = (RectTransform)m_ReleaseKnobRoot.transform;
                knobRect.anchoredPosition = new Vector2(m_ReleaseEnabled ? -20f : -116f, 0f);
            }
        }

        void PressRelease()
        {
            if (!m_ReleaseAvailable) return;
            ReleasePressed?.Invoke();
        }

        void PressExit()
        {
            if (!m_ExitAvailable) return;
            ExitPressed?.Invoke();
        }

        void UpdateExitButtonLabel()
        {
            if (m_ExitLabel == null) return;
            var english = PlayerPrefs.GetString("Moodium.Language", "Chinese")
                .Equals("English", System.StringComparison.OrdinalIgnoreCase);
            m_ExitLabel.text = GetExitButtonLabel(english);
        }

        void LateUpdate()
        {
            if (!m_Built) return;
            UpdateWristPose();
            UpdateProgressAnimation();
            UpdateOverlayFacing();
            ApplyRingColors();
        }

        void UpdateWristPose()
        {
            var tracked = TryGetWristFrame(out var wristPose, out var armAxis);
#if UNITY_EDITOR
            if (!tracked && m_Camera != null)
            {
                var forward = Vector3.ProjectOnPlane(m_Camera.transform.forward, Vector3.up).normalized;
                wristPose = new Pose(m_Camera.transform.position + forward * .65f - Vector3.up * .20f, Quaternion.identity);
                armAxis = Vector3.forward;
                tracked = true;
            }
#endif
            HandTracked = tracked;
            m_VisualAlpha = Mathf.MoveTowards(m_VisualAlpha, tracked ? 1f : 0f, Time.deltaTime * 8f);
            m_CanvasGroup.alpha = m_VisualAlpha;
            if (!tracked) return;
            var ringRotation = Quaternion.FromToRotation(Vector3.up, armAxis) * Quaternion.Euler(m_RingRotationOffset);
            var targetPosition = wristPose.position + ringRotation * m_WristPositionOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-m_FollowResponsiveness * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, ringRotation,
                1f - Mathf.Exp(-m_FollowResponsiveness * Time.deltaTime));
        }

        bool TryGetWristFrame(out Pose wristPose, out Vector3 armAxis)
        {
            wristPose = Pose.identity;
            armAxis = Vector3.up;
            if (m_HandSubsystem == null)
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (m_HandSubsystem == null || !m_HandSubsystem.running) return false;
            var flags = m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
            if (!TryGetWristFrame(m_HandSubsystem.leftHand, flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints, out wristPose, out armAxis) &&
                !TryGetWristFrame(m_HandSubsystem.rightHand, flags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints, out wristPose, out armAxis)) return false;
            if (m_XROrigin == null) m_XROrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var origin = m_XROrigin != null ? m_XROrigin.Origin.transform : null;
            var trackingWrist = wristPose;
            var trackingPalmPoint = new Pose(wristPose.position + armAxis, Quaternion.identity);
            wristPose = PalmPressGestureController.TransformTrackingPose(trackingWrist, origin);
            var worldPalm = PalmPressGestureController.TransformTrackingPose(trackingPalmPoint, origin).position;
            armAxis = (worldPalm - wristPose.position).normalized;
            return armAxis.sqrMagnitude > .0001f;
        }

        static bool TryGetWristFrame(XRHand hand, XRHandSubsystem.UpdateSuccessFlags updated, out Pose wristPose, out Vector3 armAxis)
        {
            wristPose = Pose.identity;
            armAxis = Vector3.up;
            if (updated == 0 && !hand.isTracked) return false;
            var wrist = hand.GetJoint(XRHandJointID.Wrist);
            var middle = hand.GetJoint(XRHandJointID.MiddleMetacarpal);
            if (wrist.trackingState == XRHandJointTrackingState.None || !wrist.TryGetPose(out wristPose) || !middle.TryGetPose(out var middlePose)) return false;
            armAxis = middlePose.position - wristPose.position;
            return armAxis.sqrMagnitude > .000001f;
        }

        void UpdateProgressAnimation()
        {
            m_DisplayedProgress = Mathf.Lerp(m_DisplayedProgress, m_TargetProgress, 1f - Mathf.Exp(-5.5f * Time.deltaTime));
            m_DisplayedEnergy = Mathf.MoveTowards(m_DisplayedEnergy, m_TargetEnergy, Time.deltaTime * 24f);
            m_Percentage.text = $"{Mathf.RoundToInt(m_DisplayedEnergy)}%";
            var arcDegrees = ProgressToArcDegrees(m_DisplayedProgress);
            if (Mathf.Abs(arcDegrees - m_LastBuiltArcDegrees) > .35f)
            {
                BuildTubeArc(m_ProgressRingMesh, arcDegrees);
                m_LastBuiltArcDegrees = arcDegrees;
            }
        }

        void UpdateOverlayFacing()
        {
            if (m_Camera == null || m_CanvasRect == null) return;
            var toCamera = m_Camera.transform.position - m_CanvasRect.position;
            if (toCamera.sqrMagnitude > .0001f) m_CanvasRect.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        void ApplyRingColors()
        {
            if (m_BackgroundMaterial == null || m_ProgressMaterial == null) return;
            var background = BackgroundRingColor;
            background.a *= m_RingOpacity * m_VisualAlpha;
            var progress = GetProgressRingColor(m_NatureWorld);
            progress.a *= m_RingOpacity * m_VisualAlpha;
            SetMaterialColor(m_BackgroundMaterial, background);
            SetMaterialColor(m_ProgressMaterial, progress);
        }

        static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        void BuildTubeArc(Mesh mesh, float arcDegrees)
        {
            mesh.Clear();
            if (arcDegrees <= .01f) return;
            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(m_RingSegments * arcDegrees / FullCircleDegrees));
            var vertices = new List<Vector3>((segmentCount + 1) * m_TubeSides);
            var normals = new List<Vector3>((segmentCount + 1) * m_TubeSides);
            var triangles = new List<int>(segmentCount * m_TubeSides * 6);
            var halfThickness = m_RingThickness * .5f;
            for (var segment = 0; segment <= segmentCount; segment++)
            {
                var angle = Mathf.Deg2Rad * arcDegrees * segment / segmentCount;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var center = outward * m_RingRadius;
                for (var side = 0; side < m_TubeSides; side++)
                {
                    var crossAngle = Mathf.PI * 2f * side / m_TubeSides;
                    var normal = (outward * Mathf.Cos(crossAngle) + Vector3.up * Mathf.Sin(crossAngle)).normalized;
                    vertices.Add(center + normal * halfThickness);
                    normals.Add(normal);
                }
            }
            for (var segment = 0; segment < segmentCount; segment++)
            for (var side = 0; side < m_TubeSides; side++)
            {
                var nextSide = (side + 1) % m_TubeSides;
                var a = segment * m_TubeSides + side;
                var b = segment * m_TubeSides + nextSide;
                var c = (segment + 1) * m_TubeSides + side;
                var d = (segment + 1) * m_TubeSides + nextSide;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
        }

        static TMP_Text CreateText(string value, Transform parent, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font, Color color)
        {
            var item = new GameObject($"Text - {value}", typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = item.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        void OnDestroy()
        {
            if (m_BackgroundRingMesh != null) Destroy(m_BackgroundRingMesh);
            if (m_ProgressRingMesh != null) Destroy(m_ProgressRingMesh);
            if (m_BackgroundMaterial != null) Destroy(m_BackgroundMaterial);
            if (m_ProgressMaterial != null) Destroy(m_ProgressMaterial);
        }
    }
}
