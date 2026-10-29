using UnityEngine;
using UnityEngine.UI;

namespace Moodium.CandyWorld
{
    /// <summary>A passive, model-anchored companion to the wrist progress display.</summary>
    public sealed class FloatingSqueezeProgress : MonoBehaviour
    {
        Transform m_Target;
        Camera m_Camera;
        RectTransform m_Fill;
        float m_Progress;

        public void Bind(Transform target, Camera viewer, bool nature)
        {
            m_Target = target;
            m_Camera = viewer;
            if (m_Fill == null)
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(180, 6);
                transform.localScale = Vector3.one * .001f;
                CreateStrip("Track", new Color(.12f, .14f, .18f, .35f));
                m_Fill = CreateStrip("Progress", nature
                    ? new Color(.48f, .85f, .64f, .9f)
                    : new Color(1f, .65f, .79f, .9f));
            }
            gameObject.SetActive(true);
        }

        RectTransform CreateStrip(string label, Color color)
        {
            var child = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(transform, false);
            var image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public void SetProgress(float progress) => m_Progress = Mathf.Clamp01(progress);

        void LateUpdate()
        {
            if (m_Target == null || !m_Target.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }
            var bounds = new Bounds(m_Target.position, Vector3.zero);
            foreach (var renderer in m_Target.GetComponentsInChildren<Renderer>())
                if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
            transform.position = new Vector3(bounds.center.x, bounds.max.y + .065f, bounds.center.z);
            if (m_Camera != null) transform.rotation = m_Camera.transform.rotation;
            if (m_Fill != null) m_Fill.anchorMax = new Vector2(m_Progress, 1);
        }
    }
}
