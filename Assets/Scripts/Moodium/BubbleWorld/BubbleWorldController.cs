using System;
using System.Collections;
using System.Collections.Generic;
using Moodium.Flow;
using Moodium.Interaction;
using Unity.PolySpatial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Completion reward for Nature World and Candy World: a small bubble holding a tiny copy of that world's scan
    /// appears after the energy reaches 100%. Touching it (fingertip, or look + pinch) makes it fly to the viewer, swell around them
    /// and then spread the world's points out from near to far until the whole scan surrounds them.
    ///
    /// Rendering stays on the stock URP/Unlit shader: every point is a small quad whose colour comes from one texel
    /// of point_colors.png, and the sky behind the points is an inward-facing panorama sphere. Each world has its own
    /// folder (Resources/MoodiumBubbleWorld/ for Nature, Resources/MoodiumBubbleWorld/Candy/ for Candy) with a baked
    /// Meshes/ subfolder (Moodium ▸ Bubble World ▸ Bake Meshes); missing meshes are built at runtime from points.bytes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BubbleWorldController : MonoBehaviour
    {
        public enum Phase { Idle, Armed, Waiting, Floating, Approach, Envelop, Reveal, Done }

        const float CompletedProgress = .999f;
        /// <summary>Points within this radius (metres, scan scale) live inside the small bubble (4 of 20 layers for the shipped scan).</summary>
        public const float MiniWorldRadius = 10.5f;
        public const float SkyMaxRadius = 80f;
        public const int BackgroundRenderQueue = 1000;

        [Header("Appear")]
        [SerializeField, Min(0f)] float m_AppearDelay = 10f;
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

        public Phase CurrentPhase => m_Phase;

        Camera m_Viewer;
        string m_Folder = BubbleWorldData.NatureFolder;
        AudioClip m_TouchSound;
        Phase m_Phase = Phase.Idle;
        Coroutine m_Routine;

        // Scene objects
        GameObject m_Root;          // bubble centre; the scan centre (eye point) sits here
        Transform m_WorldRoot;      // scaled copy of the scan (k = radius / MiniWorldRadius)
        Transform m_Membrane;
        Collider m_MembraneCollider;
        Transform m_Sky;
        readonly List<GameObject> m_Layers = new();
        readonly List<Mesh> m_RuntimeMeshes = new();
        Material m_PointMaterial, m_SkyMaterial, m_SkyFadeMaterial, m_MembraneMaterial;
        MeshRenderer m_SkyRenderer;
        AudioSource m_Audio;

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
            StopAndClear();
            m_Viewer = viewer;
            m_Folder = string.IsNullOrEmpty(folder) ? BubbleWorldData.NatureFolder : folder;
            if (!HasData(m_Folder))
            {
                Debug.LogWarning($"[Bubble World] Missing Resources/{BubbleWorldData.PointsResource(m_Folder)}.bytes; the bubble world reward is off for this world.");
                return;
            }
            m_Phase = Phase.Armed;
        }

        public void SetProgress(float normalizedProgress)
        {
            if (m_Phase != Phase.Armed || !ShouldAppear(normalizedProgress)) return;
            m_Phase = Phase.Waiting;
            m_Routine = StartCoroutine(Run());
        }

        /// <summary>Same as touching the bubble (look + pinch, or debug).</summary>
        public void Touch()
        {
            if (m_Phase == Phase.Floating) m_TouchRequested = true;
        }

        public void StopAndClear()
        {
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
            m_TouchRequested = false;
            m_Phase = Phase.Idle;
            m_Layers.Clear();
            if (m_Root != null) Destroy(m_Root);
            m_Root = null; m_WorldRoot = null; m_Membrane = null; m_MembraneCollider = null; m_Sky = null; m_SkyRenderer = null; m_Audio = null;
            foreach (var mesh in m_RuntimeMeshes) if (mesh != null) Destroy(mesh);
            m_RuntimeMeshes.Clear();
            DestroyMaterial(ref m_PointMaterial);
            DestroyMaterial(ref m_SkyMaterial);
            DestroyMaterial(ref m_SkyFadeMaterial);
            DestroyMaterial(ref m_MembraneMaterial);
        }

        void OnDisable() => StopAndClear();

        // ---------- Sequence ----------

        bool m_TouchRequested;

        IEnumerator Run()
        {
            yield return new WaitForSeconds(m_AppearDelay);
            var viewer = ResolveViewer();
            if (viewer == null)
            {
                Debug.LogWarning("[Bubble World] No camera; cannot show the bubble.");
                m_Phase = Phase.Idle;
                yield break;
            }

            yield return Build();
            if (m_Root == null)
            {
                m_Phase = Phase.Idle;
                yield break;
            }

            // Appear: pop in at the lower left of the gaze.
            var head = viewer.transform.position;
            var yaw = Quaternion.Euler(0f, GazeYaw(viewer.transform.forward), 0f);
            var home = head + yaw * m_AppearOffset;
            m_Root.transform.SetPositionAndRotation(home, Quaternion.identity);
            m_WorldRoot.localRotation = yaw;
            m_Phase = Phase.Floating;
            Appeared?.Invoke();
            for (var t = 0f; t < .45f; t += Time.deltaTime)
            {
                SetRadius(m_BubbleRadius * LayerGrowScale(t / .45f) * Ease(t / .3f));
                yield return null;
            }
            SetRadius(m_BubbleRadius);

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

            // Touch: wobble, sparkle, chime. There is no way back from here.
            m_Phase = Phase.Approach;
            if (m_MembraneCollider != null) m_MembraneCollider.enabled = false; // never swallow other pinches while it grows
            Touched?.Invoke();
            Moodium.CandyWorld.CandyWorldManager.SpawnSoftSqueezeFeedback(m_Root.transform.position);
            if (m_TouchSound != null && m_Audio != null) m_Audio.PlayOneShot(m_TouchSound);
            for (var t = 0f; t < .3f; t += Time.deltaTime)
            {
                var wobble = 1f - Mathf.Sin(t / .3f * Mathf.PI * 2f) * Mathf.Exp(-t * 9f) * .12f;
                SetRadius(m_BubbleRadius * wobble);
                yield return null;
            }

            // Approach: fly to the head while growing to arm's length; the tiny world turns to face the viewer.
            var startPos = m_Root.transform.position;
            var startRot = m_WorldRoot.localRotation;
            for (var t = 0f; t < m_ApproachSeconds; t += Time.deltaTime)
            {
                var u = t / m_ApproachSeconds;
                var target = Target(viewer);
                m_Root.transform.position = Vector3.Lerp(startPos, target.center, Ease(u));
                m_WorldRoot.localRotation = Quaternion.Slerp(startRot, target.yaw, Ease(u));
                SetRadius(ApproachRadius(u, m_BubbleRadius, m_ApproachRadius));
                yield return null;
            }

            // Envelop: swell past the viewer to the size of the mini world. Lock the final pose now so walking or
            // turning does not drag the world along afterwards.
            m_Phase = Phase.Envelop;
            var final = Target(viewer);
            m_Root.transform.position = final.center;
            m_WorldRoot.localRotation = final.yaw;
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

            // Sky: fades in far away and closes the last gaps; then back to the opaque material.
            m_Sky.localScale = Vector3.one * SkyMaxRadius;
            BubbleWorldData.SetAlpha(m_SkyFadeMaterial, 0f);
            m_Sky.gameObject.SetActive(true);
            for (var t = 0f; t < m_SkyFadeSeconds; t += Time.deltaTime)
            {
                BubbleWorldData.SetAlpha(m_SkyFadeMaterial, Ease(t / m_SkyFadeSeconds));
                yield return null;
            }
            m_SkyRenderer.sharedMaterial = m_SkyMaterial;
            m_Phase = Phase.Done;
            m_Routine = null;
            Revealed?.Invoke();
        }

        (Vector3 center, Quaternion yaw) Target(Camera viewer)
        {
            var head = viewer.transform.position;
            var floor = ResolveOrigin() != null ? m_Origin.Origin.transform.position.y : 0f;
            var center = FinalCenter(head, floor, m_EyeHeight, m_AlignGroundToFloor);
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
            var points = Resources.Load<TextAsset>(BubbleWorldData.PointsResource(m_Folder));
            var colors = Resources.Load<Texture2D>(BubbleWorldData.ColorsResource(m_Folder));
            var panorama = Resources.Load<Texture2D>(BubbleWorldData.PanoramaResource(m_Folder));
            var film = Resources.Load<Texture2D>(BubbleWorldData.FilmResource);
            if (points == null || colors == null || panorama == null)
            {
                Debug.LogWarning($"[Bubble World] Missing points / point_colors / panorama under Resources/{m_Folder}.");
                yield break;
            }
            var bytes = points.bytes;
            if (!BubbleWorldData.TryReadSummary(bytes, BubbleWorldData.LayerCount, out _, out m_EyeHeight, out m_LayerRadii))
            {
                Debug.LogWarning("[Bubble World] points.bytes is not a valid MSPC v1 file.");
                yield break;
            }
            m_MiniLayerCount = MiniLayerCount(m_LayerRadii, MiniWorldRadius);

            m_PointMaterial = BubbleWorldData.CreateUnlit("Bubble World Points", colors, false, CullMode.Off, Color.white);
            m_SkyMaterial = BubbleWorldData.CreateUnlit("Bubble World Sky", panorama, false, CullMode.Back, Color.white);
            m_SkyMaterial.renderQueue = BackgroundRenderQueue;
            m_SkyFadeMaterial = BubbleWorldData.CreateUnlit("Bubble World Sky Fade", panorama, true, CullMode.Back, Color.white);
            var tint = m_Folder == BubbleWorldData.CandyFolder ? m_CandyFilmTint : m_NatureFilmTint;
            tint.a = m_MembraneAlpha;
            m_MembraneMaterial = BubbleWorldData.CreateUnlit("Bubble World Film", film, true, CullMode.Off, tint);

            m_Root = new GameObject(m_Folder == BubbleWorldData.CandyFolder ? "Bubble World (Candy)" : "Bubble World (Nature)");
            m_Root.SetActive(false);
            m_WorldRoot = new GameObject("Scan (scaled)").transform;
            m_WorldRoot.SetParent(m_Root.transform, false);

            // Sky
            var skyMesh = Resources.Load<Mesh>(BubbleWorldData.MeshResource(m_Folder, BubbleWorldData.PanoramaMeshName));
            if (skyMesh == null)
            {
                skyMesh = BubbleWorldData.BuildPanoramaSphere();
                m_RuntimeMeshes.Add(skyMesh);
            }
            m_Sky = CreateRenderer("Sky", m_WorldRoot, skyMesh, m_SkyMaterial).transform;
            m_SkyRenderer = m_Sky.GetComponent<MeshRenderer>();
            m_Sky.localScale = Vector3.one * (MiniWorldRadius * .99f);

            // Point layers (near → far). Baked meshes first; otherwise build them a few per frame.
            BubbleWorldData.Cloud cloud = null;
            for (var layer = 0; layer < BubbleWorldData.LayerCount; layer++)
            {
                var mesh = Resources.Load<Mesh>(BubbleWorldData.MeshResource(m_Folder, BubbleWorldData.LayerMeshName(layer)));
                if (mesh == null)
                {
                    if (cloud == null && !BubbleWorldData.TryParse(bytes, out cloud))
                    {
                        Debug.LogWarning("[Bubble World] Could not parse points.bytes.");
                        StopAndClear();
                        yield break;
                    }
                    BubbleWorldData.LayerRange(layer, cloud.Count, BubbleWorldData.LayerCount, out var s, out var e);
                    mesh = BubbleWorldData.BuildLayerMesh(cloud, s, e, BubbleWorldData.LayerMeshName(layer));
                    m_RuntimeMeshes.Add(mesh);
                    if (layer % 2 == 1) yield return null;
                }
                var go = CreateRenderer(BubbleWorldData.LayerMeshName(layer), m_WorldRoot, mesh, m_PointMaterial);
                go.SetActive(layer < m_MiniLayerCount);
                m_Layers.Add(go);
            }
            if (cloud != null) Debug.Log("[Bubble World] Built point meshes at runtime; bake them (Moodium ▸ Bubble World ▸ Bake Meshes) for device builds.");

            // Membrane: the built-in sphere (a mesh asset, safe on device) with its collider for look + pinch.
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

            SetRadius(.001f);
            m_Root.SetActive(true);
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
