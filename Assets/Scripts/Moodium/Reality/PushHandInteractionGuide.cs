using Moodium.Interaction;
using Moodium.Calibration;
using UnityEngine;

namespace Moodium.Reality
{
    public sealed class PushHandInteractionGuide : MonoBehaviour
    {
        GameObject m_Left;
        GameObject m_Right;
        Transform m_Target;
        CalibratedSensoryBagPresentation m_Presentation;
        TrackedObjectPoseFollower m_Follower;
        float m_BaseHeight = .10f;
        float m_Travel = .08f;
        float m_Speed = 0.45f;

        public void Configure(ChocolateCapsuleInteraction target, GameObject leftPrefab, GameObject rightPrefab)
        {
            m_Target = target.transform;
            m_Presentation=target.GetComponentInParent<CalibratedSensoryBagPresentation>();
            m_Follower=target.GetComponent<TrackedObjectPoseFollower>();
            if (leftPrefab != null) m_Left = Instantiate(leftPrefab, transform);
            if (rightPrefab != null) m_Right = Instantiate(rightPrefab, transform);
            ChocolateCapsuleInteraction.AnyCapsulePinched += OnCapsulePinched;
        }

        void LateUpdate()
        {
            if (m_Target == null) return;
            var down = Mathf.PingPong(Time.time * m_Speed, m_Travel);
            var orientation=m_Presentation!=null ? m_Presentation.AnchorOrientation :
                m_Follower!=null&&m_Follower.Anchor!=null ? m_Follower.Anchor.rotation : m_Target.rotation;
            transform.SetPositionAndRotation(m_Presentation!=null?m_Presentation.CenterWorldPosition:m_Target.position,orientation);
            var center=Vector3.up*(m_BaseHeight-down);
            if (m_Left != null) m_Left.transform.localPosition = center + Vector3.left * .06f;
            if (m_Right != null) m_Right.transform.localPosition = center + Vector3.right * .06f;
        }

        void OnCapsulePinched(ChocolateCapsuleInteraction capsule, Vector3 _) { if (capsule != null && capsule.transform == m_Target) Destroy(gameObject); }
        void OnDestroy() => ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
    }
}
