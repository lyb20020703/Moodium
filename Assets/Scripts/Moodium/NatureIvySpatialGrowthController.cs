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
        bool m_Suspended;
        public void SetSuspended(bool value) { m_Suspended = value; }
        public void RegisterRoomVisibility(Moodium.Flow.RoomContentVisibility visibility) { if (m_Generator != null) visibility.Register(m_Generator.gameObject); }

        readonly HashSet<Collider> m_SpatialColliders = new();
        readonly HashSet<int> m_LeafVisualizedMeshIds = new();
        Camera m_Camera;
        ARMeshManager m_MeshManager;
        IvyGeneratorTREANT m_Generator;
        [Header("Nature Vine Debug")]
        [SerializeField] bool m_DebugMode;
        [SerializeField] bool m_ForceTestVineGrowth;
        [Header("Surface Search")]
        [SerializeField, Range(0f, 1f)] float m_MinUpNormalDot = .65f;
        [SerializeField] float m_MaxSurfaceSearchDistance = .8f;
        [SerializeField] float m_SurfaceSampleRadius = .12f;
        [SerializeField] int m_SurfaceSampleCount = 8;
        [SerializeField] float m_SurfaceOffset = .004f;
        [Header("PolySpatial Leaf Visuals")]
        [SerializeField] Material m_PolySpatialLeafMaterial;
        [SerializeField, Range(1, 6)] int m_LeafVisualsPerBranch = 3;
        [SerializeField, Range(.01f, .12f)] float m_LeafVisualSize = .045f;
        [SerializeField, Range(0f, .03f)] float m_LeafSurfaceOffset = .008f;
        [Header("Completion Burst")]
        [SerializeField, Range(4, 8)] int m_CompletionBranchCount = 6;
        [SerializeField] float m_CompletionRadius = 1f;
        [SerializeField] float m_StartPointScatterRadius = .12f;
        [SerializeField, Range(0f, 45f)] float m_DirectionRandomness = 15f;
        [SerializeField] float m_CompletionGrowthDuration = 1.5f;
        [SerializeField] float m_ForceTestLength = .75f;
        [SerializeField, TextArea] string m_LastDebugStatus;

        int m_MaxStrokes = 3;
        int m_GrownStrokeCount;
        float m_LastColliderRefresh;
        bool m_Running;
        bool m_CompletionBurstTriggered;
        Vector3 m_LastNatureSoftOrigin;

        public int GrownStrokeCount => m_GrownStrokeCount;
        public string LastDebugStatus => m_LastDebugStatus;

        // The authored Ivy Studio URP prefab uses 3D branch geometry. Its flat-mesh
        // path and custom growth shader both render incorrectly through PolySpatial.
        public static bool ShouldForceFlatIvyGeometry() => false;
        public static bool ShouldUseRuntimeGrowShader() => false;
        public static bool ShouldUseIvyStudioAutoLeaves() => false;

        public void SetNatureSoftOrigin(Vector3 origin) => m_LastNatureSoftOrigin = origin;
        public void SetDebugMode(bool enabled) => m_DebugMode = enabled;

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
            if (ShouldForceFlatIvyGeometry())
                m_Generator.use3dbranch = false;
            m_Generator.singleMeshBranch = false;
            m_Generator.branchCount = 3;
            m_Generator.maxBranchPositions = 14;
            m_Generator.branchPartLength = 0.07f;
            m_Generator.ivyTrailWidth = 0.014f;
            m_Generator.addLeaves = ShouldUseIvyStudioAutoLeaves();
            m_Generator.flowerChance = 0.04f;
            var useRuntimeGrowShader = ShouldUseRuntimeGrowShader() && growMaterial != null;
            m_Generator.growIvy = useRuntimeGrowShader;
            m_Generator.growFlowers = false;
            m_Generator.growIvySpeed = 0.65f;
            m_Generator.growIvyMaterial = useRuntimeGrowShader ? growMaterial : null;
            m_Generator.surfaceOffset = m_SurfaceOffset;
            m_PolySpatialLeafMaterial ??= Resources.Load<Material>("MoodiumNature/NatureIvyLeaf");
            m_LeafVisualizedMeshIds.Clear();
            RefreshSpatialColliders();
        }

        public void StartGrowth()
        {
            m_Running = m_Generator != null;
            m_GrownStrokeCount = 0;
            m_CompletionBurstTriggered = false;
            RefreshSpatialColliders();
        }

        public bool TryGrowNearInteraction(Vector3 interactionPosition)
        {
            m_LastNatureSoftOrigin = interactionPosition;
            if (!m_Running || m_Generator == null ||
                m_GrownStrokeCount >= m_MaxStrokes)
                return false;

            if (Time.time - m_LastColliderRefresh > 1f)
                RefreshSpatialColliders();
            Trace("Pinch Detected → Nature Interaction Accepted → Surface Search Started", interactionPosition);
            var resolver = CreateSurfaceResolver();
            if (resolver.TryFindSurfaceBelow(interactionPosition + Vector3.up * .15f, out var hit))
            {
                TraceSurface("Surface Found", hit);
                return RequestIvy(hit, true, "Pinch Vine");
            }
            Trace("Surface Failed: no upward AR Spatial Mesh below NatureSoft.", interactionPosition);
            return false;
        }

        /// <summary>
        /// Called by the shared spatial-pointer path when a pinch begins on scanned room geometry.
        /// The pointer's target collider is preserved so this does not have to guess a different
        /// surface from the camera-to-pointer ray.
        /// </summary>
        public bool TryGrowFromSpatialPointer(Collider targetCollider, Vector3 interactionPosition)
        {
            if (!m_Running || m_Camera == null || m_Generator == null)
            {
                Debug.Log("[Nature Ivy] Ignored spatial pinch: generator is not running.");
                return false;
            }
            if (!IsSpatialMeshCollider(targetCollider))
            {
                Debug.Log("[Nature Ivy] Ignored spatial pinch: pointer target is not an AR mesh collider.");
                return false;
            }

            var contact = targetCollider.ClosestPoint(interactionPosition);
            var ray = new Ray(m_Camera.transform.position, contact - m_Camera.transform.position);
            if (ray.direction.sqrMagnitude > .0001f && targetCollider.Raycast(ray, out var hit, 8f))
            {
                var grown = TryGrowAtSpatialHit(hit);
                Debug.Log($"[Nature Ivy] Spatial pinch {(grown ? "grew ivy" : "did not grow ivy")} at {hit.point}.");
                return grown;
            }

            Debug.Log("[Nature Ivy] Pointer target was valid, but a surface hit could not be resolved.");
            return false;
        }

        /// <summary>True only for colliders supplied by the room mesh, never tracked bag visuals.</summary>
        public bool IsSpatialMeshCollider(Collider collider)
        {
            if (collider == null)
                return false;
            if (Time.time - m_LastColliderRefresh > 1f)
                RefreshSpatialColliders();
            return IsTrackedSpatialMeshCollider(collider);
        }

        public bool TryGrowAtSpatialHit(RaycastHit hit)
        {
            if (!m_Running || m_Generator == null || m_GrownStrokeCount >= m_MaxStrokes ||
                hit.collider == null)
                return false;
            return RequestIvy(hit, true, "Environment Vine");
        }

        /// <summary>Debug-only entry: use the latest NatureSoft position, without pinch or progress gating.</summary>
        [ContextMenu("Force Test Vine Growth")]
        public void ForceTestVineGrowth()
        {
            TryForceTestGrowth(m_LastNatureSoftOrigin == Vector3.zero ? transform.position : m_LastNatureSoftOrigin);
        }

        public bool TryForceTestGrowth(Vector3 natureSoftOrigin)
        {
            if (!m_Running || m_Generator == null)
            {
                Trace("Force Test failed: generator is not running.", natureSoftOrigin);
                return false;
            }
            RefreshSpatialColliders();
            Trace("Force Test → Surface Search Started", natureSoftOrigin);
            if (!CreateSurfaceResolver().TryFindSurfaceBelow(natureSoftOrigin + Vector3.up * .15f, out var hit))
            {
                Trace("Force Test → Surface Failed", natureSoftOrigin);
                return false;
            }
            TraceSurface("Force Test → Surface Found", hit);
            return RequestIvy(hit, false, "Force Test", m_ForceTestLength);
        }

        public bool TryStartCompletionBurst(Vector3 natureSoftOrigin)
        {
            if (!m_Running || m_Generator == null || m_CompletionBurstTriggered)
                return false;
            // Completion is a world-state transition, not a retry loop: consume the one
            // chance even when the current spatial mesh has no usable tabletop yet.
            m_CompletionBurstTriggered = true;
            RefreshSpatialColliders();
            if (!CreateSurfaceResolver().TryFindSurfaceBelow(natureSoftOrigin + Vector3.up * .15f, out var primaryHit))
            {
                Trace("Completion Burst → Surface Failed", natureSoftOrigin);
                return false;
            }
            TraceSurface($"Completion Burst Triggered: branches={m_CompletionBranchCount}", primaryHit);
            StartCoroutine(GrowCompletionBurst(primaryHit));
            return true;
        }

        public void StopAndClear()
        {
            m_Running = false;
            m_GrownStrokeCount = 0;
            m_CompletionBurstTriggered = false;
            m_SpatialColliders.Clear();
            m_LeafVisualizedMeshIds.Clear();
            if (m_Generator != null)
                Destroy(m_Generator.gameObject);
            m_Generator = null;
        }

        void RefreshSpatialColliders()
        {
            m_LastColliderRefresh = Time.time;
            m_SpatialColliders.Clear();
            if (m_MeshManager != null)
                foreach (var collider in m_MeshManager.GetComponentsInChildren<Collider>(true))
                    m_SpatialColliders.Add(collider);
            if (m_SpatialColliders.Count == 0 && m_MeshManager == null)
                foreach (var collider in FindObjectsByType<MeshCollider>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    m_SpatialColliders.Add(collider);
        }

        bool IsTrackedSpatialMeshCollider(Collider collider)
        {
            if (collider == null)
                return false;
            if (m_SpatialColliders.Contains(collider))
                return true;
            return m_MeshManager != null &&
                   collider.GetComponentInParent<ARMeshManager>() == m_MeshManager;
        }

        void Update()
        {
            if (m_Suspended) return;
            if (!m_ForceTestVineGrowth)
                return;
            m_ForceTestVineGrowth = false;
            ForceTestVineGrowth();
        }

        NatureIvySurfaceResolver CreateSurfaceResolver() => new(
            m_SpatialColliders, m_MinUpNormalDot, m_MaxSurfaceSearchDistance,
            m_SurfaceSampleRadius, m_SurfaceSampleCount);

        bool RequestIvy(RaycastHit hit, bool consumeManualBudget, string source, float requestedLength = -1f)
        {
            if (hit.collider == null || m_Generator == null)
                return false;
            var previousBranches = m_Generator.transform.childCount;
            var originalPositions = m_Generator.maxBranchPositions;
            if (requestedLength > 0f)
                m_Generator.maxBranchPositions = Mathf.Max(2, Mathf.CeilToInt(requestedLength / Mathf.Max(.01f, m_Generator.branchPartLength)));
            TraceSurface($"{source} → Ivy Request → generateIvyA Called", hit);
            m_Generator.generateIvyA(hit);
            m_Generator.maxBranchPositions = originalPositions;
            if (consumeManualBudget)
                m_GrownStrokeCount++;
            StartCoroutine(InspectGeneratedIvy(source, previousBranches, hit.normal));
            return true;
        }

        System.Collections.IEnumerator InspectGeneratedIvy(string source, int previousBranches, Vector3 surfaceNormal)
        {
            yield return null;
            while (m_Suspended) yield return null;
            var renderers = m_Generator != null ? m_Generator.GetComponentsInChildren<MeshRenderer>(true) : null;
            var created = m_Generator != null && m_Generator.transform.childCount > previousBranches;
            if (renderers == null || renderers.Length == 0)
            {
                Trace($"{source} → Generated Object {(created ? "Found" : "Not Found")} → Renderer Not Found", transform.position);
                yield break;
            }
            CreatePolySpatialLeafVisuals(surfaceNormal);
            Trace($"{source} → MeshRenderer Count={renderers.Length}", transform.position);
            foreach (var renderer in renderers)
            {
                var material = renderer.sharedMaterial;
                var shader = material != null && material.shader != null ? material.shader.name : "<none>";
                Trace($"{source} → Generated Object {(created ? "Found" : "Not Found")} → Renderer Found: enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}, material={material?.name ?? "<none>"}, shader={shader}, bounds={renderer.bounds}, position={renderer.transform.position}, scale={renderer.transform.lossyScale}", renderer.transform.position);
            }
        }

        void CreatePolySpatialLeafVisuals(Vector3 surfaceNormal)
        {
            if (m_Generator == null || m_PolySpatialLeafMaterial == null)
                return;

            var normalizedNormal = surfaceNormal.sqrMagnitude > .0001f ? surfaceNormal.normalized : Vector3.up;
            foreach (var meshFilter in m_Generator.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter == null || meshFilter.gameObject == m_Generator.gameObject ||
                    meshFilter.name.StartsWith("Nature Ivy Leaf (PolySpatial)", System.StringComparison.Ordinal) ||
                    meshFilter.sharedMesh == null || !m_LeafVisualizedMeshIds.Add(meshFilter.GetInstanceID()))
                    continue;

                var vertices = meshFilter.sharedMesh.vertices;
                if (vertices == null || vertices.Length == 0)
                    continue;
                for (var i = 0; i < m_LeafVisualsPerBranch; i++)
                {
                    var vertexIndex = Mathf.Clamp(
                        Mathf.FloorToInt((i + .5f) * vertices.Length / m_LeafVisualsPerBranch),
                        0,
                        vertices.Length - 1);
                    var leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    leaf.name = "Nature Ivy Leaf (PolySpatial)";
                    var collider = leaf.GetComponent<Collider>();
                    if (collider != null)
                        Destroy(collider);
                    leaf.transform.SetParent(meshFilter.transform.parent, true);
                    leaf.transform.position = meshFilter.transform.TransformPoint(vertices[vertexIndex]) + normalizedNormal * m_LeafSurfaceOffset;
                    leaf.transform.rotation = Quaternion.LookRotation(normalizedNormal) *
                                              Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
                    var size = m_LeafVisualSize * Random.Range(.75f, 1.2f);
                    leaf.transform.localScale = new Vector3(size, size, 1f);
                    leaf.GetComponent<MeshRenderer>().sharedMaterial = m_PolySpatialLeafMaterial;
                }
            }
        }

        System.Collections.IEnumerator GrowCompletionBurst(RaycastHit primaryHit)
        {
            var originalBranchCount = m_Generator.branchCount;
            var originalDirectionality = m_Generator.directionality;
            var originalPositions = m_Generator.maxBranchPositions;
            m_Generator.branchCount = 1;
            m_Generator.maxBranchPositions = Mathf.Max(2, Mathf.CeilToInt(m_CompletionRadius / Mathf.Max(.01f, m_Generator.branchPartLength)));
            for (var i = 0; i < m_CompletionBranchCount; i++)
            {
                while (m_Suspended) yield return null;
                var angle = (360f * i / m_CompletionBranchCount) + Random.Range(-m_DirectionRandomness, m_DirectionRandomness);
                var baseDirection = Vector3.ProjectOnPlane(Vector3.right, primaryHit.normal);
                if (baseDirection.sqrMagnitude < .0001f)
                    baseDirection = Vector3.ProjectOnPlane(Vector3.forward, primaryHit.normal);
                var direction = Quaternion.AngleAxis(angle, primaryHit.normal) * baseDirection.normalized;
                var scatter = direction * Random.Range(0f, m_StartPointScatterRadius);
                var searchOrigin = primaryHit.point + scatter + primaryHit.normal * .2f;
                if (CreateSurfaceResolver().TryFindSurfaceBelow(searchOrigin, out var branchHit))
                {
                    m_Generator.directionality = new Vector4(direction.x, direction.y, direction.z, 0f);
                    TraceSurface($"Completion Branch {i + 1}/{m_CompletionBranchCount} → Surface Found", branchHit);
                    RequestIvy(branchHit, false, $"Completion Branch {i + 1}");
                }
                else
                    Trace($"Completion Branch {i + 1}/{m_CompletionBranchCount} → Surface Failed", searchOrigin);
                yield return new WaitForSeconds(m_CompletionGrowthDuration / Mathf.Max(1, m_CompletionBranchCount));
            }
            if (m_Generator != null)
            {
                m_Generator.branchCount = originalBranchCount;
                m_Generator.directionality = originalDirectionality;
                m_Generator.maxBranchPositions = originalPositions;
            }
        }

        void TraceSurface(string stage, RaycastHit hit) => Trace(
            $"{stage}; mesh={hit.collider?.name ?? "<none>"}; point={hit.point}; normal={hit.normal}; distance={hit.distance}", hit.point);

        void Trace(string stage, Vector3 _)
        {
            m_LastDebugStatus = stage;
            if (m_DebugMode || stage.StartsWith("Force Test", System.StringComparison.Ordinal))
                Debug.Log($"[Nature Vine] {stage}");
        }

        void OnDisable() => StopAndClear();
    }
}
