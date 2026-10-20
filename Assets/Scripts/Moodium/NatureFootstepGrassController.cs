using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Grows a bounded trail of grass on the real floor beneath a walking Nature World player.
    /// Grass is deliberately scene-rooted so it is not affected by a tracked-image transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NatureFootstepGrassController : MonoBehaviour
    {
        const float CompletedProgress = .999f;
        static readonly Vector2[] GroundProbeOffsets =
        {
            Vector2.zero,
            new(.25f, 0f), new(-.25f, 0f),
            new(0f, .25f), new(0f, -.25f)
        };

        [Header("Footstep placement")]
        [SerializeField, Min(.05f)] float m_DistanceBetweenGrass = .35f;
        [SerializeField, Min(.01f)] float m_MinimumGrassSeparation = .35f;
        [SerializeField, Min(1)] int m_MaxActiveGrass = 60;
        [SerializeField, Range(0f, 1f)] float m_MinGroundNormalY = .55f;
        [SerializeField, Min(0f)] float m_SurfaceOffset = .005f;
        [SerializeField, Min(.01f)] float m_GrassScaleMultiplier = 1f;

        readonly List<GameObject> m_SpawnedGrass = new();
        Transform m_Viewer;
        ARMeshManager m_SpatialMeshManager;
        GameObject m_GrassPrefab;
        GameObject m_AlternateGrassPrefab;
        Transform m_GrassRoot;
        Vector3 m_LastGrassPosition;
        float m_Progress;
        bool m_NatureWorldActive;
        bool m_CandyWorldActive;
        bool m_HasLastGrassPosition;
        bool m_HasLoggedMissingGround;

        public int SpawnedGrassCount => m_SpawnedGrass.Count;

        public static bool ShouldBeActive(bool natureWorldActive, float normalizedProgress) =>
            natureWorldActive && normalizedProgress >= CompletedProgress;

        public static bool ShouldBeActiveForCandy(bool candyWorldActive, float normalizedProgress) =>
            candyWorldActive && normalizedProgress >= CompletedProgress;

        public static bool HasMinimumHorizontalSeparation(Vector3 candidate, IEnumerable<Vector3> existingPositions,
            float minimumDistance)
        {
            candidate.y = 0f;
            foreach (var existing in existingPositions)
            {
                var horizontalExisting = existing;
                horizontalExisting.y = 0f;
                if (Vector3.Distance(candidate, horizontalExisting) < minimumDistance)
                    return false;
            }
            return true;
        }

        public static int GetInstancesToRecycle(int currentCount, int maximumCount) =>
            Mathf.Max(0, currentCount - Mathf.Max(1, maximumCount) + 1);

        public static Vector2[] GetGroundProbeOffsets() => GroundProbeOffsets;

        public static GameObject SelectFootstepPrefab(GameObject primary, GameObject alternate, int spawnIndex)
        {
            if (alternate == null || spawnIndex % 2 == 0)
                return primary;
            return alternate;
        }

        public static bool IsGeneratedSpatialMeshCollider(Collider collider,
            IEnumerable<MeshFilter> generatedMeshes)
        {
            if (collider is not MeshCollider || generatedMeshes == null)
                return false;

            foreach (var meshFilter in generatedMeshes)
            {
                if (meshFilter == null)
                    continue;
                if (collider.transform == meshFilter.transform ||
                    collider.transform.IsChildOf(meshFilter.transform))
                    return true;
            }
            return false;
        }

        public void Configure(Transform viewer, ARMeshManager spatialMeshManager, GameObject grassPrefab)
        {
            Configure(viewer, spatialMeshManager, grassPrefab, null);
        }

        public void Configure(Transform viewer, ARMeshManager spatialMeshManager, GameObject grassPrefab,
            GameObject alternateGrassPrefab)
        {
            m_CandyWorldActive = false;
            m_Viewer = viewer;
            m_SpatialMeshManager = spatialMeshManager;
            m_GrassPrefab = grassPrefab;
            m_AlternateGrassPrefab = alternateGrassPrefab;
        }

        public void ConfigureCandy(Transform viewer, ARMeshManager spatialMeshManager, GameObject beanPrefab)
        {
            m_CandyWorldActive = true;
            m_Viewer = viewer;
            m_SpatialMeshManager = spatialMeshManager;
            m_GrassPrefab = beanPrefab;
            m_AlternateGrassPrefab = null;
        }

        public void StartTracking()
        {
            var candyWorld = m_CandyWorldActive;
            StopAndClear();
            m_CandyWorldActive = candyWorld;
            m_NatureWorldActive = !candyWorld;
            m_Progress = 0f;
        }

        public void SetProgress(float normalizedProgress) => m_Progress = Mathf.Clamp01(normalizedProgress);

        public void StopAndClear()
        {
            m_NatureWorldActive = false;
            m_CandyWorldActive = false;
            m_Progress = 0f;
            m_HasLastGrassPosition = false;
            m_HasLoggedMissingGround = false;
            foreach (var grass in m_SpawnedGrass)
                if (grass != null)
                    Destroy(grass);
            m_SpawnedGrass.Clear();
            if (m_GrassRoot != null)
                Destroy(m_GrassRoot.gameObject);
            m_GrassRoot = null;
        }

        void Update()
        {
            if ((!ShouldBeActive(m_NatureWorldActive, m_Progress) &&
                 !ShouldBeActiveForCandy(m_CandyWorldActive, m_Progress)) ||
                m_Viewer == null || (m_GrassPrefab == null && m_AlternateGrassPrefab == null) ||
                m_SpatialMeshManager == null)
                return;

            if (!TryFindGroundBelowViewer(out var groundHit))
            {
                if (!m_HasLoggedMissingGround)
                {
                    Debug.LogWarning("[Nature Footstep Grass] Progress is full, but no upward-facing AR spatial mesh was found beneath the player. Waiting for floor meshing.");
                    m_HasLoggedMissingGround = true;
                }
                return;
            }

            m_HasLoggedMissingGround = false;

            var groundPosition = groundHit.point + groundHit.normal * m_SurfaceOffset;
            if (!m_HasLastGrassPosition)
            {
                // Reaching 100% does not place a grass clump under a stationary user.
                m_LastGrassPosition = groundPosition;
                m_HasLastGrassPosition = true;
                return;
            }

            if (HorizontalDistance(groundPosition, m_LastGrassPosition) < m_DistanceBetweenGrass)
                return;

            m_LastGrassPosition = groundPosition;
            if (!HasMinimumHorizontalSeparation(groundPosition, GetActivePositions(), m_MinimumGrassSeparation))
                return;

            SpawnGrass(groundPosition);
        }

        bool TryFindGroundBelowViewer(out RaycastHit groundHit)
        {
            // Use the same generous probe range as NatureForestGrowthController.
            // A direct point beneath a Vision Pro wearer can fall in a reconstruction
            // gap, so sample a small cross around the foot position before giving up.
            var viewerPosition = m_Viewer.position;
            var hasHit = false;
            var bestDistance = float.PositiveInfinity;
            groundHit = default;
            foreach (var offset in GroundProbeOffsets)
            {
                var origin = new Vector3(viewerPosition.x + offset.x, viewerPosition.y + 4f,
                    viewerPosition.z + offset.y);
                var hits = Physics.RaycastAll(origin, Vector3.down, 8f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (!IsSpatialGround(hit.collider, hit.normal) || hit.distance >= bestDistance)
                        continue;
                    groundHit = hit;
                    bestDistance = hit.distance;
                    hasHit = true;
                }
            }
            return hasHit;
        }

        bool IsSpatialGround(Collider collider, Vector3 normal) =>
            normal.y >= m_MinGroundNormalY && m_SpatialMeshManager != null &&
            IsGeneratedSpatialMeshCollider(collider, m_SpatialMeshManager.meshes);

        IEnumerable<Vector3> GetActivePositions()
        {
            foreach (var grass in m_SpawnedGrass)
                if (grass != null)
                    yield return grass.transform.position;
        }

        void SpawnGrass(Vector3 position)
        {
            if (m_GrassRoot == null)
                m_GrassRoot = new GameObject(m_CandyWorldActive ? "Candy Footstep Beans" : "Nature Footstep Grass").transform;

            var recycleCount = GetInstancesToRecycle(m_SpawnedGrass.Count, m_MaxActiveGrass);
            for (var i = 0; i < recycleCount; i++)
            {
                var oldest = m_SpawnedGrass[0];
                m_SpawnedGrass.RemoveAt(0);
                if (oldest != null)
                    Destroy(oldest);
            }

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var prefab = SelectFootstepPrefab(m_GrassPrefab, m_AlternateGrassPrefab, m_SpawnedGrass.Count);
            if (prefab == null)
                return;
            var grass = Instantiate(prefab, position, rotation, m_GrassRoot);
            grass.name = $"{prefab.name} (Footstep Grass {m_SpawnedGrass.Count + 1})";
            grass.transform.localScale *= m_GrassScaleMultiplier;
            AlignVisualBaseToSurface(grass, position.y);
            m_SpawnedGrass.Add(grass);
        }

        static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        static void AlignVisualBaseToSurface(GameObject instance, float surfaceY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var lowestY = float.PositiveInfinity;
            foreach (var renderer in renderers)
                if (renderer.enabled)
                    lowestY = Mathf.Min(lowestY, renderer.bounds.min.y);
            if (!float.IsPositiveInfinity(lowestY))
                instance.transform.position += Vector3.up * (surfaceY - lowestY);
        }

        void OnDisable() => StopAndClear();
    }
}
