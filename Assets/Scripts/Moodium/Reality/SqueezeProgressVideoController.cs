using System;
using System.Collections;
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
        public const int ForegroundRenderQueue = (int)RenderQueue.Overlay + 100;

        Camera m_Camera;
        VideoClip m_CandyClip;
        VideoClip m_NatureClip;
        OpeningVideoSequenceConfig m_SequenceConfig;
        GameObject m_Display;
        VideoPlayer m_Player;
        RenderTexture m_RenderTexture;
        Material m_Material;
        Coroutine m_PrepareRoutine;
        double m_TargetTime;
        float m_NormalizedProgress;
        bool m_Advancing;
        bool m_Prepared;
        bool m_Completed;

        public bool IsVisible => m_Display != null && m_Display.activeSelf;
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
            if (clip == null)
            {
                Debug.LogWarning($"[Moodium Squeeze Video] Missing {(natureWorld ? "Nature" : "Candy")} squeeze video clip.");
                return;
            }

            // Match the Opening Video: resolve the live head-tracked main camera at
            // the exact moment the panel is created. The cached camera is only a
            // fallback for editor or transitional frames where Camera.main is absent.
            var camera = SelectPlacementCamera(Camera.main, m_Camera);
            if (camera == null)
            {
                Debug.LogWarning("[Moodium Squeeze Video] No camera is available for the shared opening-video placement.");
                return;
            }

            CreateDisplay(camera, clip);
            m_PrepareRoutine = StartCoroutine(PrepareFirstFrame());
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

        void CreateDisplay(Camera camera, VideoClip clip)
        {
            m_Display = GameObject.CreatePrimitive(PrimitiveType.Quad);
            m_Display.name = "Moodium Squeeze Progress Video Display";
            OpeningVideoDisplayPlacement.Apply(m_Display.transform, camera, m_SequenceConfig);
            Destroy(m_Display.GetComponent<Collider>());

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
            var displayRenderer = m_Display.GetComponent<Renderer>();
            displayRenderer.sortingOrder = short.MaxValue;
            displayRenderer.sharedMaterial = m_Material;
            m_Display.AddComponent<PolySpatialVideoRenderTextureUpdater>().Configure(m_RenderTexture);

            m_Player = m_Display.AddComponent<VideoPlayer>();
            m_Player.playOnAwake = false;
            m_Player.isLooping = false;
            m_Player.source = VideoSource.VideoClip;
            m_Player.clip = clip;
            m_Player.renderMode = VideoRenderMode.RenderTexture;
            m_Player.targetTexture = m_RenderTexture;
            m_Player.loopPointReached += OnLoopPointReached;
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
                m_Display.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Player != null)
                m_Player.loopPointReached -= OnLoopPointReached;
            ResetAndHide();
        }
    }
}
