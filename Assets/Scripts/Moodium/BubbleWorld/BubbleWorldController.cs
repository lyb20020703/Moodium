using System;
using System.Collections;
using System.Collections.Generic;
using Moodium.Flow;
using Moodium.Interaction;
using Unity.PolySpatial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Completion reward for Nature World and Candy World: a small bubble holding a tiny copy of that world's scan
    /// is summoned by a ring. Touching it (fingertip, or look + pinch) makes it fly to the viewer, swell around them
    /// and then spread the world's points out from near to far until the whole scan surrounds them.
    ///
    /// Rendering stays on the stock URP/Unlit shader: every point is a small quad whose colour comes from one texel
    /// of point_colors.png, and the sky behind the points is an inward-facing panorama sphere. Each world has its own
    /// folder (Resources/MoodiumBubbleWorld/ for Nature, Resources/MoodiumBubbleWorld/Candy/ for Candy) with a baked
    /// Meshes/ subfolder (Moodium ▸ Bubble World ▸ Bake Meshes); missing meshes are built at runtime from points.bytes.
    /// </summary>
    public enum LeaveTarget { Room, Exit }

    [DisallowMultipleComponent]
    public sealed class BubbleWorldController : MonoBehaviour
    {
        public enum Phase { Idle, Armed, Waiting, Floating, Approach, Envelop, Reveal, Done, Rising, Sinking, Inside, Leaving, Portal, HandingOff }

        BubbleWorldController m_Nested;
        bool m_FromPortal;
        BubbleWorldPlay m_Play;
        bool m_ExitingHandoff;
        /// <summary>
        /// What fills the gaps between points far away.
        /// Soft: a 64×32 blur of the panorama — only its colours, so it reads as the world's air and never shows as a
        /// separate photo. Photo: the original panorama. None: no sky; the room shows through the gaps.
        /// </summary>
        public enum SkyStyle { Soft, Photo, None }

        [Header("Sky")]
        [SerializeField] SkyStyle m_SkyStyle = SkyStyle.Soft;
        [Tooltip("Draw the sky before every point layer (native visionOS sorting group). Without it the sky sphere and " +
                 "the point shells share the same centre, so RealityKit's distance sort can put the sky in front.")]
        [SerializeField] bool m_FixedSkyOrder = true;
        [Header("Hand wake")]
        [Tooltip("Moving a hand inside the world pulls small points of the scene along (BubbleHandWake).")]
        [SerializeField] bool m_HandWake = true;
        public SkyStyle Sky => m_SkyStyle;

        [Header("Nested handoff")]
        [SerializeField] float m_OuterFadeStartRadius = 1f;
        [SerializeField] float m_OuterFadeEndRadius = 4f;
        [SerializeField, Range(.9f, .99f)] float m_NestedSkyScale = .97f;
        [Tooltip("Seconds after entering a world before the next-layer bubble / exit bubble appear. 0 = at once " +
                 "(they used to wait 10 s).")]
        [SerializeField, Min(0f)] float m_NextBubbleDelaySeconds = 0f;
        [Tooltip("Build the next layer while the viewer is still in the room, so its bubble is ready the moment the " +
                 "viewer is inside. Off: it is built after entering (a few seconds' wait).")]
        [SerializeField] bool m_PrepareNestedEarly = true;
        public static float OuterAlpha(float radius, float start = 1f, float end = 4f) =>
            1f - Mathf.InverseLerp(start, end, radius);

        const float CompletedProgress = .999f;
        /// <summary>Points within this radius (metres, scan scale) live inside the small bubble (4 of 20 layers for the shipped scan).</summary>
        public const float MiniWorldRadius = 10.5f;
        public const float SkyMaxRadius = 80f;
        public const int BackgroundRenderQueue = 1000;

        [Header("Appear")]
        [SerializeField] bool m_SqueezeBreath = true;
        [Tooltip("Relative to the head when the bubble appears: x = right, y = up, z = forward (yaw only).")]
        [SerializeField] Vector3 m_AppearOffset = new(-.35f, -.15f, .9f);
        [SerializeField, Min(.05f)] float m_BubbleRadius = .15f;
        [SerializeField, Min(0f)] float m_BobAmplitude = .015f;
        [SerializeField, Min(.1f)] float m_BobPeriod = 3f;
        [SerializeField] float m_SpinDegreesPerSecond = 6f;
        [SerializeField, Range(0f, 1f)] float m_MembraneAlpha = .85f;
        [SerializeField] Color m_NatureFilmTint = Color.white;
        [SerializeField] Color m_CandyFilmTint = new(1f, .9f, .97f, 1f);

        [Header("Touch")]
        [SerializeField, Min(0f)] float m_TouchPadding = .01f;

        [Header("Grow")]
        [SerializeField, Min(.1f)] float m_ApproachSeconds = 1.4f;
        [SerializeField, Min(.2f)] float m_ApproachRadius = .6f;
        [SerializeField, Min(.1f)] float m_EnvelopSeconds = 1.2f;
        [SerializeField, Min(.1f)] float m_RevealSeconds = 3f;
        [SerializeField, Min(.05f)] float m_LayerGrowSeconds = .25f;
        [SerializeField, Min(0)] int m_SparklesPerLayer = 3;
        [Tooltip("After the last layer lights up, the sky fades in at SkyMaxRadius to close the gaps.")]
        [SerializeField, Min(.1f)] float m_SkyFadeSeconds = 1.5f;
        [Tooltip("Put the scan's ground on the real floor (XR Origin height) when the head height looks plausible.")]
        [SerializeField] bool m_AlignGroundToFloor = true;

        public event Action Appeared;
        public event Action Touched;
        public event Action Revealed;
        /// <summary>A layer finished opening around the viewer, with that layer's folder. Fires for the first
        /// layer and (forwarded) for the nested one, unlike Revealed which the nested layer keeps to itself.</summary>
        public event Action<string> LayerRevealed;

        public Phase CurrentPhase => m_Phase;

        Camera m_Viewer;
        string m_Folder = BubbleWorldData.NatureFolder;
        string m_OuterFolder = BubbleWorldData.NatureFolder;
        AudioClip m_TouchSound;
        Phase m_Phase = Phase.Idle;
        Coroutine m_Routine;
        Coroutine m_BuildRoutine;
        bool m_CoreLayersReady;
        bool m_AllLayersReady;
        // The next-layer bubble and the exit bubble appear right where the viewer's finger just touched to enter.
        // A touch only counts once the fingertip has been away from them, so a resting hand never triggers them.
        bool m_PortalArmed, m_HomeArmed;

        // Scene objects
        GameObject m_Root;          // bubble centre; the scan centre (eye point) sits here
        Transform m_WorldRoot;      // scaled copy of the scan (k = radius / MiniWorldRadius)
        Transform m_Membrane;
        Collider m_MembraneCollider;
        Transform m_Sky;
        readonly List<GameObject> m_Layers = new();
        readonly List<Mesh> m_RuntimeMeshes = new();
        Vector3[] m_LayerRestPositions;
        Quaternion[] m_LayerRestRotations;
        Material m_HomeMaterial;
        Material m_PointMaterial, m_SkyMaterial, m_SkyFadeMaterial, m_MembraneMaterial;
        MeshRenderer m_SkyRenderer;
        VisionOSSortingGroup m_WorldSorting;
        BubbleHandWake m_Wake;
        AudioSource m_Audio;
        BubbleSplatMotion m_Motion;

        // Data
        float m_EyeHeight;
        float[] m_LayerRadii;
        int m_MiniLayerCount;
        float m_BobPhase;
        float m_Radius;

        // Hands
        XRHandSubsystem m_Hands;
        XROrigin m_Origin;

        static readonly Dictionary<string, bool> s_HasData = new();

        /// <summary>True when <paramref name="folder"/>/points.bytes is in the build.</summary>
        public static bool HasData(string folder)
        {
            if (!s_HasData.TryGetValue(folder, out var has))
                s_HasData[folder] = has = Resources.Load<TextAsset>(BubbleWorldData.PointsResource(folder)) != null;
            return has;
        }

        public string Folder => m_Folder;

        public static bool ShouldAppear(float normalizedProgress) => normalizedProgress >= CompletedProgress;

        // ---------- Pure helpers (EditMode tests) ----------

        /// <summary>Smoothstep ease used for every phase.</summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Bubble radius during the approach (t 0..1): small → arm's-length bubble.</summary>
        public static float ApproachRadius(float t, float from, float to) => Mathf.Lerp(from, to, Ease(t));

        /// <summary>
        /// Envelop radius (t 0..1). Grows exponentially so the swell feels even: each 10% of the time multiplies the
        /// radius by the same factor (0.6 m → 10.5 m ≈ ×1.33 per step).
        /// </summary>
        public static float EnvelopRadius(float t, float from, float to)
        {
            from = Mathf.Max(.001f, from);
            return from * Mathf.Pow(to / from, Ease(t));
        }

        /// <summary>The bubble film fades out once the viewer is well inside it.</summary>
        public static float MembraneAlpha(float radius, float baseAlpha) =>
            baseAlpha * (1f - Mathf.InverseLerp(1f, 2.5f, radius));

        /// <summary>Index of the first layer that does not fit inside the small bubble.</summary>
        public static int MiniLayerCount(float[] layerOuterRadii, float miniRadius)
        {
            if (layerOuterRadii == null) return 0;
            var n = 0;
            while (n < layerOuterRadii.Length && layerOuterRadii[n] <= miniRadius) n++;
            return n;
        }

        /// <summary>Number of near layers needed for the visible miniature bubble.</summary>
        public static int WarmupLayerCount(int miniLayerCount, int totalLayers) =>
            Mathf.Clamp(miniLayerCount, 1, Mathf.Max(1, totalLayers));

        /// <summary>When (0..1 of the reveal) the j-th of <paramref name="remaining"/> outer layers switches on.</summary>
        public static float LayerRevealTime(int j, int remaining) => remaining <= 0 ? 0f : j / (float)remaining;

        /// <summary>
        /// The sky inside the small bubble fades out while it swells past the viewer, so the room shows through the
        /// gaps between points until the reveal is finished (then the sky fades back in far away).
        /// </summary>
        public static float SkyAlphaWhileEnveloping(float radius) => 1f - Mathf.InverseLerp(.8f, 1.6f, radius);

        /// <summary>Scale-in of a newly revealed layer (0.9 → 1 with a small overshoot).</summary>
        public static float LayerGrowScale(float t)
        {
            t = Mathf.Clamp01(t);
            var overshoot = Mathf.Sin(t * Mathf.PI) * .03f;
            return Mathf.Lerp(.9f, 1f, Ease(t)) + overshoot * (1f - t);
        }

        /// <summary>
        /// Where the scan centre ends up: at the viewer's head horizontally, and — when the head height above the floor
        /// is plausible — at floor + scanned eye height, so the scanned ground lies on the real floor.
        /// </summary>
        public static Vector3 FinalCenter(Vector3 head, float floorY, float scanEyeHeight, bool alignToFloor)
        {
            if (!alignToFloor) return head;
            var headAboveFloor = head.y - floorY;
            if (headAboveFloor < .9f || headAboveFloor > 2.1f || scanEyeHeight <= 0f) return head;
            return new Vector3(head.x, floorY + scanEyeHeight, head.z);
        }

        public static bool IsTouching(Vector3 fingertip, Vector3 center, float radius, float padding) =>
            (fingertip - center).sqrMagnitude <= (radius + padding) * (radius + padding);

        /// <summary>Yaw (degrees) that turns the scan's forward (+Z, the big tree) to face along <paramref name="gaze"/>.</summary>
        public static float GazeYaw(Vector3 gaze)
        {
            gaze.y = 0f;
            return gaze.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(gaze.x, gaze.z) * Mathf.Rad2Deg;
        }

        // ---------- Public API (CandyWorldManager) ----------

        public void ConfigureTouchSound(AudioClip clip) => m_TouchSound = clip;

        /// <summary>
        /// Arms the reward for a new world session. <paramref name="folder"/> is BubbleWorldData.NatureFolder or
        /// CandyFolder. Nothing is shown until progress reaches 100%.
        /// </summary>
        public void Begin(Camera viewer, string folder)
        {
            m_HasOuterEntry = false;
            Begin(viewer, folder, 0);
        }

        void Begin(Camera viewer, string folder, int natureWorldDepth)
        {
            StopAndClear();
            m_Viewer = viewer;
            m_Folder = string.IsNullOrEmpty(folder) ? BubbleWorldData.NatureFolder : folder;
            if (natureWorldDepth == 0) m_OuterFolder = m_Folder;
            m_NatureWorldDepth = natureWorldDepth;
            if (!HasData(m_Folder))
            {
                Debug.LogWarning($"[Bubble World] Missing Resources/{BubbleWorldData.PointsResource(m_Folder)}.bytes; the bubble world reward is off for this world.");
                return;
            }
            m_Phase = Phase.Armed;
            // Start building while the user is still in the room. This moves mesh
            // deserialization and GPU setup away from the eventual bubble tap.
            m_BuildRoutine = StartCoroutine(Build());
        }

        public void SetProgress(float normalizedProgress)
        {
            // Progress alone never summons a bubble. Rings own the invitation.
        }

        /// <summary>Same as touching the bubble (look + pinch, or debug).</summary>
        public void Touch()
        {
            if (m_Phase == Phase.Portal) EnterFromPortal();
            else if (m_Phase == Phase.Floating || m_Phase == Phase.Rising) m_TouchRequested = true;
        }

        public void StopAndClear()
        {
            if (m_Nested != null) { m_Nested.StopAndClear(); Destroy(m_Nested.gameObject); m_Nested = null; }
            ClearVisuals();
        }

        void ClearVisuals()
        {
            m_ExitingHandoff = false;
            m_Play=null;
            m_Motion = null;
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
            if (m_BuildRoutine != null) StopCoroutine(m_BuildRoutine);
            m_BuildRoutine = null;
            m_CoreLayersReady = false;
            m_AllLayersReady = false;
            m_TouchRequested = false;
            m_Phase = Phase.Idle;
            m_Breath = 0f; m_BreathScale = 1f; m_WanderFade = 1f; m_WanderTarget = 1f;
            m_Wandering = false; m_InsideAt = 0f; m_BobPhase = 0f; m_Radius = 0f;
            m_From = m_Hover = m_Entry = m_HomeTarget = Vector3.zero;
            if (m_Root != null) m_Root.SetActive(false);
            if (m_Home != null) m_Home.SetActive(false);
            if (m_Home != null) Destroy(m_Home);
            m_Home = null;
            m_Layers.Clear();
            m_LayerRestPositions = null;
            m_LayerRestRotations = null;
            if (m_Root != null) Destroy(m_Root);
            m_Root = null; m_WorldRoot = null; m_Membrane = null; m_MembraneCollider = null; m_Sky = null; m_SkyRenderer = null; m_Audio = null;
            m_WorldSorting = null; m_Wake = null;   // destroyed with m_Root
            foreach (var mesh in m_RuntimeMeshes) if (mesh != null) Destroy(mesh);
            m_RuntimeMeshes.Clear();
            DestroyMaterial(ref m_HomeMaterial);
            DestroyMaterial(ref m_PointMaterial);
            DestroyMaterial(ref m_SkyMaterial);
            DestroyMaterial(ref m_SkyFadeMaterial);
            DestroyMaterial(ref m_MembraneMaterial);
        }

        void OnDisable() => StopAndClear();

        public event Action<LeaveTarget> Left;
        public event Action HomeRequested;
        public event Action Wandered;
        Vector3 m_From, m_Hover, m_Entry, m_HomeTarget;
        Vector3 m_OuterEntryFrom, m_OuterEntryHover;
        float m_InsideAt, m_Breath, m_BreathScale = 1f, m_WanderFade = 1f;
        float m_WanderTarget = 1f;
        bool m_Wandering, m_HasOuterEntry;
        int m_NatureWorldDepth;
        GameObject m_Home;

        public static Vector3 SummonPosition(Vector3 from, Vector3 hover, float t) =>
            Vector3.Lerp(from, hover, Ease(t)) + Vector3.up * (Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * .12f);
        public static float LeaveRadius(float t) => EnvelopRadius(t, MiniWorldRadius, .15f);
        public static float LayerCollapseTime(int index, int count) => count <= 1 ? 0f : (count - 1 - index) / (float)(count - 1);
        public static bool WanderState(bool away, float distance) => away ? distance > .6f : distance > .8f;

        public void Summon(Vector3 from, Vector3 hover)
        {
            if (m_Phase != Phase.Armed) return;
            m_From = from; m_Hover = hover;
            if (m_NatureWorldDepth == 0)
            {
                m_OuterEntryFrom = from;
                m_OuterEntryHover = hover;
                m_HasOuterEntry = true;
            }
            m_Phase = Phase.Rising;
            m_Routine = StartCoroutine(Run());
        }

        /// <summary>Reopens the last room entry after returning from the bubble world.</summary>
        public bool ResummonAtLastEntry()
        {
            if (m_Phase != Phase.Armed || m_Viewer == null || !m_HasOuterEntry)
                return false;
            var from = m_OuterEntryFrom;
            var hover = m_OuterEntryHover;
            // Returning from a completed bubble keeps its meshes/materials hidden.
            // Reuse that warmed cache instead of synchronously rebuilding it again.
            if (m_Root == null || m_Folder != m_OuterFolder || !m_AllLayersReady)
                Begin(m_Viewer, m_OuterFolder, 0);
            Summon(from, hover);
            return true;
        }
        public void Dismiss()
        {
            if (m_Phase != Phase.Floating && m_Phase != Phase.Rising) return;
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Sink());
        }
        IEnumerator Sink()
        {
            m_Phase = Phase.Sinking;
            if (m_Root != null)
            {
                var start = m_Root.transform.position;
                var radius = m_Radius;
                for (float t = 0; t < .6f; t += Time.deltaTime)
                {
                    m_Root.transform.position = Vector3.Lerp(start, m_From, Ease(t / .6f));
                    SetRadius(Mathf.Lerp(radius, .001f, Ease(t / .6f)));
                    yield return null;
                }
                m_Root.SetActive(false);
            }
            m_TouchRequested = false; m_Phase = Phase.Armed; m_Routine = null;
        }
        public void SetBreath(float amount) => m_Breath = float.IsFinite(amount) ? Mathf.Clamp01(amount) : 0f;
        public void SetWanderFade(float alpha) => m_WanderTarget = float.IsFinite(alpha) ? Mathf.Clamp(alpha, .25f, 1f) : 1f;
        public void Leave(LeaveTarget target)
        {
            if (m_Nested != null && m_Phase == Phase.HandingOff)
            {
                if (m_Nested.m_Phase == Phase.Leaving) return;
                m_ExitingHandoff = true;
                m_Routine = StartCoroutine(FadeOuterOnExit());
                m_Nested.Leave(target);
                return;
            }
            if (m_Phase != Phase.Inside && m_Phase != Phase.Approach && m_Phase != Phase.Envelop && m_Phase != Phase.Reveal) return;
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Phase = Phase.Leaving;
            if (m_Motion != null) m_Motion.SetStage(BubbleSplatMotion.Stage.Leaving);
            m_Routine = StartCoroutine(Return(target));
        }
        IEnumerator Return(LeaveTarget target)
        {
            if (m_Nested != null) { m_Nested.StopAndClear(); Destroy(m_Nested.gameObject); m_Nested = null; }
            if (m_Home != null) m_Home.SetActive(false);
            m_Breath = 0f; m_BreathScale = 1f;
            m_WorldRoot.localScale = Vector3.one * (m_Radius / MiniWorldRadius);
            m_Sky.localScale = Vector3.one * SkyMaxRadius;
            m_SkyRenderer.sharedMaterial = m_SkyFadeMaterial;
            for (float t = 0; t < 1f; t += Time.deltaTime)
            { BubbleWorldData.SetAlpha(m_SkyFadeMaterial, (1f - Ease(t)) * m_WanderFade); yield return null; }
            m_Sky.gameObject.SetActive(false);
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                for (int i = m_MiniLayerCount; i < m_Layers.Count; i++)
                    if (t / 2f >= LayerCollapseTime(i - m_MiniLayerCount, m_Layers.Count - m_MiniLayerCount)) m_Layers[i].SetActive(false);
                yield return null;
            }
            for (int i = m_MiniLayerCount; i < m_Layers.Count; i++) m_Layers[i].SetActive(false);
            var start = m_Root.transform.position;
            var viewer = ResolveViewer();
            var destination = m_From + Vector3.up * .3f;
            if (!float.IsFinite(destination.x) || !float.IsFinite(destination.y) || !float.IsFinite(destination.z))
                destination = viewer != null ? viewer.transform.position + Quaternion.Euler(0, GazeYaw(viewer.transform.forward), 0) * Vector3.forward * .5f : start;
            m_Membrane.gameObject.SetActive(true);
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                m_Root.transform.position = Vector3.Lerp(start, destination, Ease(t / 1.2f));
                SetRadius(LeaveRadius(t / 1.2f)); yield return null;
            }
            if (m_TouchSound != null && m_Audio != null) m_Audio.PlayOneShot(m_TouchSound);
            for (float t = 0; t < .12f; t += Time.deltaTime)
            {
                SetRadius(.15f * Mathf.Lerp(1f, 1.25f, t / .12f));
                BubbleWorldData.SetAlpha(m_MembraneMaterial, m_MembraneAlpha * (1f - t / .12f));
                yield return null;
            }
            m_Root.SetActive(false); m_TouchRequested = false;
            m_Phase = Phase.Armed; m_Routine = null;
            Left?.Invoke(target);
            // Return() dropped the next layer; build it again in the room so a re-entry shows its bubble at once.
            if (m_PrepareNestedEarly && m_Root != null) PrepareNested();
        }
        void ResetCached()
        {
            m_Root.SetActive(true); m_Membrane.gameObject.SetActive(true); m_MembraneCollider.enabled = true;
            m_Sky.gameObject.SetActive(true); m_Sky.localScale = Vector3.one * MiniWorldRadius;
            m_SkyRenderer.sharedMaterial = m_SkyMaterial;
            m_BreathScale = 1f; m_Breath = 0; m_WanderFade = 1f; m_WanderTarget = 1f; m_Wandering = false;
            m_TouchRequested = false; m_BobPhase = 0f;
            if (m_Home != null) m_Home.SetActive(false);
            RestoreQuietLayerPose();
            BubbleWorldData.SetAlpha(m_PointMaterial, 1f);
            foreach (var layer in m_Layers) { layer.SetActive(m_Layers.IndexOf(layer) < m_MiniLayerCount); layer.transform.localScale = Vector3.one; }
        }

        void CacheQuietLayerPose()
        {
            m_LayerRestPositions = new Vector3[m_Layers.Count];
            m_LayerRestRotations = new Quaternion[m_Layers.Count];
            for (var i = 0; i < m_Layers.Count; i++)
            {
                m_LayerRestPositions[i] = m_Layers[i].transform.localPosition;
                m_LayerRestRotations[i] = m_Layers[i].transform.localRotation;
            }
        }

        void RestoreQuietLayerPose()
        {
            if (m_LayerRestPositions == null || m_LayerRestRotations == null) return;
            var count = Mathf.Min(m_Layers.Count, m_LayerRestPositions.Length);
            for (var i = 0; i < count; i++)
            {
                m_Layers[i].transform.localPosition = m_LayerRestPositions[i];
                m_Layers[i].transform.localRotation = m_LayerRestRotations[i];
            }
        }

        public static BubbleSplatMotion.Stage MotionStage(Phase phase)
        {
            switch (phase)
            {
                case Phase.Inside: return BubbleSplatMotion.Stage.Inside;
                case Phase.HandingOff: return BubbleSplatMotion.Stage.HandingOff;
                case Phase.Leaving: return BubbleSplatMotion.Stage.Leaving;
                case Phase.Idle: case Phase.Armed: case Phase.Done: return BubbleSplatMotion.Stage.Dormant;
                default: return BubbleSplatMotion.Stage.Miniature;
            }
        }
        void Update()
        {
            if (m_Motion != null) m_Motion.SetStage(MotionStage(m_Phase));
            if (m_Wake != null) m_Wake.Active = m_Phase == Phase.Inside && m_WanderFade > .6f;
            if(m_Play!=null)m_Play.SetInside(m_Phase==Phase.Inside&&m_WanderFade>.6f);
            CheckEditorMouse();
            if (m_Phase == Phase.Portal)
            {
                m_WorldRoot.Rotate(Vector3.up, m_SpinDegreesPerSecond * Time.deltaTime, Space.Self);
                var touchingPortal = FingertipTouching(m_Root.transform.position, m_Radius);
                if (!m_PortalArmed) m_PortalArmed = !touchingPortal;
                else if (touchingPortal) EnterFromPortal();
                return;
            }
            if (m_Phase == Phase.HandingOff)
            {
                if (!m_ExitingHandoff && m_Nested != null && m_PointMaterial != null)
                    BubbleWorldData.SetAlpha(m_PointMaterial, OuterAlpha(m_Nested.m_Radius, m_OuterFadeStartRadius, m_OuterFadeEndRadius));
                if (m_Nested != null && m_Nested.m_Phase == Phase.Reveal && m_Sky != null)
                    m_Sky.gameObject.SetActive(false);
                return;
            }
            if (m_Phase != Phase.Inside) return;
            // The GPU moves splats; keep layer frames and the horizon fixed.
            var viewer = ResolveViewer(); if (viewer == null) return;
            var offset = viewer.transform.position - m_Entry; offset.y = 0;
            var away = WanderState(m_Wandering, offset.magnitude);
            if (away && !m_Wandering) Wandered?.Invoke();
            m_Wandering = away;
            m_WanderFade = Mathf.MoveTowards(m_WanderFade, Mathf.Min(m_WanderTarget, away ? .25f : 1f), Time.deltaTime * .75f / (away ? .4f : .6f));
            BubbleWorldData.SetAlpha(m_PointMaterial, m_WanderFade);
            m_SkyRenderer.sharedMaterial = m_SkyFadeMaterial;
            BubbleWorldData.SetAlpha(m_SkyFadeMaterial, m_WanderFade);
            var breath = m_SqueezeBreath ? Mathf.Lerp(1f, 1.015f, m_Breath) : 1f;
            m_BreathScale = Mathf.MoveTowards(m_BreathScale, breath, Time.deltaTime * .015f / (breath > m_BreathScale ? .4f : 1.2f));
            m_WorldRoot.localScale = Vector3.one * m_BreathScale;
            m_Sky.localScale = Vector3.one * SkyScaleForBreath(m_BreathScale);
            if (Time.time - m_InsideAt < m_NextBubbleDelaySeconds) return;
            if (m_Nested != null)
            {
                if (!m_Nested.m_CoreLayersReady) return;
                if (m_Nested.m_Phase == Phase.Armed)
                {
                    m_HomeTarget = PortalPosition(viewer);
                    m_Nested.ShowPortal(m_HomeTarget, .08f);
                }
                if (Vector3.Angle(viewer.transform.forward, m_HomeTarget - viewer.transform.position) > 60f)
                    m_HomeTarget = PortalPosition(viewer);
                m_Nested.SetPortalTarget(m_HomeTarget);
                return;
            }
            if (m_Home == null)
            {
                m_Home = GameObject.CreatePrimitive(PrimitiveType.Sphere); m_Home.name = "Home Bubble";
                m_Home.transform.SetParent(transform, true); m_Home.transform.localScale = Vector3.one * .16f;
                if (m_HomeMaterial == null) m_HomeMaterial = new Material(m_MembraneMaterial);
                BubbleWorldData.SetAlpha(m_HomeMaterial, m_MembraneAlpha);
                m_Home.GetComponent<Renderer>().sharedMaterial = m_HomeMaterial;
                m_Home.AddComponent<MoodiumSpatialButton>().Configure(null, null, RequestHome);
                m_HomeTarget = viewer.transform.position + Quaternion.Euler(0, GazeYaw(viewer.transform.forward), 0) * Vector3.forward * .6f - Vector3.up * .25f;
                m_Home.transform.position = m_HomeTarget;
                m_HomeArmed = false;
            }
            m_Home.SetActive(true);
            if (Vector3.Angle(viewer.transform.forward, m_HomeTarget - viewer.transform.position) > 60f)
                m_HomeTarget = viewer.transform.position + Quaternion.Euler(0, GazeYaw(viewer.transform.forward), 0) * Vector3.forward * .6f - Vector3.up * .25f;
            m_Home.transform.position = Vector3.Lerp(m_Home.transform.position, m_HomeTarget, 1f - Mathf.Exp(-Time.deltaTime * 5f));
            var touchingHome = FingertipTouching(m_Home.transform.position, .08f);
            if (!m_HomeArmed) m_HomeArmed = !touchingHome;
            else if (touchingHome) RequestHome();
        }

        public static float SkyScaleForBreath(float breathScale) => SkyMaxRadius / Mathf.Max(.001f, breathScale);

        public const int SkySortOrder = 0, PointLayerSortOrder = 1, HandWakeSortOrder = 2;

        /// <summary>Texture for the sky sphere. Soft falls back to the photo (with a warning) if panorama_soft is missing.</summary>
        public static Texture2D SkyTexture(string folder, SkyStyle style, Texture2D panorama)
        {
            if (style != SkyStyle.Soft) return panorama;
            var soft = Resources.Load<Texture2D>(BubbleWorldData.SoftPanoramaResource(folder));
            if (soft != null) return soft;
            Debug.LogWarning($"[Bubble World] Missing Resources/{BubbleWorldData.SoftPanoramaResource(folder)}; using the photo panorama.");
            return panorama;
        }

        public static void ConfigureTransparentPoints(Material material)
        {
            if (material == null) return;
            Moodium.Opening.OpeningVideoSurfaceSettings.TransparentAlpha.ApplyTo(material);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        void CheckEditorMouse()
        {
#if UNITY_EDITOR
            Vector2 position;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            position = mouse.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.GetMouseButtonDown(0)) return;
            position = Input.mousePosition;
#else
            return;
#endif
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            var viewer = ResolveViewer();
            if (viewer == null) return;
            var ray = viewer.ScreenPointToRay(position);
            var collider = m_Phase == Phase.Floating || m_Phase == Phase.Rising || m_Phase == Phase.Portal ? m_MembraneCollider :
                m_Phase == Phase.Inside && m_Home != null && m_Home.activeInHierarchy ? m_Home.GetComponent<Collider>() : null;
            if (collider == null || !collider.enabled || !collider.Raycast(ray, out _, 100f)) return;
            if (m_Phase == Phase.Floating || m_Phase == Phase.Rising || m_Phase == Phase.Portal) Touch(); else RequestHome();
#endif
#endif
        }
        void RequestHome()
        {
            if (m_Phase != Phase.Inside) return;
            HomeRequested?.Invoke();
            Leave(LeaveTarget.Room);
        }

        static Vector3 PortalPosition(Camera viewer) => viewer.transform.position +
            Quaternion.Euler(0, GazeYaw(viewer.transform.forward), 0) * Vector3.forward * .6f - Vector3.up * .25f;

        void PrepareNested()
        {
            if (m_NatureWorldDepth != 0 || m_Nested != null) return;
            var folder = m_Folder == BubbleWorldData.NatureFolder ? BubbleWorldData.NestedNatureFolder :
                m_Folder == BubbleWorldData.CandyFolder ? BubbleWorldData.NestedCandyFolder : null;
            if (folder == null || !HasData(folder)) return;
            var host = new GameObject("Nested Bubble Controller");
            host.transform.SetParent(transform, false);
            m_Nested = host.AddComponent<BubbleWorldController>();
            m_Nested.ConfigureTouchSound(m_TouchSound);
            m_Nested.m_NestedSkyScale = m_NestedSkyScale;
            m_Nested.m_SkyStyle = m_SkyStyle;
            m_Nested.m_FixedSkyOrder = m_FixedSkyOrder;
            m_Nested.m_HandWake = m_HandWake;
            m_Nested.Begin(m_Viewer, folder, 1);
            m_Nested.HomeRequested += () => HomeRequested?.Invoke();
            m_Nested.Wandered += () => Wandered?.Invoke();
            m_Nested.Left += target =>
            {
                ClearVisuals();
                if (m_Nested != null) { m_Nested.StopAndClear(); Destroy(m_Nested.gameObject); m_Nested = null; }
                m_Phase = Phase.Armed;
                Left?.Invoke(target);
            };
            m_Nested.Touched += () =>
            {
                m_Phase = Phase.HandingOff;
                if (m_Motion != null) m_Motion.SetStage(BubbleSplatMotion.Stage.HandingOff);
            };
            m_Nested.Revealed += () => { ClearVisuals(); m_Phase = Phase.HandingOff; };
            m_Nested.LayerRevealed += folder => LayerRevealed?.Invoke(folder);
        }

        public void ShowPortal(Vector3 position, float radius)
        {
            if (m_Phase != Phase.Armed || !m_CoreLayersReady) return;
            ResetCached();
            m_Root.transform.SetPositionAndRotation(position, Quaternion.identity);
            SetRadius(radius);
            m_PortalArmed = false;
            m_Phase = Phase.Portal;
        }

        public void SetPortalTarget(Vector3 position)
        {
            if (m_Phase == Phase.Portal)
                m_Root.transform.position = Vector3.Lerp(m_Root.transform.position, position, 1f - Mathf.Exp(-Time.deltaTime * 5f));
        }

        public void EnterFromPortal()
        {
            if (m_Phase != Phase.Portal) return;
            m_FromPortal = true;
            var outer = transform.parent != null ? transform.parent.GetComponent<BubbleWorldController>() : null;
            if (outer != null) m_From = outer.m_OuterEntryFrom;
            m_Phase = Phase.Approach;
            m_MembraneCollider.enabled = false;
            Touched?.Invoke();
            if (Application.isPlaying)
                Moodium.CandyWorld.CandyWorldManager.SpawnSoftSqueezeFeedback(m_Root.transform.position);
            if (m_TouchSound != null) m_Audio.PlayOneShot(m_TouchSound);
            m_Routine = StartCoroutine(ApproachAndReveal(ResolveViewer()));
        }

        IEnumerator FadeOuterOnExit()
        {
            for (float t = 0; t < .4f; t += Time.deltaTime)
            {
                BubbleWorldData.SetAlpha(m_PointMaterial, 1f - t / .4f);
                if (m_SkyRenderer != null) m_SkyRenderer.sharedMaterial = m_SkyFadeMaterial;
                BubbleWorldData.SetAlpha(m_SkyFadeMaterial, 1f - t / .4f);
                yield return null;
            }
            ClearVisuals();
            m_Phase = Phase.HandingOff;
        }

        // ---------- Sequence ----------

        bool m_TouchRequested;

        IEnumerator Run()
        {
            m_Phase = Phase.Rising;
            var viewer = ResolveViewer();
            if (viewer == null)
            {
                Debug.LogWarning("[Bubble World] No camera; cannot show the bubble.");
                m_Phase = Phase.Idle;
                yield break;
            }

            while (!m_CoreLayersReady && m_BuildRoutine != null)
                yield return null;
            if (m_Root != null) ResetCached();
            if (m_Root == null)
            {
                m_Phase = Phase.Idle;
                yield break;
            }

            // Appear: pop in at the lower left of the gaze.
            var head = viewer.transform.position;
            var yaw = Quaternion.Euler(0f, GazeYaw(viewer.transform.forward), 0f);
            var home = m_Hover;
            m_Root.transform.SetPositionAndRotation(home, Quaternion.identity);
            m_WorldRoot.localRotation = yaw;
            for (var t = 0f; t < .8f; t += Time.deltaTime)
            {
                m_Root.transform.position = SummonPosition(m_From, home, t / .8f);
                SetRadius(m_BubbleRadius * Ease(t / .8f));
                yield return null;
            }
            SetRadius(m_BubbleRadius);

            m_Root.transform.position = home;
            m_Phase = Phase.Floating;
            Appeared?.Invoke();

            // Float until touched.
            var spin = 0f;
            while (!m_TouchRequested && !FingertipTouching(m_Root.transform.position, m_Radius))
            {
                m_BobPhase += Time.deltaTime / m_BobPeriod * Mathf.PI * 2f;
                m_Root.transform.position = home + Vector3.up * (Mathf.Sin(m_BobPhase) * m_BobAmplitude);
                spin += m_SpinDegreesPerSecond * Time.deltaTime;
                m_WorldRoot.localRotation = yaw * Quaternion.Euler(0f, spin, 0f);
                yield return null;
            }

            // Touch: wobble, sparkle, chime.
            m_Phase = Phase.Approach;
            if (m_MembraneCollider != null) m_MembraneCollider.enabled = false; // never swallow other pinches while it grows
            Touched?.Invoke();
            // Let room point-cloud deactivation reach the renderer before the
            // bubble starts its growth and feedback work.
            yield return null;
            Moodium.CandyWorld.CandyWorldManager.SpawnSoftSqueezeFeedback(m_Root.transform.position);
            if (m_TouchSound != null && m_Audio != null) m_Audio.PlayOneShot(m_TouchSound);
            for (var t = 0f; t < .3f; t += Time.deltaTime)
            {
                var wobble = 1f - Mathf.Sin(t / .3f * Mathf.PI * 2f) * Mathf.Exp(-t * 9f) * .12f;
                SetRadius(m_BubbleRadius * wobble);
                yield return null;
            }

            m_FromPortal = false;
            yield return ApproachAndReveal(viewer);
        }

        IEnumerator ApproachAndReveal(Camera viewer)
        {
            var startRadius = m_Radius;
            // Approach starts from the actual invitation pose and radius.
            var startPos = m_Root.transform.position;
            var startRot = m_WorldRoot.localRotation;
            for (var t = 0f; t < m_ApproachSeconds; t += Time.deltaTime)
            {
                var u = t / m_ApproachSeconds;
                var target = Target(viewer);
                m_Root.transform.position = Vector3.Lerp(startPos, target.center, Ease(u));
                m_WorldRoot.localRotation = Quaternion.Slerp(startRot, target.yaw, Ease(u));
                SetRadius(ApproachRadius(u, startRadius, m_ApproachRadius));
                yield return null;
            }

            // Envelop: swell past the viewer to the size of the mini world. Lock the final pose now so walking or
            // turning does not drag the world along afterwards.
            m_Phase = Phase.Envelop;
            m_Entry = viewer.transform.position;
            var final = Target(viewer);
            m_Root.transform.position = final.center;
            m_WorldRoot.localRotation = final.yaw;
            // Both layers: the small bubble's sky fades out while it swells past the viewer and the sky only comes
            // back last, far away, after the points (below). The second layer used to fade its full sky in here,
            // before any of its points had spread out, so its panorama was the first and biggest thing in view.
            m_SkyRenderer.sharedMaterial = m_SkyFadeMaterial;
            for (var t = 0f; t < m_EnvelopSeconds; t += Time.deltaTime)
            {
                SetRadius(EnvelopRadius(t / m_EnvelopSeconds, m_ApproachRadius, MiniWorldRadius));
                var skyAlpha = SkyAlphaWhileEnveloping(m_Radius);
                BubbleWorldData.SetAlpha(m_SkyFadeMaterial, skyAlpha);
                if (skyAlpha <= 0f && m_Sky.gameObject.activeSelf) m_Sky.gameObject.SetActive(false);
                yield return null;
            }
            SetRadius(MiniWorldRadius);
            m_Membrane.gameObject.SetActive(false);
            m_Sky.gameObject.SetActive(false);

            // The visible mini world was ready immediately; wait here only when a
            // user has touched unusually quickly, so the full reveal never hitches.
            while (!m_AllLayersReady && m_BuildRoutine != null)
                yield return null;
            if (!m_AllLayersReady)
            {
                m_Phase = Phase.Idle;
                yield break;
            }

            // Reveal: the rest of the scan spreads out shell by shell, the sky receding just behind it.
            m_Phase = Phase.Reveal;
            var remaining = m_Layers.Count - m_MiniLayerCount;
            var started = new float[m_Layers.Count];
            for (var i = 0; i < started.Length; i++) started[i] = -1f;
            var elapsed = 0f;
            var finished = remaining <= 0;
            while (!finished)
            {
                elapsed += Time.deltaTime;
                finished = true;
                for (var j = 0; j < remaining; j++)
                {
                    var index = m_MiniLayerCount + j;
                    var layer = m_Layers[index];
                    if (started[index] < 0f)
                    {
                        if (elapsed < LayerRevealTime(j, remaining) * m_RevealSeconds) { finished = false; continue; }
                        started[index] = elapsed;
                        if (layer != null) layer.SetActive(true);
                        Sparkle(m_LayerRadii[index]);
                    }
                    var grow = (elapsed - started[index]) / m_LayerGrowSeconds;
                    if (layer != null) layer.transform.localScale = Vector3.one * LayerGrowScale(grow);
                    if (grow < 1f) finished = false;
                }
                yield return null;
            }

            // Sky: fades in far away and closes the last gaps; then back to the opaque material. A nested world's sky
            // sits just inside the outer world's (which is hidden during this reveal) and fades in the same way.
            m_Sky.localScale = Vector3.one * (m_FromPortal ? SkyMaxRadius * m_NestedSkyScale : SkyMaxRadius);
            m_SkyRenderer.sharedMaterial = m_SkyFadeMaterial;
            BubbleWorldData.SetAlpha(m_SkyFadeMaterial, 0f);
            m_Sky.gameObject.SetActive(true);
            for (var t = 0f; t < m_SkyFadeSeconds; t += Time.deltaTime)
            {
                BubbleWorldData.SetAlpha(m_SkyFadeMaterial, Ease(t / m_SkyFadeSeconds));
                yield return null;
            }
            m_SkyRenderer.sharedMaterial = m_SkyMaterial;
            m_Phase = Phase.Inside;
            if (m_Motion != null) m_Motion.SetStage(BubbleSplatMotion.Stage.Inside);
            m_InsideAt = Time.time;
            if (m_Home != null) { Destroy(m_Home); m_Home = null; }
            m_Routine = null;
            Revealed?.Invoke();
            LayerRevealed?.Invoke(m_Folder);
            PrepareNested();
        }

        (Vector3 center, Quaternion yaw) Target(Camera viewer)
        {
            var head = viewer.transform.position;
            var floor = ResolveOrigin() != null ? m_Origin.Origin.transform.position.y : 0f;
            var center = FinalCenter(head, floor, m_EyeHeight, m_AlignGroundToFloor);
            // The export header contains the measured capture height for every world.
            return (center, Quaternion.Euler(0f, GazeYaw(viewer.transform.forward), 0f));
        }

        /// <summary>World scale follows the bubble radius: the mini world exactly fills it.</summary>
        void SetRadius(float radius)
        {
            m_Radius = Mathf.Max(.001f, radius);
            m_WorldRoot.localScale = Vector3.one * (m_Radius / MiniWorldRadius);
            m_Membrane.localScale = Vector3.one * (m_Radius * 2f); // built-in sphere has radius 0.5
            BubbleWorldData.SetAlpha(m_MembraneMaterial, MembraneAlpha(m_Radius, m_MembraneAlpha));
        }

        void Sparkle(float layerRadius)
        {
            if (m_SparklesPerLayer <= 0 || m_Root == null) return;
            var center = m_Root.transform.position;
            // Bursts are sized for arm's length, so keep them within a few metres even for far shells.
            var distance = Mathf.Clamp(layerRadius, 1.2f, 4f);
            for (var i = 0; i < m_SparklesPerLayer; i++)
            {
                var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var height = UnityEngine.Random.Range(-.6f, .8f);
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Moodium.CandyWorld.CandyWorldManager.SpawnSoftSqueezeFeedback(center + direction * distance + Vector3.up * height);
            }
        }

        // ---------- Building ----------

        IEnumerator Build()
        {
            // Give the current world transition one frame to settle before resource work.
            yield return null;
            var points = Resources.Load<TextAsset>(BubbleWorldData.PointsResource(m_Folder));
            var colors = Resources.Load<Texture2D>(BubbleWorldData.RoundColorsResource(m_Folder));
            var panorama = Resources.Load<Texture2D>(BubbleWorldData.PanoramaResource(m_Folder));
            var film = Resources.Load<Texture2D>(BubbleWorldData.FilmResource);
            if (points == null || colors == null || panorama == null)
            {
                Debug.LogWarning($"[Bubble World] Missing points / point_colors / panorama under Resources/{m_Folder}.");
                m_BuildRoutine = null;
                yield break;
            }
            var bytes = points.bytes;
            if (!BubbleWorldData.TryReadSummary(bytes, BubbleWorldData.LayerCount, out _, out m_EyeHeight, out m_LayerRadii))
            {
                Debug.LogWarning("[Bubble World] points.bytes is not a valid MSPC v1 file.");
                m_BuildRoutine = null;
                yield break;
            }
            m_MiniLayerCount = MiniLayerCount(m_LayerRadii, MiniWorldRadius);

            m_PointMaterial = BubbleSplatMaterial.Create(colors, m_EyeHeight,
                m_Folder == BubbleWorldData.CandyFolder || m_Folder == BubbleWorldData.NestedCandyFolder);
            var skyTexture = SkyTexture(m_Folder, m_SkyStyle, panorama);
            m_SkyMaterial = BubbleWorldData.CreateUnlit("Bubble World Sky", skyTexture, false, CullMode.Back, Color.white);
            m_SkyMaterial.renderQueue = BackgroundRenderQueue + m_NatureWorldDepth;
            if (m_SkyMaterial.HasProperty("_ZWrite")) m_SkyMaterial.SetFloat("_ZWrite", 0f);
            m_SkyFadeMaterial = BubbleWorldData.CreateUnlit("Bubble World Sky Fade", skyTexture, true, CullMode.Back, Color.white);
            m_SkyFadeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var tint = m_Folder == BubbleWorldData.CandyFolder ? m_CandyFilmTint : m_NatureFilmTint;
            tint.a = m_MembraneAlpha;
            m_MembraneMaterial = BubbleWorldData.CreateUnlit("Bubble World Film", film, true, CullMode.Off, tint);
            m_MembraneMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            m_Root = new GameObject(m_Folder == BubbleWorldData.CandyFolder ? "Bubble World (Candy)" : "Bubble World (Nature)");
            m_Root.SetActive(false);
            m_WorldRoot = new GameObject("Scan (scaled)").transform;
            m_WorldRoot.SetParent(m_Root.transform, false);
            if (m_PointMaterial.shader.name == BubbleSplatMaterial.ShaderName)
            {
                m_Motion = m_Root.AddComponent<BubbleSplatMotion>();
                m_Motion.Initialize(m_PointMaterial, m_WorldRoot, ResolveViewer());
            }

            // Sky
            var skyMesh = Resources.Load<Mesh>(BubbleWorldData.MeshResource(m_Folder, BubbleWorldData.PanoramaMeshName));
            if (skyMesh == null)
            {
                skyMesh = BubbleWorldData.BuildPanoramaSphere();
                m_RuntimeMeshes.Add(skyMesh);
            }
            m_Sky = CreateRenderer("Sky", m_WorldRoot, skyMesh, m_SkyMaterial).transform;
            // Panorama assets are already level equirectangular images. Only the
            // scan points need the source capture-frame correction.
            m_Sky.localRotation = Quaternion.identity;
            m_SkyRenderer = m_Sky.GetComponent<MeshRenderer>();
            m_Sky.localScale = Vector3.one * (MiniWorldRadius * .99f);
            // None: the sky object keeps its timing logic but never draws.
            m_SkyRenderer.enabled = m_SkyStyle != SkyStyle.None;
            if (m_FixedSkyOrder)
            {
                // The sky sphere and every point shell are centred on the scan origin, so RealityKit's per-object
                // distance sort cannot tell them apart and sometimes drew the (transparent, while inside) sky over
                // the points. A native group draws the sky first, then the layers. PostPass, as for the UI.
                m_WorldSorting = m_WorldRoot.gameObject.AddComponent<VisionOSSortingGroup>();
                m_WorldSorting.DepthPass = VisionOSSortingGroup.DepthPassType.PostPass;
                m_WorldSorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting
                    { Renderer = m_Sky.gameObject, Order = SkySortOrder, ApplyToDescendants = false });
            }

            if (m_HandWake)
            {
                var wake = new GameObject("Bubble Hand Wake");
                // Under the scan root and in its sorting group, drawn after the point layers: a renderer outside
                // the group had no defined order against the PostPass group and could be hidden behind it.
                wake.transform.SetParent(m_WorldRoot, false);
                m_Wake = wake.AddComponent<BubbleHandWake>();
                m_Wake.Initialize(bytes, colors, m_EyeHeight, BubbleWorldData.IsCandy(m_Folder), m_WorldRoot, ResolveViewer());
                if (m_WorldSorting != null)
                    m_WorldSorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting
                        { Renderer = wake, Order = HandWakeSortOrder, ApplyToDescendants = false });
            }

            // The interaction shell belongs to the core warmup, so a user can
            // summon the miniature world while outer point layers keep streaming.
            var membrane = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            membrane.name = "Bubble Film";
            membrane.transform.SetParent(m_Root.transform, false);
            var membraneRenderer = membrane.GetComponent<MeshRenderer>();
            membraneRenderer.sharedMaterial = m_MembraneMaterial;
            membraneRenderer.shadowCastingMode = ShadowCastingMode.Off;
            membraneRenderer.receiveShadows = false;
            var button = membrane.AddComponent<MoodiumSpatialButton>();
            button.Configure(null, null, Touch);
            var hover = membrane.AddComponent<VisionOSHoverEffect>();
            hover.Type = VisionOSHoverEffect.EffectType.Highlight;
            hover.Color = new Color(.85f, 1f, .9f, 1f);
            m_Membrane = membrane.transform;
            m_MembraneCollider = membrane.GetComponent<Collider>();
            m_Audio = m_Root.AddComponent<AudioSource>();
            m_Audio.playOnAwake = false;
            m_Audio.spatialBlend = 1f;

            // Point layers (near → far). Baked meshes first; otherwise build them a few per frame.
            BubbleWorldData.Cloud cloud = null;
            var warmupLayers = WarmupLayerCount(m_MiniLayerCount, BubbleWorldData.LayerCount);
            for (var layer = 0; layer < BubbleWorldData.LayerCount; layer++)
            {
                var resourcePath = BubbleWorldData.MeshResource(m_Folder, BubbleWorldData.LayerMeshName(layer));
                Mesh mesh;
                // Stream core layers too: loading a dozen large meshes synchronously stalls input.
                var request = Resources.LoadAsync<Mesh>(resourcePath);
                yield return request;
                mesh = request.asset as Mesh;
                if (mesh == null)
                {
                    if (cloud == null && !BubbleWorldData.TryParse(bytes, out cloud))
                    {
                        Debug.LogWarning("[Bubble World] Could not parse points.bytes.");
                        StopAndClear();
                        yield break;
                    }
                    BubbleWorldData.LayerRange(layer, cloud.Count, BubbleWorldData.LayerCount, out var s, out var e);
                    mesh = BubbleWorldData.BuildLayerMesh(cloud, s, e, BubbleWorldData.LayerMeshName(layer), candy: BubbleWorldData.IsCandy(m_Folder));
                    m_RuntimeMeshes.Add(mesh);
                    if (layer % 2 == 1) yield return null;
                }
                var go = CreateRenderer(BubbleWorldData.LayerMeshName(layer), m_WorldRoot, mesh, m_PointMaterial);
                go.SetActive(layer < m_MiniLayerCount);
                if (m_WorldSorting != null)
                    m_WorldSorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting
                        { Renderer = go, Order = PointLayerSortOrder, ApplyToDescendants = false });
                m_Layers.Add(go);
                if (layer + 1 == warmupLayers)
                {
                    // The miniature bubble can now appear. Remaining layers stream
                    // in one per frame while it floats and approaches the viewer.
                    SetRadius(.001f);
                    m_CoreLayersReady = true;
                    yield return null;
                }
                else
                    yield return null;
            }
            var playObject=new GameObject("Bubble World Play");playObject.transform.SetParent(m_WorldRoot,false);
            m_Play=playObject.AddComponent<BubbleWorldPlay>();
            m_Play.Initialize(m_Folder,m_WorldRoot,ResolveViewer(),m_Layers,m_LayerRadii,m_PointMaterial,Sparkle,
                point=>(m_Home!=null&&m_Home.activeInHierarchy&&Vector3.Distance(point,m_Home.transform.position)<.15f)||
                    (m_Nested!=null&&m_Nested.m_Phase==Phase.Portal&&Vector3.Distance(point,m_Nested.m_Root.transform.position)<.15f));
            if(m_WorldSorting!=null)m_WorldSorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting{Renderer=playObject,Order=3,ApplyToDescendants=true});
            CacheQuietLayerPose();
            m_AllLayersReady = true;
            m_BuildRoutine = null;
            if (cloud != null) Debug.Log("[Bubble World] Built point meshes at runtime; bake them (Moodium ▸ Bubble World ▸ Bake Meshes) for device builds.");

            if (m_Phase == Phase.Armed) SetRadius(.001f);
            // ResetCached enables the root only when the bubble is actually summoned.
            if (m_PrepareNestedEarly) PrepareNested();
        }

        static GameObject CreateRenderer(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        // ---------- Input ----------

        bool FingertipTouching(Vector3 center, float radius)
        {
            if (m_Hands == null || !m_Hands.running)
            {
                m_Hands = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
                if (m_Hands == null) return false;
            }
            var origin = ResolveOrigin() != null ? m_Origin.Origin.transform : null;
            return TipTouching(m_Hands.leftHand, origin, center, radius) || TipTouching(m_Hands.rightHand, origin, center, radius);
        }

        bool TipTouching(XRHand hand, Transform origin, Vector3 center, float radius)
        {
            if (!hand.isTracked) return false;
            if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var pose)) return false;
            var tip = PalmPressGestureController.TransformTrackingPose(pose, origin).position;
            return IsTouching(tip, center, radius, m_TouchPadding);
        }

        XROrigin ResolveOrigin()
        {
            if (m_Origin == null) m_Origin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            return m_Origin;
        }

        Camera ResolveViewer()
        {
            if (m_Viewer == null) m_Viewer = Camera.main;
            return m_Viewer;
        }

        static void DestroyMaterial(ref Material material)
        {
            if (material != null) Destroy(material);
            material = null;
        }
    }
}
