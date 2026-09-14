using UnityEngine;

namespace Moodium.Flow
{
    public sealed class MoodiumWindowHandle : MonoBehaviour
    {
        Transform m_Window;
        int m_ActivePointer = -1;
        Vector3 m_GrabOffset;

        public void Configure(Transform windowRoot) => m_Window = windowRoot;

        public void PointerStarted(int pointerId, Vector3 position)
        {
            if (m_Window == null || m_ActivePointer >= 0)
                return;
            m_ActivePointer = pointerId;
            m_GrabOffset = m_Window.position - position;
            FaceCurrentUser();
            Debug.Log($"[Moodium Window] Drag started: {m_Window.name}");
        }

        public void PointerMoved(int pointerId, Vector3 position)
        {
            if (pointerId != m_ActivePointer || m_Window == null)
                return;
            m_Window.position = position + m_GrabOffset;
            FaceCurrentUser();
        }

        public void PointerEnded(int pointerId)
        {
            if (pointerId != m_ActivePointer)
                return;
            m_ActivePointer = -1;
            Debug.Log($"[Moodium Window] Drag ended: {(m_Window != null ? m_Window.name : name)}");
        }

        void FaceCurrentUser()
        {
            if (m_Window == null)
                return;
            var camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null)
                return;

            // Moodium panels render toward local -Z, so local +Z points from the user
            // toward the window. Updating this vector while dragging also produces a
            // natural yaw change when the window is moved left or right.
            var awayFromUser = Vector3.ProjectOnPlane(
                m_Window.position - camera.transform.position,
                Vector3.up).normalized;
            if (awayFromUser.sqrMagnitude < 0.001f)
                awayFromUser = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (awayFromUser.sqrMagnitude >= 0.001f)
                m_Window.rotation = Quaternion.LookRotation(awayFromUser, Vector3.up);
        }
    }
}
