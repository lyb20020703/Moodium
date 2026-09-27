using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Moodium.Calibration
{
    [DisallowMultipleComponent]
    public sealed class SensoryBagCalibrator : MonoBehaviour
    {
        [Header("Calibration")]
        [SerializeField, Min(0.01f)] float m_DefaultWidth = 0.25f;
        [SerializeField, Min(0.01f)] float m_DefaultHeight = 0.17f;
        [SerializeField, Min(0.005f)] float m_DefaultDepth = 0.05f;
        [SerializeField] Vector2 m_DefaultCenterOffset;
        [SerializeField, Min(0.01f)] float m_MinWidth = 0.08f;
        [SerializeField, Min(0.01f)] float m_MaxWidth = 0.6f;
        [SerializeField, Min(0.01f)] float m_MinHeight = 0.06f;
        [SerializeField, Min(0.01f)] float m_MaxHeight = 0.45f;
        [SerializeField] float m_PlaneLocalOffset;

        [Header("Calibration UI")]
        [SerializeField, Min(0.001f)] float m_BorderThickness = 0.004f;
        [SerializeField, Min(0.005f)] float m_HandleSize = 0.018f;
        [SerializeField, Min(0.005f)] float m_HandleDepth = 0.02f;
        [SerializeField, Min(0.01f)] float m_ButtonWidth = 0.16f;
        [SerializeField, Min(0.01f)] float m_ButtonHeight = 0.06f;
        [SerializeField] Color m_BorderColor = new(0.92f, 0.98f, 1f, 1f);
        [SerializeField] Color m_HandleColor = new(1f, 1f, 1f, 1f);
        [SerializeField] Color m_ConfirmColor = new(14f / 255f, 20f / 255f, 41f / 255f, 230f / 255f);
        [SerializeField] Color m_ResetColor = new(14f / 255f, 20f / 255f, 41f / 255f, 230f / 255f);

        [Header("Debug (read only at runtime)")]
        [SerializeField] string m_ReferenceImageName;
        [SerializeField] string m_TrackingState = "Inactive";
        [SerializeField] float m_CurrentWidth;
        [SerializeField] float m_CurrentHeight;
        [SerializeField] Vector2 m_CurrentCenterOffset;
        [SerializeField] Vector3 m_CurrentAnchorPosition;
        [SerializeField] Vector3 m_CurrentAnchorEulerAngles;

        readonly Dictionary<int, CalibrationHandleKind> m_Drags = new();
        readonly Dictionary<int, Vector2> m_DragStartPointer = new();
        readonly Dictionary<int, Vector2> m_DragStartCenter = new();

        Transform m_Anchor;
        Transform m_FrameRoot;
        GameObject m_FrameBody;
        readonly Dictionary<CalibrationHandleKind, Transform> m_Visuals = new();
        SensoryBagCalibrationData m_DefaultData;
        SensoryBagCalibrationSettings m_Settings;
        SensoryBagCalibrationRectangle m_Rectangle;
        bool m_FrameVisible;
        bool m_Confirmed;
        TMP_FontAsset m_ChineseFont;
        TMP_FontAsset m_EnglishFont;
        Sprite m_RoundedButtonSprite;
        static Material s_RuntimeUiMaterial;

        public event Action<SensoryBagCalibrationData> CalibrationConfirmed;
        public Transform Anchor => m_Anchor;
        public Transform FrameRoot => m_FrameRoot;
        public SensoryBagCalibrationData CurrentData => m_Rectangle != null ? m_Rectangle.Data : m_DefaultData;
        public bool FrameVisible => m_FrameVisible;
        public bool IsConfirmed => m_Confirmed;

        public void ConfigureFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            m_ChineseFont = chineseFont;
            m_EnglishFont = englishFont;
        }

        public void ConfigureButtonStyle(Sprite roundedButtonSprite)
        {
            m_RoundedButtonSprite = roundedButtonSprite;
        }

        public SensoryBagCalibrationSettings CreateInspectorSettings() => new(
            m_DefaultWidth, m_DefaultHeight, m_DefaultDepth, m_DefaultCenterOffset,
            m_MinWidth, m_MaxWidth, m_MinHeight, m_MaxHeight);

        public void Begin(
            Transform anchor,
            SensoryBagCalibrationData initialData,
            SensoryBagCalibrationSettings settings)
        {
            m_Anchor = anchor;
            m_Settings = settings;
            m_TrackingState = "Tracking";
            m_DefaultData = SensoryBagCalibrationData.CreateDefault(
                initialData.ReferenceImageName, initialData.WorldKind, settings);
            m_DefaultData.PlaneLocalOffset = initialData.PlaneLocalOffset;
            m_DefaultData.LocalOrientation = initialData.LocalOrientation;
            initialData.ClampDimensions(settings);
            m_Rectangle = new SensoryBagCalibrationRectangle(initialData, settings);
            m_Confirmed = false;
            EnsureVisuals();
            SetFrameVisible(true);
            RefreshVisuals();
            Debug.Log($"[Sensory Bag Calibration] Started {initialData.ReferenceImageName}: " +
                      $"{initialData.Width:F3}m × {initialData.Height:F3}m, offset={initialData.CenterOffset}.");
        }

        public void Begin(Transform anchor, SensoryBagCalibrationData initialData)
        {
            Begin(anchor, initialData, CreateInspectorSettings());
        }

        public void SetAnchor(Transform anchor)
        {
            m_Anchor = anchor;
            SyncFrameNow();
        }

        public void SetTrackingVisible(bool tracked)
        {
            m_TrackingState = tracked ? "Tracking" : "Temporarily lost";
            if (!m_Confirmed)
                SetFrameVisible(tracked);
        }

        public void MoveFrameToAnchorPlane(Vector2 centerOffset)
        {
            if (m_Rectangle == null)
                return;
            m_Rectangle.MoveTo(centerOffset);
            RefreshVisuals();
        }

        public void ResizeFromHandle(CalibrationHandleKind kind, Vector2 anchorLocalPoint)
        {
            if (m_Rectangle == null)
                return;
            m_Rectangle.ResizeFromHandle(kind, anchorLocalPoint);
            RefreshVisuals();
        }

        public void BeginPointerDrag(int pointerId, CalibrationHandleKind kind, Vector3 worldPosition)
        {
            if (!m_FrameVisible || m_Rectangle == null || m_Anchor == null)
                return;
            var localPoint = SensoryBagCalibrationRectangle.ProjectWorldPointToAnchorPlane(m_Anchor, worldPosition);
            m_Drags[pointerId] = kind;
            m_DragStartPointer[pointerId] = localPoint;
            m_DragStartCenter[pointerId] = m_Rectangle.Data.CenterOffset;
        }

        public void MovePointerDrag(int pointerId, Vector3 worldPosition)
        {
            if (!m_Drags.TryGetValue(pointerId, out var kind) || m_Anchor == null)
                return;
            var localPoint = SensoryBagCalibrationRectangle.ProjectWorldPointToAnchorPlane(m_Anchor, worldPosition);
            if (kind == CalibrationHandleKind.Frame)
            {
                var delta = localPoint - m_DragStartPointer[pointerId];
                MoveFrameToAnchorPlane(m_DragStartCenter[pointerId] + delta);
            }
            else
                ResizeFromHandle(kind, localPoint);
        }

        public void EndPointerDrag(int pointerId)
        {
            m_Drags.Remove(pointerId);
            m_DragStartPointer.Remove(pointerId);
            m_DragStartCenter.Remove(pointerId);
        }

        public void ResetCalibration()
        {
            if (m_Rectangle == null)
                return;
            m_Rectangle = new SensoryBagCalibrationRectangle(m_DefaultData, m_Settings);
            m_Confirmed = false;
            SetFrameVisible(true);
            RefreshVisuals();
            Debug.Log($"[Sensory Bag Calibration] Reset {m_DefaultData.ReferenceImageName}.");
        }

        public void ConfirmCalibration()
        {
            if (m_Rectangle == null || m_Confirmed)
                return;
            m_Confirmed = true;
            SetFrameVisible(false);
            var data = m_Rectangle.Data;
            Debug.Log($"[Sensory Bag Calibration] Confirmed {data.ReferenceImageName}: " +
                      $"{data.Width:F3}m × {data.Height:F3}m, offset={data.CenterOffset}, depth={data.Depth:F3}m.");
            CalibrationConfirmed?.Invoke(data);
        }

        public void SyncFrameNow()
        {
            if (m_Anchor == null || m_FrameRoot == null || m_Rectangle == null)
                return;
            var data = m_Rectangle.Data;
            var planeOffset = data.PlaneLocalOffset;
            if (Mathf.Approximately(planeOffset.y, 0f))
                planeOffset.y = m_PlaneLocalOffset;
            m_FrameRoot.SetPositionAndRotation(
                m_Anchor.TransformPoint(new Vector3(data.CenterOffset.x, planeOffset.y, data.CenterOffset.y)),
                m_Anchor.rotation * data.LocalOrientation);
            UpdateDebugData();
        }

        void LateUpdate()
        {
            SyncFrameNow();
        }

        void EnsureVisuals()
        {
            if (m_FrameRoot != null)
                return;
            var root = new GameObject("Calibration Frame");
            root.transform.SetParent(transform, false);
            m_FrameRoot = root.transform;

            m_FrameBody = CreateVisual("Frame Drag Surface", m_BorderColor, CalibrationHandleKind.Frame);
            // The draggable body must remain raycastable, but should not cover the
            // real bag. The visible rectangle is provided by the edge/corner handles.
            var frameRenderer = m_FrameBody.GetComponent<Renderer>();
            if (frameRenderer != null)
                frameRenderer.enabled = false;
            CreateVisual("Left Edge Handle", m_HandleColor, CalibrationHandleKind.Left);
            CreateVisual("Right Edge Handle", m_HandleColor, CalibrationHandleKind.Right);
            CreateVisual("Top Edge Handle", m_HandleColor, CalibrationHandleKind.Top);
            CreateVisual("Bottom Edge Handle", m_HandleColor, CalibrationHandleKind.Bottom);
            CreateVisual("Top Left Corner Handle", m_HandleColor, CalibrationHandleKind.TopLeft);
            CreateVisual("Top Right Corner Handle", m_HandleColor, CalibrationHandleKind.TopRight);
            CreateVisual("Bottom Left Corner Handle", m_HandleColor, CalibrationHandleKind.BottomLeft);
            CreateVisual("Bottom Right Corner Handle", m_HandleColor, CalibrationHandleKind.BottomRight);
            CreateAction("确认 / Confirm", m_ConfirmColor, CalibrationAction.Confirm);
            CreateAction("重置 / Reset", m_ResetColor, CalibrationAction.Reset);
        }

        GameObject CreateVisual(string name, Color color, CalibrationHandleKind kind)
        {
            var visual = kind is CalibrationHandleKind.TopLeft or CalibrationHandleKind.TopRight or
                CalibrationHandleKind.BottomLeft or CalibrationHandleKind.BottomRight
                ? GameObject.CreatePrimitive(PrimitiveType.Capsule)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(m_FrameRoot, false);
            visual.GetComponent<Renderer>().sharedMaterial = GetRuntimeUiMaterial();
            SetColor(visual.GetComponent<Renderer>(), color);
            var handle = visual.AddComponent<SensoryBagCalibrationHandle>();
            handle.Configure(this, kind);
            m_Visuals[kind] = visual.transform;
            return visual;
        }

        void CreateAction(string text, Color color, CalibrationAction action)
        {
            var button = new GameObject($"Calibration {text} Button", typeof(BoxCollider));
            button.name = $"Calibration {text} Button";
            button.transform.SetParent(m_FrameRoot, false);
            button.GetComponent<BoxCollider>().size = new Vector3(m_ButtonWidth, m_ButtonHeight, 0.04f);
            button.AddComponent<SensoryBagCalibrationHandle>().Configure(this, CalibrationHandleKind.Frame, action);

            var visual = CreateRoundedButtonVisual(button.transform, color);
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(visual.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            var english = PlayerPrefs.GetString("Moodium.Language", "Chinese").Equals("English", System.StringComparison.OrdinalIgnoreCase);
            label.text = action == CalibrationAction.Confirm
                ? (english ? "Confirm" : "确认")
                : (english ? "Reset" : "重置");
            label.font = english ? m_EnglishFont : m_ChineseFont;
            label.fontSize = 7f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(10f, 3f);
            label.color = Color.white;
            if (action == CalibrationAction.Confirm)
                button.transform.name = "Calibration Confirm Button";
            else
                button.transform.name = "Calibration Reset Button";
        }

        GameObject CreateRoundedButtonVisual(Transform parent, Color color)
        {
            // Use the same sliced rounded artwork as the opening-language choices.
            // The canvas is visual-only; the parent BoxCollider remains the stable
            // PolySpatial hit target.
            var canvasRoot = new GameObject("Button Visual", typeof(RectTransform));
            canvasRoot.transform.SetParent(parent, false);
            canvasRoot.transform.localPosition = new Vector3(0f, 0f, -0.012f);
            canvasRoot.transform.localScale = Vector3.one * 0.001f;
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var canvasRect = canvasRoot.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(m_ButtonWidth * 1000f, m_ButtonHeight * 1000f);

            var imageObject = new GameObject("Rounded Background", typeof(RectTransform));
            imageObject.transform.SetParent(canvasRoot.transform, false);
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            var image = imageObject.AddComponent<Image>();
            image.sprite = m_RoundedButtonSprite;
            image.type = m_RoundedButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            var outline = imageObject.AddComponent<Outline>();
            outline.effectColor = new Color(.42f, .58f, .95f, .65f);
            outline.effectDistance = new Vector2(1f, -1f);
            return canvasRoot;
        }

        void RefreshVisuals()
        {
            if (m_Rectangle == null || m_FrameRoot == null)
                return;
            var halfWidth = m_Rectangle.Data.Width * 0.5f;
            var halfHeight = m_Rectangle.Data.Height * 0.5f;
            var border = m_BorderThickness;
            var handle = m_HandleSize;

            SetVisual(CalibrationHandleKind.Left, new Vector3(-halfWidth, 0f, 0f), new Vector3(border, m_Rectangle.Data.Height, m_HandleDepth));
            SetVisual(CalibrationHandleKind.Right, new Vector3(halfWidth, 0f, 0f), new Vector3(border, m_Rectangle.Data.Height, m_HandleDepth));
            SetVisual(CalibrationHandleKind.Top, new Vector3(0f, halfHeight, 0f), new Vector3(m_Rectangle.Data.Width, border, m_HandleDepth));
            SetVisual(CalibrationHandleKind.Bottom, new Vector3(0f, -halfHeight, 0f), new Vector3(m_Rectangle.Data.Width, border, m_HandleDepth));
            SetVisual(CalibrationHandleKind.TopLeft, new Vector3(-halfWidth, halfHeight, -0.004f), Vector3.one * handle);
            SetVisual(CalibrationHandleKind.TopRight, new Vector3(halfWidth, halfHeight, -0.004f), Vector3.one * handle);
            SetVisual(CalibrationHandleKind.BottomLeft, new Vector3(-halfWidth, -halfHeight, -0.004f), Vector3.one * handle);
            SetVisual(CalibrationHandleKind.BottomRight, new Vector3(halfWidth, -halfHeight, -0.004f), Vector3.one * handle);
            if (m_FrameBody != null)
            {
                m_FrameBody.transform.localPosition = new Vector3(0f, 0f, 0.006f);
                m_FrameBody.transform.localScale = new Vector3(
                    Mathf.Max(0.001f, m_Rectangle.Data.Width - border),
                    Mathf.Max(0.001f, m_Rectangle.Data.Height - border),
                    0.004f);
            }

            var confirm = m_FrameRoot.Find("Calibration Confirm Button");
            var reset = m_FrameRoot.Find("Calibration Reset Button");
            if (confirm != null)
            {
                confirm.localPosition = new Vector3(-m_ButtonWidth * 0.6f, -halfHeight - 0.10f, 0f);
                confirm.localScale = Vector3.one;
            }
            if (reset != null)
            {
                reset.localPosition = new Vector3(m_ButtonWidth * 0.6f, -halfHeight - 0.10f, 0f);
                reset.localScale = Vector3.one;
            }
            SyncFrameNow();
        }

        void SetVisual(CalibrationHandleKind kind, Vector3 localPosition, Vector3 localScale)
        {
            if (!m_Visuals.TryGetValue(kind, out var visual) || visual == null)
                return;
            visual.localPosition = localPosition;
            visual.localScale = localScale;

            // Keep the spatial-pointer target comfortably larger than the visible,
            // UI-like line. This preserves Vision Pro pinch usability without making
            // the frame look like thick 3D tubes.
            var collider = visual.GetComponent<BoxCollider>();
            if (collider != null && kind != CalibrationHandleKind.Frame)
            {
                var hitSize = kind is CalibrationHandleKind.TopLeft or CalibrationHandleKind.TopRight or
                    CalibrationHandleKind.BottomLeft or CalibrationHandleKind.BottomRight
                    ? Vector3.one * 0.04f
                    : new Vector3(
                        kind is CalibrationHandleKind.Left or CalibrationHandleKind.Right ? 0.04f : localScale.x,
                        kind is CalibrationHandleKind.Top or CalibrationHandleKind.Bottom ? 0.04f : localScale.y,
                        0.04f);
                collider.size = new Vector3(
                    hitSize.x / Mathf.Max(0.001f, localScale.x),
                    hitSize.y / Mathf.Max(0.001f, localScale.y),
                    hitSize.z / Mathf.Max(0.001f, localScale.z));
            }
        }

        void SetFrameVisible(bool visible)
        {
            m_FrameVisible = visible;
            if (m_FrameRoot != null)
                m_FrameRoot.gameObject.SetActive(visible);
        }

        void UpdateDebugData()
        {
            if (m_Rectangle != null)
            {
                var data = m_Rectangle.Data;
                m_ReferenceImageName = data.ReferenceImageName;
                m_CurrentWidth = data.Width;
                m_CurrentHeight = data.Height;
                m_CurrentCenterOffset = data.CenterOffset;
            }
            if (m_Anchor != null)
            {
                m_CurrentAnchorPosition = m_Anchor.position;
                m_CurrentAnchorEulerAngles = m_Anchor.eulerAngles;
            }
        }

        static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        static Material GetRuntimeUiMaterial()
        {
            if (s_RuntimeUiMaterial != null)
                return s_RuntimeUiMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            s_RuntimeUiMaterial = new Material(shader) { name = "Moodium Runtime Spatial UI" };
            return s_RuntimeUiMaterial;
        }

        void OnDestroy()
        {
            if (m_FrameRoot != null)
                Destroy(m_FrameRoot.gameObject);
        }
    }
}
