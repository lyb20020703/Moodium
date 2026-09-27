using Moodium.Interaction;
using UnityEngine;

namespace Moodium.Reality
{
    public sealed class PushHandInteractionGuide : MonoBehaviour
    {
        GameObject m_Left;
        GameObject m_Right;
        Transform m_Target;
        float m_BaseHeight = .10f;
        float m_Travel = .08f;
        float m_Speed = 0.45f;

        public void Configure(ChocolateCapsuleInteraction target, GameObject leftPrefab, GameObject rightPrefab)
        {
            m_Target = target.transform;
            if (leftPrefab != null) m_Left = Instantiate(leftPrefab, transform);
            if (rightPrefab != null) m_Right = Instantiate(rightPrefab, transform);
            ChocolateCapsuleInteraction.AnyCapsulePinched += OnCapsulePinched;
        }

        void LateUpdate()
        {
            if (m_Target == null) return;
            var down = Mathf.PingPong(Time.time * m_Speed, m_Travel);
            var center = m_Target.position + Vector3.up * (m_BaseHeight - down);
            if (m_Left != null) m_Left.transform.position = center + Vector3.left * .06f;
            if (m_Right != null) m_Right.transform.position = center + Vector3.right * .06f;
        }

        void OnCapsulePinched(ChocolateCapsuleInteraction capsule, Vector3 _) { if (capsule != null && capsule.transform == m_Target) Destroy(gameObject); }
        void OnDestroy() => ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
    }
}
