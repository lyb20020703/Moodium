using Moodium.Gestures;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.Interaction
{
    /// <summary>Detects either physical palm pressing the currently tracked capsule.</summary>
    [DisallowMultipleComponent]
    public sealed class PalmPressGestureController : MonoBehaviour
    {
        [Header("Palm contact volume (metres)")]
        [SerializeField] Vector3 m_PalmColliderSize = new(0.09f, 0.035f, 0.105f);
        [SerializeField, Range(-0.04f, 0.06f)] float m_PalmForwardOffset = 0.015f;
        [SerializeField, Min(0f)] float m_TargetPadding = 0.008f;
        [SerializeField, Min(0f)] float m_TriggerCooldown = 0.3f;

        [Header("Runtime diagnostics")]
        [SerializeField] bool m_InteractionEnabled;
        [SerializeField] bool m_LeftPalmTracked;
        [SerializeField] bool m_RightPalmTracked;
        [SerializeField] bool m_LeftPalmTouching;
        [SerializeField] bool m_RightPalmTouching;

        readonly Collider[] m_OverlapBuffer = new Collider[24];

        XRHandSubsystem m_HandSubsystem;
        PalmPressStateMachine m_LeftState;
        PalmPressStateMachine m_RightState;
        ChocolateCapsuleInteraction m_Target;
        BoxCollider m_GeneratedTargetCollider;
        bool m_LoggedSubsystemReady;
        bool m_LoggedSubsystemMissing;

        public bool LeftPalmTouching => m_LeftPalmTouching;
        public bool RightPalmTouching => m_RightPalmTouching;

        public void SetTarget(ChocolateCapsuleInteraction target)
        {
            if (m_Target == target)
                return;

            EndCurrentContact();
            RemoveGeneratedTargetCollider();
            m_Target = target;
            if (m_Target != null)
                EnsureTargetCollider();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (m_InteractionEnabled == enabled)
                return;

            m_InteractionEnabled = enabled;
            if (!enabled)
                EndCurrentContact();
        }

        void Awake()
        {
            RebuildStateMachines();
        }

        void OnEnable()
        {
            TrySubscribeToHandSubsystem();
        }

        void Update()
        {
            if (m_HandSubsystem == null)
                TrySubscribeToHandSubsystem();
        }

        void OnDisable()
        {
            if (m_HandSubsystem != null)
                m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem = null;
            EndCurrentContact();
        }

        void OnDestroy()
        {
            RemoveGeneratedTargetCollider();
        }

        void OnValidate()
        {
            m_PalmColliderSize.x = Mathf.Max(0.01f, m_PalmColliderSize.x);
            m_PalmColliderSize.y = Mathf.Max(0.005f, m_PalmColliderSize.y);
            m_PalmColliderSize.z = Mathf.Max(0.01f, m_PalmColliderSize.z);
            if (Application.isPlaying)
            {
                EndCurrentContact();
                RebuildStateMachines();
            }
        }

        void RebuildStateMachines()
        {
            m_LeftState = new PalmPressStateMachine(m_TriggerCooldown);
            m_RightState = new PalmPressStateMachine(m_TriggerCooldown);
        }

        void TrySubscribeToHandSubsystem()
        {
            var subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader
                ?.GetLoadedSubsystem<XRHandSubsystem>();
            if (subsystem == null)
            {
                if (!m_LoggedSubsystemMissing)
                {
                    m_LoggedSubsystemMissing = true;
                    Debug.LogWarning("[Moodium Palm Press] XRHandSubsystem is not available yet.");
                }
                return;
            }

            m_HandSubsystem = subsystem;
            m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem.updatedHands += OnHandsUpdated;
            m_LoggedSubsystemMissing = false;
            if (!m_LoggedSubsystemReady)
            {
                m_LoggedSubsystemReady = true;
                Debug.Log("[Moodium Palm Press] XRHandSubsystem connected.");
            }
        }

        void OnHandsUpdated(
            XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType != XRHandSubsystem.UpdateType.Dynamic)
                return;

            if (!m_InteractionEnabled || m_Target == null || !m_Target.isActiveAndEnabled)
            {
                EndCurrentContact();
                return;
            }

            var now = Time.unscaledTime;
            Physics.SyncTransforms();
            m_LeftPalmTracked = TryGetPalmBox(subsystem.leftHand, out var leftCenter, out var leftRotation);
            m_RightPalmTracked = TryGetPalmBox(subsystem.rightHand, out var rightCenter, out var rightRotation);
            m_LeftPalmTouching = m_LeftPalmTracked && IsTouchingTarget(leftCenter, leftRotation);
            m_RightPalmTouching = m_RightPalmTracked && IsTouchingTarget(rightCenter, rightRotation);

            var leftStarted = m_LeftState.Update(m_LeftPalmTouching, now);
            var rightStarted = m_RightState.Update(m_RightPalmTouching, now);
            if (leftStarted || rightStarted)
            {
                m_Target.PalmContactStarted();
                Debug.Log($"[Moodium Palm Press] Capsule pressed by " +
                          $"{(leftStarted ? "left" : "right")} palm.");
            }

            if (!m_LeftPalmTouching && !m_RightPalmTouching && m_Target.IsPinched)
                m_Target.PalmContactEnded();
        }

        bool TryGetPalmBox(XRHand hand, out Vector3 center, out Quaternion rotation)
        {
            center = default;
            rotation = Quaternion.identity;
            if (!hand.isTracked)
                return false;

            var palm = hand.GetJoint(XRHandJointID.Palm);
            var wrist = hand.GetJoint(XRHandJointID.Wrist);
            var indexMetacarpal = hand.GetJoint(XRHandJointID.IndexMetacarpal);
            var littleMetacarpal = hand.GetJoint(XRHandJointID.LittleMetacarpal);
            if (!palm.TryGetPose(out var palmPose) ||
                !wrist.TryGetPose(out var wristPose) ||
                !indexMetacarpal.TryGetPose(out var indexPose) ||
                !littleMetacarpal.TryGetPose(out var littlePose))
                return false;

            var forward = palmPose.position - wristPose.position;
            var across = indexPose.position - littlePose.position;
            if (forward.sqrMagnitude < 0.000001f || across.sqrMagnitude < 0.000001f)
                return false;

            forward.Normalize();
            var palmNormal = Vector3.Cross(forward, across).normalized;
            if (palmNormal.sqrMagnitude < 0.5f)
                return false;

            center = palmPose.position + forward * m_PalmForwardOffset;
            rotation = Quaternion.LookRotation(forward, palmNormal);
            return true;
        }

        bool IsTouchingTarget(Vector3 center, Quaternion rotation)
        {
            var count = Physics.OverlapBoxNonAlloc(
                center,
                m_PalmColliderSize * 0.5f,
                m_OverlapBuffer,
                rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var candidate = m_OverlapBuffer[i];
                if (candidate != null &&
                    candidate.GetComponentInParent<ChocolateCapsuleInteraction>() == m_Target)
                    return true;
            }
            return false;
        }

        void EnsureTargetCollider()
        {
            if (m_Target.GetComponentInChildren<Collider>(true) != null)
                return;

            var renderers = m_Target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[Moodium Palm Press] Capsule has no renderer for contact bounds.");
                return;
            }

            var root = m_Target.transform;
            var localBounds = ToLocalBounds(root, renderers[0].bounds);
            for (var i = 1; i < renderers.Length; i++)
            {
                var rendererBounds = ToLocalBounds(root, renderers[i].bounds);
                localBounds.Encapsulate(rendererBounds.min);
                localBounds.Encapsulate(rendererBounds.max);
            }

            m_GeneratedTargetCollider = m_Target.gameObject.AddComponent<BoxCollider>();
            m_GeneratedTargetCollider.isTrigger = true;
            m_GeneratedTargetCollider.center = localBounds.center;
            var scale = root.lossyScale;
            var localPadding = new Vector3(
                m_TargetPadding / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                m_TargetPadding / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                m_TargetPadding / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
            m_GeneratedTargetCollider.size = localBounds.size + localPadding * 2f;
        }

        static Bounds ToLocalBounds(Transform root, Bounds worldBounds)
        {
            var center = worldBounds.center;
            var extents = worldBounds.extents;
            var local = new Bounds(root.InverseTransformPoint(center), Vector3.zero);
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                local.Encapsulate(root.InverseTransformPoint(corner));
            }
            return local;
        }

        void EndCurrentContact()
        {
            if (m_Target != null && m_Target.IsPinched)
                m_Target.PalmContactEnded();
            m_LeftState?.Reset();
            m_RightState?.Reset();
            m_LeftPalmTracked = false;
            m_RightPalmTracked = false;
            m_LeftPalmTouching = false;
            m_RightPalmTouching = false;
        }

        void RemoveGeneratedTargetCollider()
        {
            if (m_GeneratedTargetCollider == null)
                return;
            if (Application.isPlaying)
                Destroy(m_GeneratedTargetCollider);
            else
                DestroyImmediate(m_GeneratedTargetCollider);
            m_GeneratedTargetCollider = null;
        }
    }
}
