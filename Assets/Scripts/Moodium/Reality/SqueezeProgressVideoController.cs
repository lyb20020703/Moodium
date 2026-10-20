using System;
using System.Collections;
using Moodium.Calibration;
using Moodium.Interaction;
using Moodium.Opening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace Moodium.Reality
{
    /// <summary>
    /// Presents a non-looping squeeze video whose timeline follows the existing
    /// CandyEnergyController normalized progress. The controller only owns video
    /// presentation; it never changes squeeze energy or completion gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SqueezeProgressVideoController : MonoBehaviour
    {
        const string SequenceConfigPath = "MoodiumOpening/OpeningVideoSequenceConfig";
        const float TargetTimeTolerance = 0.015f;
        const float PrepareTimeout = 15f;
        const int DiscSegments = 64;
        const int DomeRings = 10;
        const int DomeSegments = 48;
        public const int ForegroundRenderQueue = (int)RenderQueue.Overlay + 100;

        [Header("Tracked Soft presentation")]
        [SerializeField, Min(.01f)] float m_VideoDiameter = .075f;
        [SerializeField, Range(-.05f, .05f)] float m_SurfaceOffset = -.005f;
        [SerializeField, Min(.01f)] float m_DomeRadius = .042f;
        [SerializeField, Range(0f, .5f)] float m_DomeOpacity = .10f;
        [SerializeField] Color m_CandyDomeColor = new(1f, .38f, .62f, 1f);
        [SerializeField] Color m_NatureDomeColor = new(.55f, .86f, .53f, 1f);

        Camera m_Camera;
        VideoClip m_CandyClip;
        VideoClip m_NatureClip;
        OpeningVideoSequenceConfig m_SequenceConfig;
        GameObject m_Display;
        VideoPlayer m_Player;
        RenderTexture m_RenderTexture;
        Material m_Material;
        Material m_DomeMaterial;
        Mesh m_DiscMesh;
        Mesh m_DomeMesh;
        Renderer m_DiscRenderer;
        Renderer m_DomeRenderer;
        Coroutine m_PrepareRoutine;
        Transform m_SurfaceAnchor;
        Vector3 m_AnchorLocalPosition;
        Vector3 m_AnchorLocalNormal = Vector3.up;
        double m_TargetTime;
        float m_NormalizedProgress;
        bool m_Advancing;
        bool m_Prepared;
        bool m_Completed;
        bool m_BoundToSoft;
        bool m_NatureWorld;

        public bool IsVisible => m_DiscRenderer != null && m_DiscRenderer.enabled;
        public VideoClip ActiveClip => m_Player != null ? m_Player.clip : null;

        public void Configure(Camera camera, VideoClip candyClip, VideoClip natureClip)
        {
            m_Camera = camera;
            m_CandyClip = candyClip;
            m_NatureClip = natureClip;
            m_SequenceConfig = Resources.Load<OpeningVideoSequenceConfig>(SequenceConfigPath);
        }

        public void Begin(bool natureWorld)
        {
            var clip = natureWorld ? m_NatureClip : m_CandyClip;
            ResetAndHide();
            m_NatureWorld = natureWorld;
            if (clip == null)
            {
                Debug.LogWarning($"[Moodium Squeeze Video] Missing {(natureWorld ? "Nature" : "Candy")} squeeze video clip.");
                return;
            }

            CreateDisplay(clip);
            m_PrepareRoutine = StartCoroutine(PrepareFirstFrame());
        }

        /// <summary>
        /// Binds the already-preparing video to the accepted tracked CandySoft/NatureSoft.
        /// The display remains scene-rooted so non-uniform calibration scale cannot turn
        /// the circular video into an ellipse.
        /// </summary>
        public void BindToSoft(ChocolateCapsuleInteraction capsule)
        {
            if (capsule == null)
                return;

            var presentation = capsule.GetComponentInParent<CalibratedSensoryBagPresentation>();
            var follower = capsule.GetComponent<TrackedObjectPoseFollower>();
            m_SurfaceAnchor = presentation != null
                ? presentation.transform
                : follower != null && follower.Anchor != null ? follower.Anchor : capsule.transform;

            var trackingCenter = presentation != null
                ? presentation.CenterWorldPosition
                : follower != null && follower.Anchor != null ? follower.Anchor.position : capsule.transform.position;
            var surfaceNormal = presentation != null
                ? presentation.AnchorOrientation * Vector3.up
                : follower != null && follower.Anchor != null ? follower.Anchor.up : capsule.transform.up;
            if (Vector3.Dot(surfaceNormal, Vector3.up) < 0f)
                surfaceNormal = -surfaceNormal;

            var renderers = capsule.GetComponentsInChildren<Renderer>(true);
            var softTopY = trackingCenter.y;
            foreach (var renderer in renderers)
                if (renderer.enabled)
                    softTopY = Mathf.Max(softTopY, renderer.bounds.max.y);

            var worldPosition = BuildSurfacePosition(trackingCenter, softTopY, surfaceNormal, m_SurfaceOffset);
            m_AnchorLocalPosition = m_SurfaceAnchor.InverseTransformPoint(worldPosition);
            m_AnchorLocalNormal = m_SurfaceAnchor.InverseTransformDirection(surfaceNormal).normalized;
            m_BoundToSoft = true;
            UpdateDisplayPose();
            UpdateDisplayVisibility();
        }

        public void SetProgress(float normalizedProgress)
        {
            m_NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            if (m_Completed || !m_Prepared || m_Player == null || m_Player.clip == null)
                return;

            m_TargetTime = ProgressToVideoTime(m_NormalizedProgress, m_Player.length);
            var currentTime = m_Player.time;
            if (m_TargetTime >= m_Player.length - TargetTimeTolerance &&
                currentTime + TargetTimeTolerance >= m_Player.length)
            {
                CompleteAndHide();
                return;
            }
            if (m_TargetTime <= currentTime + TargetTimeTolerance)
            {
                m_Player.Pause();
                m_Advancing = false;
                return;
            }

            m_Advancing = true;
            m_Player.Play();
        }

        public void ResetAndHide()
        {
            if (m_PrepareRoutine != null)
            {
                StopCoroutine(m_PrepareRoutine);
                m_PrepareRoutine = null;
            }
            m_Advancing = false;
            m_Prepared = false;
            m_Completed = false;
            m_BoundToSoft = false;
            m_SurfaceAnchor = null;
            m_NormalizedProgress = 0f;
            if (m_Player != null)
            {
                m_Player.Stop();
                m_Player.targetTexture = null;
            }
            if (m_Display != null)
                Destroy(m_Display);
            m_Display = null;
            m_Player = null;
            m_DiscRenderer = null;
            m_DomeRenderer = null;
            if (m_RenderTexture != null)
            {
                m_RenderTexture.Release();
                Destroy(m_RenderTexture);
                m_RenderTexture = null;
            }
            if (m_Material != null)
            {
                Destroy(m_Material);
                m_Material = null;
            }
            if (m_DomeMaterial != null)
            {
                Destroy(m_DomeMaterial);
                m_DomeMaterial = null;
            }
            if (m_DiscMesh != null)
            {
                Destroy(m_DiscMesh);
                m_DiscMesh = null;
            }
            if (m_DomeMesh != null)
            {
                Destroy(m_DomeMesh);
                m_DomeMesh = null;
            }
        }

        public static double ProgressToVideoTime(float normalizedProgress, double duration)
        {
            return Math.Max(0d, duration) * Mathf.Clamp01(normalizedProgress);
        }

        public static Camera SelectPlacementCamera(Camera liveMainCamera, Camera cachedCamera) =>
            liveMainCamera != null ? liveMainCamera : cachedCamera;

        public static bool HasReachedTarget(double currentTime, double targetTime, double tolerance)
        {
            return currentTime + Math.Max(0d, tolerance) >= targetTime;
        }

        /// <summary>
        /// Maps a point from the unit video disc (-1..1) into a centered square crop of
        /// the source video. Wide clips crop their left/right edges instead of stretching.
        /// </summary>
        public static Vector2 MapDiscPointToVideoUv(Vector2 unitDiscPoint, uint videoWidth, uint videoHeight)
        {
            var width = Mathf.Max(1f, videoWidth);
            var height = Mathf.Max(1f, videoHeight);
            var aspect = width / height;
            var horizontalSpan = aspect >= 1f ? 1f / aspect : 1f;
            var verticalSpan = aspect >= 1f ? 1f : aspect;
            return new Vector2(
                .5f + unitDiscPoint.x * .5f * horizontalSpan,
                .5f + unitDiscPoint.y * .5f * verticalSpan);
        }

        public static Quaternion BuildSurfaceRotation(Vector3 surfaceNormal)
        {
            if (surfaceNormal.sqrMagnitude < .000001f)
                surfaceNormal = Vector3.up;
            return Quaternion.FromToRotation(Vector3.up, surfaceNormal.normalized);
        }

        public static Vector3 BuildSurfacePosition(Vector3 trackingCenter, float softTopY,
            Vector3 surfaceNormal, float surfaceOffset)
        {
            var normal = surfaceNormal.sqrMagnitude < .000001f ? Vector3.up : surfaceNormal.normalized;
            var surfaceCenter = new Vector3(trackingCenter.x, softTopY, trackingCenter.z);
            return surfaceCenter + normal * surfaceOffset;
        }

        void CreateDisplay(VideoClip clip)
        {
            m_Display = new GameObject("Moodium Squeeze Progress Video Display");
            m_Display.name = "Moodium Squeeze Progress Video Display";
            m_DiscMesh = CreateCircularDiscMesh(m_VideoDiameter * .5f, DiscSegments, clip.width, clip.height);
            m_Display.AddComponent<MeshFilter>().sharedMesh = m_DiscMesh;

            m_RenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "Moodium Squeeze Progress Video Render Texture"
            };
            m_RenderTexture.Create();
            m_Material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
            if (m_Material.HasProperty("_BaseMap"))
                m_Material.SetTexture("_BaseMap", m_RenderTexture);
            if (m_Material.HasProperty("_MainTex"))
                m_Material.SetTexture("_MainTex", m_RenderTexture);
            OpeningVideoSurfaceSettings.TransparentAlpha.ApplyTo(m_Material);
            m_Material.renderQueue = ForegroundRenderQueue;
            if (m_Material.HasProperty("_ZWrite")) m_Material.SetFloat("_ZWrite", 0f);
            if (m_Material.HasProperty("_ZTest")) m_Material.SetInt("_ZTest", (int)CompareFunction.Always);
            m_DiscRenderer = m_Display.AddComponent<MeshRenderer>();
            m_DiscRenderer.sortingOrder = short.MaxValue;
            m_DiscRenderer.sharedMaterial = m_Material;
            m_Display.AddComponent<PolySpatialVideoRenderTextureUpdater>().Configure(m_RenderTexture);
            CreateDomeVisual();

            m_Player = m_Display.AddComponent<VideoPlayer>();
            m_Player.playOnAwake = false;
            m_Player.isLooping = false;
            m_Player.source = VideoSource.VideoClip;
            m_Player.clip = clip;
            m_Player.renderMode = VideoRenderMode.RenderTexture;
            m_Player.targetTexture = m_RenderTexture;
            m_Player.loopPointReached += OnLoopPointReached;
            UpdateDisplayVisibility();
        }

        static Mesh CreateCircularDiscMesh(float radius, int segments, uint videoWidth, uint videoHeight)
        {
            segments = Mathf.Max(12, segments);
            radius = Mathf.Max(.001f, radius);
            var vertices = new Vector3[segments + 1];
            var normals = new Vector3[segments + 1];
            var uvs = new Vector2[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = MapDiscPointToVideoUv(Vector2.zero, videoWidth, videoHeight);
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[index + 1] = new Vector3(unit.x * radius, 0f, unit.y * radius);
                normals[index + 1] = Vector3.up;
                uvs[index + 1] = MapDiscPointToVideoUv(unit, videoWidth, videoHeight);
                var triangle = index * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = (index + 1) % segments + 1;
                triangles[triangle + 2] = index + 1;
            }

            var mesh = new Mesh { name = "Moodium Circular Squeeze Video Disc" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        void CreateDomeVisual()
        {
            var dome = new GameObject("Squeeze Video Transparent Dome");
            dome.transform.SetParent(m_Display.transform, false);
            m_DomeMesh = CreateHemisphereMesh(m_DomeRadius, DomeRings, DomeSegments);
            dome.AddComponent<MeshFilter>().sharedMesh = m_DomeMesh;
            m_DomeRenderer = dome.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            m_DomeMaterial = new Material(shader) { name = "Moodium Squeeze Video Dome Material" };
            var tint = m_NatureWorld ? m_NatureDomeColor : m_CandyDomeColor;
            tint.a = m_DomeOpacity;
            if (m_DomeMaterial.HasProperty("_BaseColor")) m_DomeMaterial.SetColor("_BaseColor", tint);
            if (m_DomeMaterial.HasProperty("_Color")) m_DomeMaterial.SetColor("_Color", tint);
            OpeningVideoSurfaceSettings.TransparentAlpha.ApplyTo(m_DomeMaterial);
            m_DomeMaterial.renderQueue = (int)RenderQueue.Transparent + 25;
            if (m_DomeMaterial.HasProperty("_ZWrite")) m_DomeMaterial.SetFloat("_ZWrite", 0f);
            if (m_DomeMaterial.HasProperty("_Cull")) m_DomeMaterial.SetFloat("_Cull", (float)CullMode.Off);
            m_DomeRenderer.sortingOrder = short.MaxValue - 1;
            m_DomeRenderer.sharedMaterial = m_DomeMaterial;
        }

        static Mesh CreateHemisphereMesh(float radius, int rings, int segments)
        {
            radius = Mathf.Max(.001f, radius);
            rings = Mathf.Max(2, rings);
            segments = Mathf.Max(12, segments);
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var ring = 0; ring <= rings; ring++)
            {
                var latitude = ring / (float)rings;
                var theta = latitude * Mathf.PI * .5f;
                var ringRadius = Mathf.Sin(theta) * radius;
                var height = Mathf.Cos(theta) * radius;
                for (var segment = 0; segment <= segments; segment++)
                {
                    var longitude = segment / (float)segments;
                    var phi = longitude * Mathf.PI * 2f;
                    var index = ring * (segments + 1) + segment;
                    var vertex = new Vector3(Mathf.Cos(phi) * ringRadius, height,
                        Mathf.Sin(phi) * ringRadius);
                    vertices[index] = vertex;
                    normals[index] = vertex.normalized;
                    uvs[index] = new Vector2(longitude, latitude);
                }
            }

            var triangles = new int[rings * segments * 6];
            var triangleIndex = 0;
            for (var ring = 0; ring < rings; ring++)
            for (var segment = 0; segment < segments; segment++)
            {
                var current = ring * (segments + 1) + segment;
                var next = current + segments + 1;
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = current + 1;
                triangles[triangleIndex++] = current + 1;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next + 1;
            }

            var mesh = new Mesh { name = "Moodium Squeeze Video Hemisphere" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        void UpdateDisplayPose()
        {
            if (!m_BoundToSoft || m_SurfaceAnchor == null || m_Display == null)
                return;
            var worldPosition = m_SurfaceAnchor.TransformPoint(m_AnchorLocalPosition);
            var worldNormal = m_SurfaceAnchor.TransformDirection(m_AnchorLocalNormal).normalized;
            m_Display.transform.SetPositionAndRotation(worldPosition, BuildSurfaceRotation(worldNormal));
        }

        void UpdateDisplayVisibility()
        {
            var visible = m_BoundToSoft && m_Prepared && !m_Completed;
            if (m_DiscRenderer != null) m_DiscRenderer.enabled = visible;
            if (m_DomeRenderer != null) m_DomeRenderer.enabled = visible;
        }

        IEnumerator PrepareFirstFrame()
        {
            m_Player.Prepare();
            var deadline = Time.realtimeSinceStartup + PrepareTimeout;
            while (m_Player != null && !m_Player.isPrepared && Time.realtimeSinceStartup < deadline)
                yield return null;

            m_PrepareRoutine = null;
            if (m_Player == null || !m_Player.isPrepared)
            {
                Debug.LogWarning("[Moodium Squeeze Video] Video preparation timed out; hiding the squeeze video.");
                ResetAndHide();
                yield break;
            }

            m_Player.time = 0d;
            m_TargetTime = 0d;
            m_Prepared = true;
            m_Completed = false;
            m_Player.Pause();
            UpdateDisplayVisibility();
            SetProgress(m_NormalizedProgress);
            Debug.Log($"[Moodium Squeeze Video] Ready: {m_Player.clip.name}, duration={m_Player.length:0.00}s.");
        }

        void Update()
        {
            if (!m_Advancing || m_Player == null || !m_Prepared)
                return;
            if (!HasReachedTarget(m_Player.time, m_TargetTime, TargetTimeTolerance))
                return;

            if (m_TargetTime >= m_Player.length - TargetTimeTolerance)
            {
                CompleteAndHide();
                return;
            }

            m_Player.time = Math.Min(m_TargetTime, m_Player.length);
            m_Player.Pause();
            m_Advancing = false;
        }

        void LateUpdate() => UpdateDisplayPose();

        void OnLoopPointReached(VideoPlayer player)
        {
            if (player == null)
                return;
            CompleteAndHide();
        }

        void CompleteAndHide()
        {
            m_Completed = true;
            m_Advancing = false;
            if (m_Player != null)
            {
                m_Player.isLooping = false;
                m_Player.Pause();
                m_Player.Stop();
            }
            if (m_Display != null)
                UpdateDisplayVisibility();
        }

        void OnDestroy()
        {
            if (m_Player != null)
                m_Player.loopPointReached -= OnLoopPointReached;
            ResetAndHide();
        }
    }
}
