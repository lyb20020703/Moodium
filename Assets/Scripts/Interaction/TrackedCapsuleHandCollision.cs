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

        static readonly XRHandJointID[] HandJointIds =
        {
            XRHandJointID.Wrist, XRHandJointID.Palm,
            XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal,
            XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip,
            XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal,
            XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal,
            XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal,
            XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal,
            XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip
        };

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

            var touching = HandTouchesCapsule(subsystem.leftHand, out var contactPosition) ||
                           HandTouchesCapsule(subsystem.rightHand, out contactPosition);
            SetTouching(touching, contactPosition);
        }

        bool HandTouchesCapsule(XRHand hand, out Vector3 contactPosition)
        {
            contactPosition = default;
            if (!hand.isTracked)
                return false;

            foreach (var jointId in HandJointIds)
                if (JointTouchesCapsule(hand.GetJoint(jointId), out contactPosition))
                    return true;
            return false;
        }

        bool JointTouchesCapsule(XRHandJoint joint, out Vector3 contactPosition)
        {
            contactPosition = default;
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
                {
                    contactPosition = hit.ClosestPoint(worldPose.position);
                    return true;
                }
            }
            return false;
        }

        void SetTouching(bool touching)
        {
            SetTouching(touching, default);
        }

        void SetTouching(bool touching, Vector3 contactPosition)
        {
            if (m_Touching == touching)
            {
                if (touching)
                    m_Capsule?.HandContactHeld(contactPosition);
                return;
            }
            m_Touching = touching;
            if (m_Capsule == null)
                return;
            if (touching)
                m_Capsule.HandContactStarted(contactPosition);
            else
                m_Capsule.PalmContactEnded();
        }
    }
}
