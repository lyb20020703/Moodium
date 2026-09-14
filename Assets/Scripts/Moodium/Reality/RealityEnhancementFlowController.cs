using System.Collections;
using Moodium.Flow;
using Moodium.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Moodium.Reality
{
    public enum RealityEnhancementFlowState
    {
        Idle,
        ObjectDetected,
        Analyzing,
        Recommend,
        Generating,
        Interactive
    }

    /// <summary>Shows the initial guidance, then hands the tracked object directly to the enhancement.</summary>
    public sealed class RealityEnhancementFlowController : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(2f)] float m_GuideDuration = 4f;
        [SerializeField, Min(0.2f)] float m_GenerationDuration = 0.5f;

        TissueObjectTrackingSpawner m_Spawner;
        Camera m_Camera;
        GameObject m_GlassPanelPrefab;
        TMP_FontAsset m_Font;
        TMP_FontAsset m_ChineseFont;
        RealityEnhancementFlowState m_State;
        ARTrackedObject m_TrackedObject;
        GameObject m_Guide;
        Coroutine m_GuideRoutine;
        Coroutine m_DetectionRoutine;
        PalmPressGestureController m_PalmPressGesture;
        bool m_Running;

        public RealityEnhancementFlowState State => m_State;

        public void Configure(
            TissueObjectTrackingSpawner spawner,
            Camera camera,
            GameObject glassPanelPrefab,
            TMP_FontAsset font,
            TMP_FontAsset chineseFont)
        {
            m_Spawner = spawner;
            m_Camera = camera;
            m_GlassPanelPrefab = glassPanelPrefab;
            m_Font = font;
            m_ChineseFont = chineseFont;
            var legacyThumbBend = GetComponent<BimanualThumbBendGestureController>();
            if (legacyThumbBend != null)
            {
                legacyThumbBend.SetGestureEnabled(false);
                legacyThumbBend.SetTarget(null);
                legacyThumbBend.enabled = false;
            }
            if (m_PalmPressGesture == null)
                m_PalmPressGesture = GetComponent<PalmPressGestureController>();
            if (m_PalmPressGesture == null)
                m_PalmPressGesture = gameObject.AddComponent<PalmPressGestureController>();
            m_PalmPressGesture.SetInteractionEnabled(false);
        }

        public void StartFlow()
        {
            StopFlow(false);
            if (m_Spawner == null)
            {
                Debug.LogError("[Reality Flow] TissueObjectTrackingSpawner was not found.");
                return;
            }
            m_Running = true;
            m_State = RealityEnhancementFlowState.Idle;
            m_Spawner.SetDeferredSpawning(true);
            m_Spawner.TissueDetected += OnTissueDetected;
            m_GuideRoutine = StartCoroutine(ShowInitialGuide());
            Debug.Log("[Reality Flow] Waiting for an everyday object.");
        }

        public void StopFlow() => StopFlow(true);

        void StopFlow(bool clearTrackedVisuals)
        {
            if (m_Spawner != null)
            {
                m_Spawner.TissueDetected -= OnTissueDetected;
                m_Spawner.SetDeferredSpawning(false);
                if (clearTrackedVisuals)
                    m_Spawner.ClearRuntimeInstances();
            }
            m_Running = false;
            m_State = RealityEnhancementFlowState.Idle;
            if (m_PalmPressGesture != null)
            {
                m_PalmPressGesture.SetInteractionEnabled(false);
                m_PalmPressGesture.SetTarget(null);
            }
            if (m_GuideRoutine != null) StopCoroutine(m_GuideRoutine);
            if (m_DetectionRoutine != null) StopCoroutine(m_DetectionRoutine);
            m_GuideRoutine = null;
            m_DetectionRoutine = null;
            DestroyPresentationObjects();
        }

        void OnDestroy()
        {
            StopFlow(false);
        }

        void Update()
        {
            if (m_Guide != null && m_Camera != null)
            {
                var forward = HorizontalForward(m_Camera.transform);
                m_Guide.transform.SetPositionAndRotation(
                    m_Camera.transform.position + forward * 0.92f + Vector3.up * 0.035f,
                    Quaternion.LookRotation(forward, Vector3.up));
            }

        }

        IEnumerator ShowInitialGuide()
        {
            m_Guide = CreateGlassCard(
                "Reality Initial Guidance",
                "拿起身边的日常物品\n将它放在面前，开始感官增强体验\n\n" +
                "Pick up an everyday object\nPlace it in front of you to begin",
                new Vector2(0.56f, 0.28f),
                TextAlignmentOptions.Center);
            yield return AnimateScaleIn(m_Guide, 0.35f);
            yield return new WaitForSeconds(Mathf.Max(0f, m_GuideDuration - 0.8f));
            yield return AnimateScaleOut(m_Guide, 0.4f);
            DestroySafe(ref m_Guide);
            m_GuideRoutine = null;
        }

        void OnTissueDetected(ARTrackedObject trackedObject)
        {
            if (!m_Running || trackedObject == null || m_State != RealityEnhancementFlowState.Idle)
                return;
            if (m_GuideRoutine != null) StopCoroutine(m_GuideRoutine);
            m_GuideRoutine = null;
            DestroySafe(ref m_Guide);
            m_TrackedObject = trackedObject;
            m_DetectionRoutine = StartCoroutine(RunDetectionFlow());
        }

        IEnumerator RunDetectionFlow()
        {
            m_State = RealityEnhancementFlowState.ObjectDetected;
            if (!CanContinue())
            {
                CancelDetection("Tracked tissue was removed before generation.");
                yield break;
            }

            m_State = RealityEnhancementFlowState.Generating;
            var capsule = m_Spawner.SpawnDeferred(m_TrackedObject);
            if (capsule == null)
            {
                CancelDetection("Chocolate Capsule could not be generated.");
                yield break;
            }
            yield return AnimateCapsuleIn(capsule, m_GenerationDuration);

            var interaction = capsule.GetComponent<ChocolateCapsuleInteraction>();
            if (interaction == null)
            {
                CancelDetection("Chocolate Capsule interaction was not found.");
                yield break;
            }
            m_PalmPressGesture.SetTarget(interaction);
            m_PalmPressGesture.SetInteractionEnabled(true);

            m_State = RealityEnhancementFlowState.Interactive;
            Debug.Log("[Reality Flow] Interactive Candy Capsule experience ready.");
            m_DetectionRoutine = null;
        }

        bool CanContinue()
        {
            return m_Running && m_TrackedObject != null &&
                   m_TrackedObject.trackingState != TrackingState.None;
        }

        void CancelDetection(string reason)
        {
            Debug.LogWarning($"[Reality Flow] {reason}");
            m_Spawner?.ReleaseDeferredDetection(m_TrackedObject);
            m_TrackedObject = null;
            m_State = RealityEnhancementFlowState.Idle;
            m_DetectionRoutine = null;
        }

        GameObject CreateGlassCard(string name, string text, Vector2 size, TextAlignmentOptions alignment)
        {
            var root = new GameObject(name);
            root.transform.localScale = Vector3.zero;
            if (m_GlassPanelPrefab != null)
            {
                var glass = Instantiate(m_GlassPanelPrefab, root.transform, false);
                glass.name = "Soft VisionOS Glass";
                glass.transform.localScale = new Vector3(size.x / 0.72f, size.y / 0.43f, 1f);
                foreach (var oldText in glass.GetComponentsInChildren<TMP_Text>(true))
                    oldText.gameObject.SetActive(false);
            }

            var textObject = new GameObject("AI Perception Text");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.035f);
            textObject.transform.localScale = Vector3.one * 0.1f;
            var tmp = textObject.AddComponent<TextMeshPro>();
            tmp.font = m_ChineseFont != null ? m_ChineseFont : m_Font;
            tmp.text = text;
            tmp.fontSize = 2.25f;
            tmp.color = new Color(0.96f, 0.97f, 1f, 0.98f);
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(size.x * 8.5f, size.y * 8.1f);
            tmp.renderer.sortingOrder = 80;
            return root;
        }

        static IEnumerator AnimateScaleIn(GameObject target, float duration)
        {
            if (target == null) yield break;
            var elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                target.transform.localScale = Vector3.one * eased;
                yield return null;
            }
            if (target != null) target.transform.localScale = Vector3.one;
        }

        static IEnumerator AnimateScaleOut(GameObject target, float duration)
        {
            if (target == null) yield break;
            var start = target.transform.localScale;
            var elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.transform.localScale = Vector3.Lerp(start, Vector3.zero, t * t);
                yield return null;
            }
        }

        static IEnumerator AnimateCapsuleIn(GameObject capsule, float duration)
        {
            if (capsule == null) yield break;
            var targetScale = capsule.transform.localScale;
            capsule.transform.localScale = targetScale * 0.04f;
            var elapsed = 0f;
            while (elapsed < duration && capsule != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var back = 1f + 0.1f * Mathf.Sin(t * Mathf.PI);
                capsule.transform.localScale = Vector3.LerpUnclamped(targetScale * 0.04f, targetScale, t) * back;
                yield return null;
            }
            if (capsule != null) capsule.transform.localScale = targetScale;
        }

        void DestroyPresentationObjects()
        {
            DestroySafe(ref m_Guide);
            m_TrackedObject = null;
        }

        static void DestroySafe(ref GameObject value)
        {
            if (value != null) Destroy(value);
            value = null;
        }

        static Vector3 HorizontalForward(Transform cameraTransform)
        {
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            return forward.sqrMagnitude > 0.001f ? forward : cameraTransform.forward.normalized;
        }
    }
}
