using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.Interaction
{
    /// <summary>
    /// Self-contained hand/capsule contact detection. Each tracked Capsule owns one
    /// instance, so object and image tracked Capsules can collide independently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackedCapsuleHandCollision : MonoBehaviour
    {
        [SerializeField, Min(0.005f)] float m_JointProbeRadius = 0.025f;

        readonly Collider[] m_HitBuffer = new Collider[12];
        ChocolateCapsuleInteraction m_Capsule;
        XRHandSubsystem m_HandSubsystem;
        XROrigin m_XROrigin;
        bool m_Touching;
        bool m_LoggedMissingSubsystem;

        public bool IsTouching => m_Touching;

        void Awake()
        {
            m_Capsule = GetComponent<ChocolateCapsuleInteraction>();
        }

        void OnEnable()
        {
            TrySubscribe();
        }

        void Update()
        {
            if (m_HandSubsystem == null)
                TrySubscribe();
        }

        void OnDisable()
        {
            if (m_HandSubsystem != null)
                m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem = null;
            SetTouching(false);
        }

        void TrySubscribe()
        {
            var subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader
                ?.GetLoadedSubsystem<XRHandSubsystem>();
            if (subsystem == null)
            {
                if (!m_LoggedMissingSubsystem)
                {
                    m_LoggedMissingSubsystem = true;
                    Debug.LogWarning("[Moodium Capsule Collision] XRHandSubsystem is not available yet.");
                }
                return;
            }

            m_HandSubsystem = subsystem;
            m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem.updatedHands += OnHandsUpdated;
            m_LoggedMissingSubsystem = false;
        }

        void OnHandsUpdated(
            XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType != XRHandSubsystem.UpdateType.Dynamic ||
                m_Capsule == null || !m_Capsule.InteractionEnabled || !isActiveAndEnabled)
            {
                SetTouching(false);
                return;
            }

            var touching = HandTouchesCapsule(subsystem.leftHand) ||
                           HandTouchesCapsule(subsystem.rightHand);
            SetTouching(touching);
        }

        bool HandTouchesCapsule(XRHand hand)
        {
            if (!hand.isTracked)
                return false;

            return JointTouchesCapsule(hand.GetJoint(XRHandJointID.Wrist)) ||
                   JointTouchesCapsule(hand.GetJoint(XRHandJointID.IndexMetacarpal)) ||
                   JointTouchesCapsule(hand.GetJoint(XRHandJointID.MiddleMetacarpal)) ||
                   JointTouchesCapsule(hand.GetJoint(XRHandJointID.LittleMetacarpal));
        }

        bool JointTouchesCapsule(XRHandJoint joint)
        {
            if (!joint.TryGetPose(out var trackingPose))
                return false;

            if (m_XROrigin == null)
                m_XROrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var origin = m_XROrigin != null ? m_XROrigin.Origin.transform : null;
            var worldPose = PalmPressGestureController.TransformTrackingPose(trackingPose, origin);
            var count = Physics.OverlapSphereNonAlloc(
                worldPose.position,
                m_JointProbeRadius,
                m_HitBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var hit = m_HitBuffer[i];
                if (hit != null && hit.GetComponentInParent<ChocolateCapsuleInteraction>() == m_Capsule)
                    return true;
            }
            return false;
        }

        void SetTouching(bool touching)
        {
            if (m_Touching == touching)
                return;
            m_Touching = touching;
            if (m_Capsule == null)
                return;
            if (touching)
                m_Capsule.PalmContactStarted();
            else
                m_Capsule.PalmContactEnded();
        }
    }
}
