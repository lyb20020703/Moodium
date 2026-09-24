using System.Collections.Generic;
using UnityEngine;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Adds a small, LOD-backed grove around the player as Nature World energy grows.
    /// This stays deliberately separate from the candy burst and spatial-mesh effects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NatureForestGrowthController : MonoBehaviour
    {
        static readonly float[] SpawnThresholds = { 0.35f, 0.55f, 0.75f, 0.9f, 1f };
        static readonly float[] HorizontalOffsets = { -0.78f, 0.74f, -0.4f, 0.42f, 0f };

        readonly List<GameObject> m_TreePrefabs = new();
        readonly List<GameObject> m_SpawnedTrees = new();
        Transform m_Viewer;
        Transform m_ForestRoot;
        int m_NextThresholdIndex;
        bool m_Running;

        public int SpawnedTreeCount => m_SpawnedTrees.Count;

        public void Configure(Transform viewer, GameObject[] treePrefabs)
        {
            m_Viewer = viewer;
            m_TreePrefabs.Clear();
            if (treePrefabs == null)
                return;

            foreach (var prefab in treePrefabs)
                if (prefab != null)
                    m_TreePrefabs.Add(prefab);
        }

        public void StartGrowth()
        {
            StopAndClear();
            m_Running = true;
            m_NextThresholdIndex = 0;
        }

        public void SetProgress(float normalizedProgress)
        {
            if (!m_Running)
                return;

            var progress = Mathf.Clamp01(normalizedProgress);
            while (m_NextThresholdIndex < SpawnThresholds.Length &&
                   progress >= SpawnThresholds[m_NextThresholdIndex])
            {
                SpawnTree(m_NextThresholdIndex);
                m_NextThresholdIndex++;
            }
        }

        public void StopAndClear()
        {
            m_Running = false;
            foreach (var tree in m_SpawnedTrees)
                if (tree != null)
                    Destroy(tree);
            m_SpawnedTrees.Clear();

            if (m_ForestRoot != null)
                Destroy(m_ForestRoot.gameObject);
            m_ForestRoot = null;
            m_NextThresholdIndex = 0;
        }

        void SpawnTree(int index)
        {
            if (m_Viewer == null || m_TreePrefabs.Count == 0)
                return;

            if (m_ForestRoot == null)
            {
                var root = new GameObject("Nature Forest Growth");
                root.transform.SetParent(transform, false);
                m_ForestRoot = root.transform;
            }

            var forward = Vector3.ProjectOnPlane(m_Viewer.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var distance = 2.15f + (index % 2) * 0.55f;
            var horizontal = HorizontalOffsets[index % HorizontalOffsets.Length];
            var desiredPosition = m_Viewer.position + forward * distance + right * horizontal;
            var position = FindGroundPosition(desiredPosition);
            var rotation = Quaternion.LookRotation(-forward, Vector3.up) *
                           Quaternion.Euler(0f, (index * 47f) % 360f, 0f);
            var prefab = m_TreePrefabs[index % m_TreePrefabs.Count];
            var instance = Instantiate(prefab, position, rotation, m_ForestRoot);
            instance.name = $"{prefab.name} (Nature Forest {index + 1})";
            m_SpawnedTrees.Add(instance);
            instance.AddComponent<NatureTreeGrowIn>();
        }

        Vector3 FindGroundPosition(Vector3 desiredPosition)
        {
            var origin = desiredPosition + Vector3.up * 4f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return hit.point;

            // Spatial meshes can be unavailable for a moment at session start. Keep the tree
            // beneath the viewer rather than placing it at eye level until the mesh is present.
            return desiredPosition + Vector3.down * 1.15f;
        }

        void OnDisable() => StopAndClear();
    }
}
