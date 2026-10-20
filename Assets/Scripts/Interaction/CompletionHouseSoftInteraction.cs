using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace Moodium.Interaction
{
    /// <summary>
    /// Adds a gentle, bottom-anchored soft response to a completed-world house.
    /// It deliberately does not use ChocolateCapsuleInteraction, so it cannot alter
    /// the energy, squeeze, burst, or release flows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CompletionHouseSoftInteraction : MonoBehaviour
    {
        static readonly XRHandJointID[] ProbeJoints =
        {
            XRHandJointID.ThumbTip, XRHandJointID.IndexTip, XRHandJointID.MiddleTip,
            XRHandJointID.RingTip, XRHandJointID.LittleTip,
            XRHandJointID.IndexMetacarpal, XRHandJointID.MiddleMetacarpal,
            XRHandJointID.RingMetacarpal, XRHandJointID.LittleMetacarpal
        };

        [Header("Soft building response")]
        [SerializeField, Range(0f, .2f)] float m_MaxCompression = .08f;
        [SerializeField, Range(0f, .12f)] float m_HorizontalExpansion = .035f;
        [SerializeField, Min(1f)] float m_ResponseSpeed = 9f;
        [SerializeField, Min(.005f)] float m_ContactRadius = .035f;
        [SerializeField, Min(0f)] float m_ContactReleaseGrace = .12f;

        XRHandSubsystem m_HandSubsystem;
        XROrigin m_XROrigin;
        BoxCollider m_ContactCollider;
        Vector3 m_BaseScale;
        Vector3 m_BasePosition;
        float m_VisualHeight;
        float m_Response;
        float m_LastContactTime = float.NegativeInfinity;
        bool m_Configured;

        public static Vector3 CalculatePressedScale(Vector3 baseScale, float response, float maximumCompression,
            float horizontalExpansion)
        {
            response = Mathf.Clamp01(response);
            return Vector3.Scale(baseScale, new Vector3(
                1f + horizontalExpansion * response,
                1f - maximumCompression * response,
                1f + horizontalExpansion * response));
        }

        public static float CalculateBottomLockOffset(float visualHeight, float verticalScaleMultiplier) =>
            Mathf.Max(0f, visualHeight) * Mathf.Max(0f, 1f - verticalScaleMultiplier);

        void Awake()
        {
            EnsureContactCollider();
        }

        void OnEnable()
        {
            TrySubscribeToHands();
        }

        void OnDisable()
        {
            if (m_HandSubsystem != null)
                m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem = null;
            RestoreBasePose();
        }

        /// <summary>Called after the completion house is positioned flush with the real floor.</summary>
        public void Configure()
        {
            EnsureContactCollider();
            m_BaseScale = transform.localScale;
            m_BasePosition = transform.position;
            m_VisualHeight = GetVisualWorldHeight();
            m_Response = 0f;
            m_Configured = true;
        }

        void Update()
        {
            if (!m_Configured)
                return;
            if (m_HandSubsystem == null)
                TrySubscribeToHands();
            var target = Time.unscaledTime - m_LastContactTime <= m_ContactReleaseGrace ? 1f : 0f;
            m_Response = Mathf.MoveTowards(m_Response, target, m_ResponseSpeed * Time.deltaTime);
            ApplySoftPose();
        }

        void TrySubscribeToHands()
        {
            var subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (subsystem == null || subsystem == m_HandSubsystem)
                return;
            if (m_HandSubsystem != null)
                m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem = subsystem;
            m_HandSubsystem.updatedHands += OnHandsUpdated;
        }

        void OnHandsUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType != XRHandSubsystem.UpdateType.Dynamic || m_ContactCollider == null)
                return;
            if (HandTouchesHouse(subsystem.leftHand) || HandTouchesHouse(subsystem.rightHand))
                m_LastContactTime = Time.unscaledTime;
        }

        bool HandTouchesHouse(XRHand hand)
        {
            if (!hand.isTracked)
                return false;
            if (m_XROrigin == null)
                m_XROrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var origin = m_XROrigin != null ? m_XROrigin.Origin.transform : null;
            foreach (var jointId in ProbeJoints)
            {
                var joint = hand.GetJoint(jointId);
                if (!joint.TryGetPose(out var trackingPose))
                    continue;
                var worldPosition = PalmPressGestureController.TransformTrackingPose(trackingPose, origin).position;
                if ((m_ContactCollider.ClosestPoint(worldPosition) - worldPosition).sqrMagnitude <=
                    m_ContactRadius * m_ContactRadius)
                    return true;
            }
            return false;
        }

        void EnsureContactCollider()
        {
            if (m_ContactCollider != null)
                return;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            m_ContactCollider = gameObject.AddComponent<BoxCollider>();
            m_ContactCollider.isTrigger = true;
            m_ContactCollider.center = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            m_ContactCollider.size = new Vector3(
                Mathf.Max(.01f, Mathf.Abs(localSize.x)),
                Mathf.Max(.01f, Mathf.Abs(localSize.y)),
                Mathf.Max(.01f, Mathf.Abs(localSize.z)));
        }

        void ApplySoftPose()
        {
            transform.localScale = CalculatePressedScale(m_BaseScale, m_Response, m_MaxCompression, m_HorizontalExpansion);
            var verticalMultiplier = Mathf.Abs(m_BaseScale.y) < .0001f ? 1f : transform.localScale.y / m_BaseScale.y;
            transform.position = m_BasePosition + Vector3.up * CalculateBottomLockOffset(m_VisualHeight, verticalMultiplier);
        }

        float GetVisualWorldHeight()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return 0f;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.size.y;
        }

        void RestoreBasePose()
        {
            if (!m_Configured)
                return;
            transform.localScale = m_BaseScale;
            transform.position = m_BasePosition;
        }
    }
}
