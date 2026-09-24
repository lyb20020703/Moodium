using System.Collections.Generic;
using Artngame.TreeGEN.ProceduralIvy;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Bridges Nature World interactions to Ivy Studio's public runtime API. Ivy is only
    /// planted on colliders owned by the scanned spatial mesh, never on tracked props.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NatureIvySpatialGrowthController : MonoBehaviour
    {
        readonly HashSet<Collider> m_SpatialColliders = new();
        Camera m_Camera;
        ARMeshManager m_MeshManager;
        IvyGeneratorTREANT m_Generator;
        int m_MaxStrokes = 3;
        int m_GrownStrokeCount;
        float m_LastColliderRefresh;
        bool m_Running;

        public int GrownStrokeCount => m_GrownStrokeCount;

        public void Configure(IvyGeneratorTREANT generator, int maxStrokes)
        {
            m_Generator = generator;
            m_MaxStrokes = Mathf.Max(1, maxStrokes);
        }

        public void Configure(
            Camera camera,
            ARMeshManager meshManager,
            GameObject ivyGeneratorPrefab,
            Material growMaterial,
            int maxStrokes)
        {
            StopAndClear();
            m_Camera = camera;
            m_MeshManager = meshManager;
            m_MaxStrokes = Mathf.Max(1, maxStrokes);
            if (ivyGeneratorPrefab == null)
            {
                m_Generator = null;
                return;
            }

            var instance = Instantiate(ivyGeneratorPrefab, transform);
            instance.name = "Nature Runtime Ivy Generator";
            m_Generator = instance.GetComponent<IvyGeneratorTREANT>();
            if (m_Generator == null)
            {
                Destroy(instance);
                return;
            }

            // Keep the procedural mesh deliberately light enough for a spatial session.
            m_Generator.player = camera != null ? camera.transform : null;
            m_Generator.runTimePlanting = false;
            m_Generator.addColliderPerIvy = false;
            m_Generator.globalBatchEditorIvy = false;
            m_Generator.autoLOD = false;
            m_Generator.limitPlantingArea = false;
            m_Generator.use3dbranch = false;
            m_Generator.singleMeshBranch = false;
            m_Generator.branchCount = 3;
            m_Generator.maxBranchPositions = 14;
            m_Generator.branchPartLength = 0.07f;
            m_Generator.ivyTrailWidth = 0.014f;
            m_Generator.addLeaves = true;
            m_Generator.flowerChance = 0.04f;
            m_Generator.growIvy = growMaterial != null;
            m_Generator.growFlowers = false;
            m_Generator.growIvySpeed = 0.65f;
            m_Generator.growIvyMaterial = growMaterial;
            RefreshSpatialColliders();
        }

        public void StartGrowth()
        {
            m_Running = m_Generator != null;
            m_GrownStrokeCount = 0;
            RefreshSpatialColliders();
        }

        public bool TryGrowNearInteraction(Vector3 interactionPosition)
        {
            if (!m_Running || m_Camera == null || m_Generator == null ||
                m_GrownStrokeCount >= m_MaxStrokes)
                return false;

            if (Time.time - m_LastColliderRefresh > 1f)
                RefreshSpatialColliders();
            var direction = interactionPosition - m_Camera.transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                return false;

            var hits = Physics.RaycastAll(
                m_Camera.transform.position,
                direction.normalized,
                Mathf.Min(direction.magnitude + 2f, 8f),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
                if (hit.collider != null && m_SpatialColliders.Contains(hit.collider))
                    return TryGrowAtSpatialHit(hit);
            return false;
        }

        public bool TryGrowAtSpatialHit(RaycastHit hit)
        {
            if (!m_Running || m_Generator == null || m_GrownStrokeCount >= m_MaxStrokes ||
                hit.collider == null)
                return false;

            m_Generator.generateIvyA(hit);
            m_GrownStrokeCount++;
            return true;
        }

        public void StopAndClear()
        {
            m_Running = false;
            m_GrownStrokeCount = 0;
            m_SpatialColliders.Clear();
            if (m_Generator != null)
                Destroy(m_Generator.gameObject);
            m_Generator = null;
        }

        void RefreshSpatialColliders()
        {
            m_LastColliderRefresh = Time.time;
            m_SpatialColliders.Clear();
            if (m_MeshManager == null)
                return;
            foreach (var collider in m_MeshManager.GetComponentsInChildren<Collider>(true))
                m_SpatialColliders.Add(collider);
        }

        void OnDisable() => StopAndClear();
    }
}
