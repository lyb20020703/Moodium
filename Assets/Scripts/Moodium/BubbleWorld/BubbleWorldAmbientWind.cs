using UnityEngine;

namespace Moodium.NatureWorld
{
    /// <summary>Small spatially coherent motion of splat centres; never edits a shared asset mesh.</summary>
    [DisallowMultipleComponent]
    public sealed class BubbleWorldAmbientWind : MonoBehaviour
    {
        MeshFilter m_Filter;
        Mesh m_Source, m_Mesh;
        Vector3[] m_Rest, m_Work;
        Vector2[] m_Phases;
        float[] m_Weights;
        float m_NextUpdate, m_Strength;
        bool m_Animating;

        public void Configure(int layer, bool candy)
        {
            m_Filter = GetComponent<MeshFilter>();
            m_Source = m_Filter.sharedMesh;
            if (m_Source == null || !m_Source.isReadable) return;
            m_Rest = m_Source.vertices;
            m_Work = (Vector3[])m_Rest.Clone();
            var count = m_Rest.Length / 4;
            m_Phases = new Vector2[count];
            m_Weights = new float[count];
            m_Strength = candy ? .004f : .007f;
            for (var i = 0; i < count; i++)
            {
                var v = i * 4;
                var centre = (m_Rest[v] + m_Rest[v + 2]) * .5f;
                m_Phases[i] = new Vector2(centre.x * .7f + centre.z * .43f,
                    centre.y * .8f - centre.z * .31f);
                m_Weights[i] = Mathf.Lerp(.25f, 1f, Mathf.SmoothStep(0, 1,
                    Mathf.InverseLerp(-1.5f, 3f, centre.y)));
            }
            m_NextUpdate = Time.unscaledTime + layer / 20f / 15f;
        }

        public void SetAnimating(bool active)
        {
            if (active == m_Animating || m_Rest == null) return;
            m_Animating = active;
            if (!active)
            {
                if (m_Filter != null) m_Filter.sharedMesh = m_Source;
                return;
            }
            if (m_Mesh == null)
            {
                m_Mesh = Instantiate(m_Source);
                m_Mesh.name = m_Source.name + " (ambient wind)";
                m_Mesh.MarkDynamic();
                var bounds = m_Source.bounds;
                bounds.Expand(.04f);
                m_Mesh.bounds = bounds;
            }
            m_Filter.sharedMesh = m_Mesh;
        }

        public static Vector3 Offset(Vector2 phase, float weight, float strength, float time)
        {
            var gust = .7f + .3f * Mathf.Sin(time * .23f);
            return new Vector3(
                Mathf.Sin(time * .85f + phase.x) + .25f * Mathf.Sin(time * 1.43f + phase.y),
                .25f * Mathf.Sin(time * .67f + phase.y),
                .65f * Mathf.Sin(time * .73f + phase.y + phase.x * .4f)) * (weight * strength * gust);
        }

        void Update()
        {
            if (!m_Animating || Time.unscaledTime < m_NextUpdate) return;
            m_NextUpdate = Time.unscaledTime + 1f / 15f;
            var time = Time.time;
            for (var i = 0; i < m_Phases.Length; i++)
            {
                var offset = Offset(m_Phases[i], m_Weights[i], m_Strength, time);
                var v = i * 4;
                for (var corner = 0; corner < 4; corner++) m_Work[v + corner] = m_Rest[v + corner] + offset;
            }
            m_Mesh.SetVertices(m_Work);
        }

        void OnDestroy()
        {
            if (m_Filter != null) m_Filter.sharedMesh = m_Source;
            if (m_Mesh != null)
            {
                if (Application.isPlaying) Destroy(m_Mesh); else DestroyImmediate(m_Mesh);
            }
        }
    }
}
