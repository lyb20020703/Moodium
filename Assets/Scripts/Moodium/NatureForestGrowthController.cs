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
        // Local X is viewer-right and local Y is viewer-forward. Keep the central forward
        // lane open for the sensory bag, with the front trees farther away than the side trees.
        static readonly Vector2[] ViewerRelativeSpawnOffsets =
        {
            new(-2.1f, 3.9f), // distant front-left
            new(2.1f, 3.9f),  // distant front-right
            new(-3.5f, 0.45f), // left side
            new(3.5f, 0.45f),  // right side
            new(0f, -3.0f)     // behind the viewer
        };
        // Trees were already reduced to half their prefab scale. Nature World now
        // needs one third of that displayed size, i.e. one sixth of prefab scale.
        const float TreeScaleMultiplier = 1f / 6f;

        [Header("Ground placement")]
        [Tooltip("visionOS world-space floor height used only while the spatial mesh is unavailable.")]
        [SerializeField] float m_FallbackGroundY = 0f;

        readonly List<GameObject> m_TreePrefabs = new();
        readonly List<GameObject> m_SpawnedTrees = new();
        Transform m_Viewer;
        Transform m_ForestRoot;
        int m_NextThresholdIndex;
        bool m_Running;

        public int SpawnedTreeCount => m_SpawnedTrees.Count;
        public Transform ForestRoot => m_ForestRoot;

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
                // Trees belong to the real room, not to an image anchor, calibrated bag,
                // or any world-flow transform. A scene-root parent prevents a later
                // tracked-parent rotation/scale update from making the grove lean.
                m_ForestRoot = root.transform;
            }

            var forward = Vector3.ProjectOnPlane(m_Viewer.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var spawnOffset = ViewerRelativeSpawnOffsets[index % ViewerRelativeSpawnOffsets.Length];
            var desiredPosition = m_Viewer.position + right * spawnOffset.x + forward * spawnOffset.y;
            var position = FindGroundPosition(desiredPosition);
            // Lock the tree's up vector to gravity. Only yaw is allowed to vary;
            // no image-anchor, camera-roll, or mesh-normal rotation may tilt a tree.
            var baseYaw = Mathf.Atan2(-forward.x, -forward.z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, baseYaw + (index * 47f) % 360f, 0f);
            var prefab = m_TreePrefabs[index % m_TreePrefabs.Count];
            var instance = Instantiate(prefab, position, rotation, m_ForestRoot);
            instance.name = $"{prefab.name} (Nature Forest {index + 1})";
            instance.transform.rotation = rotation;
            instance.transform.localScale *= TreeScaleMultiplier;
            m_SpawnedTrees.Add(instance);
            instance.AddComponent<NatureTreeGrowIn>();
        }

        Vector3 FindGroundPosition(Vector3 desiredPosition)
        {
            var origin = desiredPosition + Vector3.up * 4f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return hit.point;

            // Spatial meshes can be unavailable for a moment at session start. The previous
            // eye-relative fallback left trees visibly floating for seated users. Keep their
            // roots on the world floor until a real mesh hit is available instead.
            return new Vector3(desiredPosition.x, m_FallbackGroundY, desiredPosition.z);
        }

        void OnDisable() => StopAndClear();
    }
}
