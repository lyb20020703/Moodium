using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

namespace Moodium.Reality
{
    /// <summary>
    /// Adds a transparent visual-only layer to AR meshes. The source MeshRenderer,
    /// MeshFilter and MeshCollider are never modified, so tracking and physics stay isolated.
    /// </summary>
    public sealed class CandySpatialMeshTransformationController : MonoBehaviour
    {
        const int MaterialLevels = 8;
        const float ActivationProgress = 0.2f;
        static readonly int CandyProgressProperty = Shader.PropertyToID("_CandyProgress");
        static readonly int CandyOriginProperty = Shader.PropertyToID("_CandyOrigin");
        static readonly int TransparencyProperty = Shader.PropertyToID("_Transparency");
        static readonly int RimStrengthProperty = Shader.PropertyToID("_RimStrength");
        static readonly int IridescenceProperty = Shader.PropertyToID("_IridescenceStrength");
        static readonly int SparkleProperty = Shader.PropertyToID("_SparkleStrength");
        static readonly int ShaderTimeProperty = Shader.PropertyToID("_ShaderTime");

        sealed class OverlayEntry
        {
            public MeshFilter Source;
            public GameObject VisualObject;
            public MeshFilter VisualFilter;
            public MeshRenderer VisualRenderer;
        }

        [SerializeField] ARMeshManager m_MeshManager;
        [SerializeField] Material m_CandyMaterialTemplate;
        [SerializeField, Min(1f)] float m_MaxSpreadDistance = 5f;
        [SerializeField] Color m_CandyPink = new(1f, 0.58f, 0.78f, 1f);
        [SerializeField] Color m_CandyViolet = new(0.68f, 0.55f, 1f, 1f);
        [SerializeField, Range(0.05f, 0.3f)] float m_MaximumAlpha = 0.24f;

        readonly Dictionary<MeshFilter, OverlayEntry> m_Overlays = new();
        Material[] m_LevelMaterials;
        Vector3 m_Origin;
        float m_Progress;
        bool m_Running;

        public float Progress => m_Progress;
        public Vector3 Origin => m_Origin;

        public void Configure(ARMeshManager meshManager, Material candyMaterialTemplate)
        {
            m_MeshManager = meshManager;
            m_CandyMaterialTemplate = candyMaterialTemplate;
        }

        public void StartTransformation()
        {
            if (m_Running || m_MeshManager == null || m_CandyMaterialTemplate == null)
                return;
            m_Running = true;
            m_Progress = 0f;
            m_Origin = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            BuildLevelMaterials();
            m_MeshManager.meshesChanged += OnMeshesChanged;
            foreach (var filter in m_MeshManager.GetComponentsInChildren<MeshFilter>(true))
                Register(filter);
            DisableAllOverlays();
            Debug.Log("[Candy Spatial Mesh] Visual overlay ready but disabled at 0% Energy. Tracking mesh is untouched.");
        }

        public void StopTransformation()
        {
            if (!m_Running)
                return;
            m_Running = false;
            if (m_MeshManager != null)
                m_MeshManager.meshesChanged -= OnMeshesChanged;
            DestroyAllOverlays();
            DestroyLevelMaterials();
            Debug.Log("[Candy Spatial Mesh] Transparent visual overlays removed. Tracking mesh remained unchanged.");
        }

        public void SetOrigin(Vector3 worldPosition)
        {
            m_Origin = worldPosition;
            UpdateMaterialGlobals();
            if (m_Progress >= ActivationProgress)
                ApplyToAllMeshes();
        }

        public void SetProgress(float energy, float normalizedProgress)
        {
            m_Progress = Mathf.Clamp01(normalizedProgress);
            UpdateMaterialGlobals();
            if (m_Progress < ActivationProgress)
                DisableAllOverlays();
            else
                ApplyToAllMeshes();
            Debug.Log($"[Candy Spatial Mesh] Transparent overlay progress={m_Progress:0.00}, origin={m_Origin}");
        }

        void OnDestroy() => StopTransformation();

        void Update()
        {
            if (!m_Running || m_LevelMaterials == null)
                return;
            foreach (var material in m_LevelMaterials)
                if (material != null && material.HasProperty(ShaderTimeProperty))
                    material.SetFloat(ShaderTimeProperty, Time.time);
        }

        void OnMeshesChanged(ARMeshesChangedEventArgs args)
        {
            foreach (var filter in args.added) Register(filter);
            foreach (var filter in args.updated) Register(filter);
            foreach (var filter in args.removed) Unregister(filter);
            if (m_Progress >= ActivationProgress)
                ApplyToAllMeshes();
        }

        void Register(MeshFilter source)
        {
            if (source == null || source.sharedMesh == null)
                return;
            if (m_Overlays.TryGetValue(source, out var existing))
            {
                existing.VisualFilter.sharedMesh = source.sharedMesh;
                return;
            }

            var visual = new GameObject("Moodium Candy Visual Overlay");
            visual.transform.SetParent(source.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var visualFilter = visual.AddComponent<MeshFilter>();
            visualFilter.sharedMesh = source.sharedMesh;
            var visualRenderer = visual.AddComponent<MeshRenderer>();
            visualRenderer.shadowCastingMode = ShadowCastingMode.Off;
            visualRenderer.receiveShadows = false;
            visualRenderer.lightProbeUsage = LightProbeUsage.Off;
            visualRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            visualRenderer.enabled = false;
            m_Overlays.Add(source, new OverlayEntry
            {
                Source = source,
                VisualObject = visual,
                VisualFilter = visualFilter,
                VisualRenderer = visualRenderer
            });
        }

        void Unregister(MeshFilter source)
        {
            if (source == null || !m_Overlays.Remove(source, out var entry))
                return;
            if (entry.VisualObject != null)
                Destroy(entry.VisualObject);
        }

        void ApplyToAllMeshes()
        {
            if (!m_Running || m_LevelMaterials == null || m_Progress < ActivationProgress)
                return;
            var effectProgress = Mathf.InverseLerp(ActivationProgress, 1f, m_Progress);
            var stale = new List<MeshFilter>();
            foreach (var pair in m_Overlays)
            {
                var entry = pair.Value;
                if (entry.Source == null || entry.VisualRenderer == null)
                {
                    stale.Add(pair.Key);
                    continue;
                }
                entry.VisualFilter.sharedMesh = entry.Source.sharedMesh;
                var distance01 = Mathf.Clamp01(
                    Vector3.Distance(entry.Source.transform.position, m_Origin) / m_MaxSpreadDistance);
                var start = distance01 * 0.65f;
                var localProgress = Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(start, start + 0.35f, effectProgress));
                var level = Mathf.Clamp(
                    Mathf.RoundToInt(localProgress * (MaterialLevels - 1)), 0, MaterialLevels - 1);
                entry.VisualRenderer.sharedMaterial = m_LevelMaterials[level];
                entry.VisualRenderer.enabled = localProgress > 0.001f;
            }
            foreach (var source in stale)
                m_Overlays.Remove(source);
        }

        void DisableAllOverlays()
        {
            foreach (var entry in m_Overlays.Values)
                if (entry.VisualRenderer != null)
                    entry.VisualRenderer.enabled = false;
        }

        void BuildLevelMaterials()
        {
            DestroyLevelMaterials();
            var stableShader = Shader.Find("Universal Render Pipeline/Lit");
            m_LevelMaterials = new Material[MaterialLevels];
            for (var i = 0; i < MaterialLevels; i++)
            {
                var progress = i / (float)(MaterialLevels - 1);
                // Prefer the existing transparent Candy shader for Fresnel/iridescence.
                // It is still rendered on a separate visual-only layer; AR mesh data and
                // colliders are never replaced. URP/Lit remains the safe fallback.
                var material = m_CandyMaterialTemplate != null
                    ? new Material(m_CandyMaterialTemplate)
                    : new Material(stableShader);
                material.name = $"Candy Transparent Overlay {i} (Runtime)";
                material.hideFlags = HideFlags.DontSave;
                ConfigureTransparentMaterial(material);
                var color = Color.Lerp(m_CandyPink, m_CandyViolet, progress);
                color.a = Mathf.Lerp(0.08f, m_MaximumAlpha, progress);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Lerp(0.78f, 0.96f, progress));
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Lerp(0.01f, 0.07f, progress));
                if (material.HasProperty(TransparencyProperty))
                    material.SetFloat(TransparencyProperty, Mathf.Lerp(0.07f, m_MaximumAlpha, progress));
                if (material.HasProperty(CandyProgressProperty))
                    material.SetFloat(CandyProgressProperty, Mathf.Lerp(ActivationProgress, 1f, progress));
                if (material.HasProperty(RimStrengthProperty))
                    material.SetFloat(RimStrengthProperty, Mathf.Lerp(0.45f, 2.15f, progress));
                if (material.HasProperty(IridescenceProperty))
                    material.SetFloat(IridescenceProperty, Mathf.Lerp(0f, 0.65f, progress));
                if (material.HasProperty(SparkleProperty))
                    material.SetFloat(SparkleProperty, Mathf.Lerp(0f, 0.48f, progress));
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.Lerp(m_CandyPink, m_CandyViolet, progress) * (0.015f + progress * 0.04f));
                }
                m_LevelMaterials[i] = material;
            }
            UpdateMaterialGlobals();
        }

        void UpdateMaterialGlobals()
        {
            if (m_LevelMaterials == null)
                return;
            foreach (var material in m_LevelMaterials)
            {
                if (material == null)
                    continue;
                if (material.HasProperty(CandyOriginProperty))
                    material.SetVector(CandyOriginProperty, m_Origin);
            }
        }

        static void ConfigureTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        }

        void DestroyAllOverlays()
        {
            foreach (var entry in m_Overlays.Values)
                if (entry.VisualObject != null)
                    Destroy(entry.VisualObject);
            m_Overlays.Clear();
        }

        void DestroyLevelMaterials()
        {
            if (m_LevelMaterials == null)
                return;
            foreach (var material in m_LevelMaterials)
                if (material != null)
                    Destroy(material);
            m_LevelMaterials = null;
        }
    }
}
