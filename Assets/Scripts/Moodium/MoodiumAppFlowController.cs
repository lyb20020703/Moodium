using System.Collections;
using System.Collections.Generic;
using Samples.PolySpatial.SwiftUI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.PolySpatial;
using Moodium.Audio;
using Moodium.CandyWorld;
using Moodium.Flow.ObjectPicker;
using Moodium.Reality;

namespace Moodium.Flow
{
    public sealed class MoodiumAppFlowController : MonoBehaviour
    {
        [Header("Existing scene modules")]
        [SerializeField] ARTrackedObjectManager m_TrackedObjectManager;
        [SerializeField] ARMeshManager m_SpatialMeshManager;
        [SerializeField] MeshSwiftUIDriver m_SpatialPhysicsDriver;

        [Header("Creative Space worlds")]
        [SerializeField] MoodiumWorldDefinition[] m_Worlds;
        [SerializeField] MoodiumWorldDatabase m_WorldDatabase;
        [SerializeField] GameObject m_WorldCardPrefab;
        [SerializeField, Min(0.5f)] float m_SpawnDistance = 1f;

        [Header("Reality Enhancement world models")]
        [SerializeField] GameObject m_RealityCandyWorldModelPrefab;
        [SerializeField] GameObject m_RealityNatureWorldModelPrefab;
        [SerializeField] GameObject m_RealityFallbackWorldModelPrefab;

        [Header("Creative Space initial object scale")]
        [Tooltip("Creative Space only. Each value multiplies the source prefab scale when it is spawned.")]
        [SerializeField] CreativePrefabScaleConfig[] m_CreativePrefabScaleConfigs;

        [Header("Prototype UI")]
        [SerializeField] TMP_FontAsset m_FontAsset;
        [SerializeField] TMP_FontAsset m_ChineseFontAsset;
        [SerializeField] GameObject m_GlassPanelPrefab;
        [SerializeField] Material m_ObjectPickerGlassMaterial;
        [SerializeField] GameObject m_SpatialTablePrefab;
        [SerializeField] GameObject m_CandyTransformationPreviewPrefab;
        [SerializeField] Material m_CandyTransformationMaterial;
        [SerializeField] Sprite m_WristHudRoundedSprite;
        [SerializeField] Sprite m_WristHudCircleSprite;
        [SerializeField] GameObject m_CandyFrostPrefab;
        [SerializeField] Sprite m_CreativeSpaceModeArtwork;
        [SerializeField] Sprite m_RealityEnhancementModeArtwork;

        MoodiumMode m_CurrentMode;
        Transform m_UiRoot;
        GameObject m_ModePanel;
        GameObject m_WorldSelectionPanel;
        GameObject m_ObjectWorldSelectionPanel;
        GameObject m_NoObjectPanel;
        GameObject m_InteractionPanel;
        GameObject m_ObjectPanel;
        TMP_Text m_SpawnObjectsButtonLabel;
        MoodiumNoObjectModeController m_NoObjectModeController;
        MoodiumWorldDefinition m_SelectedWorld;
        MoodiumWorldNavigationState m_WorldNavigationState;
        TMP_Text m_WorldEditTitle;
        MoodiumObjectPickerController m_ObjectPicker;
        WorldCarouselController m_WorldCarousel;
        WorldCarouselController m_ObjectWorldCarousel;
        CandyWorldManager m_CandyWorldManager;
        RealitySpatialTableManager m_RealitySpatialTableManager;
        RealityEnhancementFlowController m_RealityEnhancementFlow;
        GameObject m_CandyTransformationPreviewInstance;
        Camera m_MainCamera;
        bool m_LoggedMissingFont;

        public MoodiumMode CurrentMode => m_CurrentMode;

        public void PrepareForOpening()
        {
            MoodiumAudioManager.StopBackgroundMusic();
            SetExistingModules(false, false);
            HideAllPanels();
        }

        void Awake()
        {
            HideSpatialMeshVisualization();
            if (m_TrackedObjectManager != null)
                m_TrackedObjectManager.enabled = false;
            if (m_SpatialPhysicsDriver != null)
                m_SpatialPhysicsDriver.enabled = false;
        }

        IEnumerator Start()
        {
            if (GetComponent<MoodiumSpatialInteractionController>() == null)
                gameObject.AddComponent<MoodiumSpatialInteractionController>();

            m_NoObjectModeController = GetComponent<MoodiumNoObjectModeController>();
            if (m_NoObjectModeController == null)
                m_NoObjectModeController = gameObject.AddComponent<MoodiumNoObjectModeController>();

            if (m_TrackedObjectManager != null)
                m_TrackedObjectManager.enabled = false;

            if (m_SpatialPhysicsDriver != null)
                m_SpatialPhysicsDriver.enabled = false;

            var timeout = Time.realtimeSinceStartup + 10f;
            while (Camera.main == null && Time.realtimeSinceStartup < timeout)
                yield return null;

            m_MainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (m_MainCamera == null)
            {
                Debug.LogError("[Moodium Flow] Main Camera was not found.");
                yield break;
            }

            BuildFlowUI();
            ShowModeSelection();
        }

        void BuildFlowUI()
        {
            m_UiRoot = new GameObject("Moodium Flow UI").transform;

            BuildModeSelection();
            BuildWorldSelectionPanel();
            BuildObjectWorldSelectionPanel();
            BuildNoObjectPanel();
            BuildInteractionPanel();
            BuildObjectPanel();
        }

        void BuildObjectWorldSelectionPanel()
        {
            m_ObjectWorldSelectionPanel = CreatePanelRoot("Moodium Object World Selection", -0.08f, false);
            CreateText(
                m_ObjectWorldSelectionPanel.transform,
                "Choose a tracked-object world",
                new Vector3(0f, 0.36f, 0f),
                3.1f,
                new Vector2(7f, 0.8f));

            var carouselObject = new GameObject("Reality Enhancement World Carousel");
            carouselObject.transform.SetParent(m_ObjectWorldSelectionPanel.transform, false);
            carouselObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            m_ObjectWorldCarousel = carouselObject.AddComponent<WorldCarouselController>();
            m_ObjectWorldCarousel.ConfigureModels(
                m_WorldDatabase,
                m_RealityCandyWorldModelPrefab,
                m_RealityNatureWorldModelPrefab,
                m_RealityFallbackWorldModelPrefab,
                m_FontAsset,
                SelectObjectWorld,
                PreviewWorldMusic);

            CreateButton(
                m_ObjectWorldSelectionPanel.transform,
                "Back",
                new Vector3(0f, -0.36f, 0f),
                new Vector2(0.28f, 0.09f),
                ShowModeSelection);
            m_ObjectWorldSelectionPanel.SetActive(false);
        }

        void BuildWorldSelectionPanel()
        {
            m_WorldSelectionPanel = CreatePanelRoot("Moodium World Selection", -0.08f, false);
            CreateText(
                m_WorldSelectionPanel.transform,
                "Explore sensory worlds",
                new Vector3(0f, 0.36f, 0f),
                3.1f,
                new Vector2(7f, 0.8f));

            var carouselObject = new GameObject("World Carousel");
            carouselObject.transform.SetParent(m_WorldSelectionPanel.transform, false);
            carouselObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            m_WorldCarousel = carouselObject.AddComponent<WorldCarouselController>();
            m_WorldCarousel.Configure(m_WorldDatabase, m_WorldCardPrefab, SelectWorld, PreviewWorldMusic);

            CreateButton(
                m_WorldSelectionPanel.transform,
                "Back",
                new Vector3(0f, -0.36f, 0f),
                new Vector2(0.28f, 0.09f),
                ShowModeSelection);
            m_WorldSelectionPanel.SetActive(false);
        }

        void BuildModeSelection()
        {
            var spaceCreationSprite = Resources.Load<Sprite>("UI/SpaceCreationUI");
            var augmentedRealitySprite = Resources.Load<Sprite>("UI/AugmentedRealityUI");
            if (spaceCreationSprite == null)
                spaceCreationSprite = m_CreativeSpaceModeArtwork;
            if (augmentedRealitySprite == null)
                augmentedRealitySprite = m_RealityEnhancementModeArtwork;
            Debug.Log(
                $"[Moodium UI] spaceCreationSprite is null={spaceCreationSprite == null}; " +
                $"augmentedRealitySprite is null={augmentedRealitySprite == null}; " +
                $"platform={Application.platform}");

            m_ModePanel = CreatePanelRoot("Moodium Mode Selection", -0.05f, false);
            CreateText(
                m_ModePanel.transform,
                "Choose your Moodium space",
                new Vector3(0f, 0.3f, 0f),
                2.7f,
                new Vector2(7f, 0.8f));

            CreateModeCard(
                m_ModePanel.transform,
                "空间创造模式",
                "Creative Space Mode",
                "打造属于你的幻想世界，释放无限创意。",
                spaceCreationSprite,
                new Vector3(-0.225f, 0f, 0f),
                EnterNoObjectMode);
            CreateModeCard(
                m_ModePanel.transform,
                "现实增强模式",
                "Reality Enhancement Mode",
                "赋予真实世界魔力，开启感官新体验。",
                augmentedRealitySprite,
                new Vector3(0.225f, 0f, 0f),
                EnterObjectMode);
            m_ModePanel.SetActive(false);
        }

        void BuildNoObjectPanel()
        {
            // Edit Mode uses real 3D picker objects rather than a dark panel or text list.
            m_NoObjectPanel = CreatePanelRoot("Moodium Model Library", -0.2f, false);
            m_WorldEditTitle = CreateText(
                m_NoObjectPanel.transform,
                "Candy World · Choose an object",
                new Vector3(0f, 0.4f, 0f),
                2.6f,
                new Vector2(7f, 0.8f));

            var pickerObject = new GameObject("Candy World 3D Object Picker");
            pickerObject.transform.SetParent(m_NoObjectPanel.transform, false);
            m_ObjectPicker = pickerObject.AddComponent<MoodiumObjectPickerController>();
            m_ObjectPicker.Configure(m_MainCamera, GetPickerGlassMaterial(), SpawnFreeObject);

            CreateButton(
                m_NoObjectPanel.transform,
                "Finish Editing",
                new Vector3(-0.23f, -0.30f, 0f),
                new Vector2(0.42f, 0.09f),
                EnterNoObjectInteractionMode);
            CreateButton(
                m_NoObjectPanel.transform,
                "Clear Scene",
                new Vector3(0.18f, -0.30f, 0f),
                new Vector2(0.36f, 0.09f),
                ClearNoObjectScene);
            CreateButton(
                m_NoObjectPanel.transform,
                "Worlds",
                new Vector3(0f, -0.41f, 0f),
                new Vector2(0.28f, 0.09f),
                ShowWorldSelection);
            m_NoObjectPanel.SetActive(false);
        }

        void RebuildWorldModelButtons()
        {
            if (m_ObjectPicker == null || m_SelectedWorld == null)
                return;

            m_WorldEditTitle.text = $"{m_SelectedWorld.DisplayName} - Edit Mode";
            m_ObjectPicker.Configure(m_MainCamera, GetPickerGlassMaterial(), SpawnFreeObject);
            m_ObjectPicker.Build(m_SelectedWorld.Prefabs);
        }

        void BuildInteractionPanel()
        {
            m_InteractionPanel = CreatePanelRoot("Moodium Interaction Mode", -0.2f, false);
            CreateText(
                m_InteractionPanel.transform,
                "Candy World",
                new Vector3(0f, 0.105f, 0f),
                2.35f,
                new Vector2(5.8f, 0.65f));

            var toolbar = new GameObject("Candy World Floating Toolbar").transform;
            toolbar.SetParent(m_InteractionPanel.transform, false);
            toolbar.localPosition = new Vector3(0f, -0.005f, 0f);
            CreateGlassVisual(toolbar, "Toolbar Glass Capsule", new Vector2(0.7f, 0.15f), 2);

            CreateButton(
                toolbar,
                "Edit",
                new Vector3(-0.235f, 0f, -0.012f),
                new Vector2(0.17f, 0.075f),
                EnterNoObjectEditMode);
            var spawnButton = CreateButton(
                toolbar,
                "Spawn Objects · OFF",
                new Vector3(0f, 0f, -0.012f),
                new Vector2(0.25f, 0.075f),
                ToggleCreativeSpaceSpawning);
            spawnButton.Label.fontSize = 2.05f;
            m_SpawnObjectsButtonLabel = spawnButton.Label;
            CreateButton(
                toolbar,
                "← Back",
                new Vector3(0.235f, 0f, -0.012f),
                new Vector2(0.17f, 0.075f),
                ShowWorldSelection);
            m_InteractionPanel.SetActive(false);
        }

        void BuildObjectPanel()
        {
            // Reality mode only needs a persistent, unobtrusive bottom Back capsule.
            m_ObjectPanel = CreatePanelRoot(
                "Moodium Reality Enhancement Mode",
                -0.28f,
                addGlassBackground: false,
                addWindowHandle: false);
            CreateButton(
                m_ObjectPanel.transform,
                "Back",
                new Vector3(0f, 0.834f, 0f),
                new Vector2(0.18f, 0.07f),
                ShowModeSelection);
            m_ObjectPanel.SetActive(false);
        }

        public void ShowModeSelection()
        {
            ExitCreativeSpaceIfNeeded();
            StopCandyWorldExperience();
            MoodiumAudioManager.StopBackgroundMusic();
            SetMode(MoodiumMode.ModeSelection);
            SetExistingModules(false, false);
            HideAllPanels();
            PlacePanel(m_ModePanel, -0.05f);
            m_ModePanel.SetActive(true);
            Debug.Log("[Moodium Flow] Mode selection shown.");
        }

        void EnterNoObjectMode()
        {
            SetMode(MoodiumMode.NoObject);
            SetExistingModules(false, false);
            ShowWorldSelection();
            Debug.Log("[Moodium Flow] Entered Creative Space Mode / 空间创造模式.");
        }

        void ShowWorldSelection()
        {
            StopCandyWorldExperience();
            MoodiumAudioManager.StopBackgroundMusic();
            SetCreativeSpacePhysicsEnabled(false);
            SetMode(MoodiumMode.NoObject);
            m_WorldNavigationState = MoodiumWorldNavigationState.WorldSelection;
            HideAllPanels();
            PlacePanel(m_WorldSelectionPanel, -0.08f);
            m_WorldSelectionPanel.SetActive(true);
            if (m_WorldCarousel != null)
                m_WorldCarousel.ResetToFirstAvailable();
            Debug.Log("[Moodium World] World selection shown.");
        }

        void SelectWorld(MoodiumWorldDefinition world)
        {
            if (world == null)
                return;
            if (!world.IsAvailable)
            {
                Debug.Log($"[Moodium World] {world.DisplayName} is coming soon.");
                return;
            }

            m_SelectedWorld = world;
            MoodiumAudioManager.PlayBackgroundMusic(world.BackgroundMusic);
            m_NoObjectModeController.SelectWorld(world);
            RebuildWorldModelButtons();
            EnterNoObjectEditMode();
        }

        void PreviewWorldMusic(MoodiumWorldDefinition world)
        {
            if (world != null)
                MoodiumAudioManager.PlayBackgroundMusic(world.BackgroundMusic);
        }

        void EnterNoObjectEditMode()
        {
            if (m_SelectedWorld == null)
            {
                ShowWorldSelection();
                return;
            }

            SetMode(MoodiumMode.NoObject);
            m_WorldNavigationState = MoodiumWorldNavigationState.WorldEdit;
            SetCreativeSpacePhysicsEnabled(false);
            m_NoObjectModeController.EnterEditMode();
            HideAllPanels();
            PlacePanel(m_NoObjectPanel, -0.2f);
            m_NoObjectPanel.SetActive(true);
            m_ObjectPicker?.PlaceInFront();
            Debug.Log("[Moodium Flow] Creative Space Mode: Edit Mode.");
        }

        void EnterNoObjectInteractionMode()
        {
            if (m_SelectedWorld == null)
            {
                ShowWorldSelection();
                return;
            }

            SetMode(MoodiumMode.NoObject);
            m_WorldNavigationState = MoodiumWorldNavigationState.WorldInteraction;
            m_NoObjectModeController.EnterInteractionMode();
            SetCreativeSpacePhysicsEnabled(true);
            HideAllPanels();
            PlacePanel(m_InteractionPanel, -0.2f);
            m_InteractionPanel.SetActive(true);
            Debug.Log("[Moodium Flow] Creative Space Mode: Interaction Mode.");
        }

        void ClearNoObjectScene()
        {
            m_NoObjectModeController.ClearScene();
        }

        void EnterObjectMode()
        {
            ExitCreativeSpaceIfNeeded();
            SetMode(MoodiumMode.ObjectTracking);
            SetExistingModules(false, false);
            HideAllPanels();
            PlacePanel(m_ObjectWorldSelectionPanel, -0.08f);
            m_ObjectWorldSelectionPanel.SetActive(true);
            if (m_ObjectWorldCarousel != null)
                m_ObjectWorldCarousel.ResetToFirstAvailable();
            Debug.Log("[Moodium Flow] Reality Enhancement Mode world selection shown.");
        }

        void SelectObjectWorld(MoodiumWorldDefinition world)
        {
            if (world == null || !world.IsAvailable)
                return;
            if (!world.WorldId.Contains("candy", System.StringComparison.OrdinalIgnoreCase) &&
                !world.DisplayName.Contains("candy", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[Moodium Object World] {world.DisplayName} is not implemented yet.");
                return;
            }

            m_SelectedWorld = world;
            MoodiumAudioManager.PlayBackgroundMusic(world.BackgroundMusic);
            EnsureCandyWorldManager();
            m_CandyWorldManager.StartExperience(world);
            EnsureRealitySpatialTableManager();
            m_RealitySpatialTableManager.ShowTable(m_MainCamera);
            EnsureRealityEnhancementFlow();
            m_RealityEnhancementFlow.StartFlow();
            SetMode(MoodiumMode.ObjectTracking);
            SetExistingModules(true, false);
            HideAllPanels();
            PlacePanel(m_ObjectPanel, -0.28f, 1f);
            m_ObjectPanel.SetActive(true);
            Debug.Log("[Moodium Flow] Reality Enhancement Mode: Candy World. Tissue tracking enabled.");
        }

        void SetCreativeSpacePhysicsEnabled(bool enabled)
        {
            if (m_SpatialPhysicsDriver == null)
                return;
            m_SpatialPhysicsDriver.SetEmbeddedMoodiumMode(enabled);
            m_SpatialPhysicsDriver.enabled = enabled;
            if (!enabled)
                m_SpatialPhysicsDriver.SetSpawningObjects(false);
            UpdateSpawnObjectsLabel();
        }

        void ToggleCreativeSpaceSpawning()
        {
            if (m_SpatialPhysicsDriver == null)
                return;
            if (!m_SpatialPhysicsDriver.enabled)
                SetCreativeSpacePhysicsEnabled(true);
            m_SpatialPhysicsDriver.SetSpawningObjects(!m_SpatialPhysicsDriver.IsSpawningObjects);
            UpdateSpawnObjectsLabel();
        }

        void UpdateSpawnObjectsLabel()
        {
            if (m_SpawnObjectsButtonLabel != null)
                m_SpawnObjectsButtonLabel.text = m_SpatialPhysicsDriver != null && m_SpatialPhysicsDriver.IsSpawningObjects
                    ? "Spawn Objects · ON"
                    : "Spawn Objects · OFF";
        }

        void EnsureCandyWorldManager()
        {
            if (m_CandyWorldManager != null)
                return;
            var managerObject = new GameObject("Candy World Reality Enhancement");
            managerObject.transform.SetParent(transform, false);
            m_CandyWorldManager = managerObject.AddComponent<CandyWorldManager>();
            m_CandyWorldManager.Configure(
                m_MainCamera,
                m_GlassPanelPrefab,
                m_FontAsset,
                m_SelectedWorld,
                m_SpatialMeshManager,
                m_CandyTransformationMaterial,
                m_WristHudRoundedSprite,
                m_WristHudCircleSprite,
                m_CandyFrostPrefab);
        }

        void StopCandyWorldExperience()
        {
            m_RealityEnhancementFlow?.StopFlow();
            if (m_CandyWorldManager != null)
                m_CandyWorldManager.StopExperience();
            m_RealitySpatialTableManager?.HideTable();
            if (m_CandyTransformationPreviewInstance != null)
            {
                Destroy(m_CandyTransformationPreviewInstance);
                m_CandyTransformationPreviewInstance = null;
            }
        }

        void ShowCandyTransformationPreview()
        {
            if (m_CandyTransformationPreviewInstance != null || m_CandyTransformationPreviewPrefab == null)
                return;
            m_CandyTransformationPreviewInstance = Instantiate(m_CandyTransformationPreviewPrefab);
            m_CandyTransformationPreviewInstance.name = "Candy Material Transformation Vision Preview";
            m_CandyTransformationPreviewInstance.SetActive(true);
        }

        void EnsureRealitySpatialTableManager()
        {
            if (m_RealitySpatialTableManager == null)
                m_RealitySpatialTableManager = GetComponent<RealitySpatialTableManager>();
            if (m_RealitySpatialTableManager == null)
                m_RealitySpatialTableManager = gameObject.AddComponent<RealitySpatialTableManager>();
            m_RealitySpatialTableManager.Configure(m_SpatialTablePrefab);
        }

        void EnsureRealityEnhancementFlow()
        {
            if (m_RealityEnhancementFlow == null)
                m_RealityEnhancementFlow = GetComponent<RealityEnhancementFlowController>();
            if (m_RealityEnhancementFlow == null)
                m_RealityEnhancementFlow = gameObject.AddComponent<RealityEnhancementFlowController>();
            var spawner = FindFirstObjectByType<TissueObjectTrackingSpawner>(FindObjectsInactive.Include);
            m_RealityEnhancementFlow.Configure(
                spawner,
                m_MainCamera,
                m_GlassPanelPrefab,
                m_FontAsset,
                m_ChineseFontAsset);
        }

        void SetExistingModules(bool objectTracking, bool spatialPhysics)
        {
            // Spatial mesh data and colliders remain active; only the sample's grey
            // visualization renderer is hidden for the finished Moodium experience.
            HideSpatialMeshVisualization();
            if (m_TrackedObjectManager != null)
                m_TrackedObjectManager.enabled = objectTracking;
            if (m_SpatialPhysicsDriver != null)
            {
                if (spatialPhysics)
                    m_SpatialPhysicsDriver.SetEmbeddedMoodiumMode(false);
                m_SpatialPhysicsDriver.enabled = spatialPhysics;
                if (!spatialPhysics)
                    m_SpatialPhysicsDriver.SetSpawningObjects(false);
            }
        }

        void SetMode(MoodiumMode mode)
        {
            m_CurrentMode = mode;
        }

        void ExitCreativeSpaceIfNeeded()
        {
            if (m_CurrentMode != MoodiumMode.NoObject || m_NoObjectModeController == null)
                return;

            SetCreativeSpacePhysicsEnabled(false);
            var fallingCount = m_SpatialPhysicsDriver != null
                ? m_SpatialPhysicsDriver.SpawnedObjectCount
                : 0;
            m_SpatialPhysicsDriver?.ClearSpawnedObjects();
            m_NoObjectModeController.ClearScene();
            var registryCount = MoodiumRuntimeObjectRegistry.ClearCreativeObjects();
            Debug.Log(
                $"[Moodium Flow] Exited Creative Space Mode; cleared {fallingCount} Spawn Objects instances. " +
                $"Cleared all user-placed objects and {registryCount} registered runtime objects/effects.");
        }

        void HideAllPanels()
        {
            if (m_ModePanel != null)
                m_ModePanel.SetActive(false);
            if (m_WorldSelectionPanel != null)
                m_WorldSelectionPanel.SetActive(false);
            if (m_ObjectWorldSelectionPanel != null)
                m_ObjectWorldSelectionPanel.SetActive(false);
            if (m_NoObjectPanel != null)
                m_NoObjectPanel.SetActive(false);
            if (m_InteractionPanel != null)
                m_InteractionPanel.SetActive(false);
            if (m_ObjectPanel != null)
                m_ObjectPanel.SetActive(false);
        }

        void SpawnFreeObject(GameObject prefab)
        {
            if (prefab == null || m_MainCamera == null)
                return;

            var cameraTransform = m_MainCamera.transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = cameraTransform.forward.normalized;

            var position = cameraTransform.position + forward * m_SpawnDistance;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var instance = Instantiate(prefab, position, rotation);
            instance.name = $"{prefab.name} (Free Mode)";
            var sourceScale = instance.transform.localScale;
            var scaleMultiplier = GetCreativeInitialScaleMultiplier(prefab);
            instance.transform.localScale = sourceScale * scaleMultiplier;
            MoodiumRuntimeObjectSetup.Configure(
                instance,
                allowMove: true,
                allowScale: true,
                disablePhysics: true);
            m_NoObjectModeController.Register(instance);

            Debug.Log(
                $"[Moodium Flow] Free object spawned: {prefab.name}, " +
                $"position={position}, prefabScale={sourceScale}, " +
                $"initialScaleMultiplier={scaleMultiplier:0.###}, scale={instance.transform.localScale}");
        }

        float GetCreativeInitialScaleMultiplier(GameObject prefab)
        {
            if (m_CreativePrefabScaleConfigs != null)
            {
                foreach (var config in m_CreativePrefabScaleConfigs)
                {
                    if (config.Prefab == prefab)
                        return config.InitialScaleMultiplier;
                }
            }

            // Safe defaults apply only to Creative Space instances. Inspector entries override them.
            return prefab.name switch
            {
                "HeartPrefab" => 0.5f,
                "ChocolatePrefab" => 0.7f,
                "CookiePrefab" => 0.7f,
                "StarPrefab" => 0.7f,
                "Macaron" => 0.7f,
                _ => 1f
            };
        }

        GameObject CreatePanelRoot(
            string panelName,
            float verticalOffset,
            bool addGlassBackground = true,
            bool addWindowHandle = true)
        {
            var panel = new GameObject(panelName);
            panel.transform.SetParent(m_UiRoot, false);
            if (addGlassBackground)
            {
                var panelSize = GetPanelSize(panelName);
                CreateGlassVisual(panel.transform, "Glass Panel Background", panelSize, 0);
            }
            if (addWindowHandle)
                CreateWindowHandle(panel.transform, GetPanelSize(panelName));
            PlacePanel(panel, verticalOffset);
            return panel;
        }

        void PlacePanel(GameObject panel, float verticalOffset, float distance = 1.1f)
        {
            if (panel == null)
                return;

            // Resolve the active XR camera for every transition. Cached camera transforms can be
            // stale after visionOS relocalization or XR Origin changes.
            var activeCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (activeCamera == null)
                return;
            m_MainCamera = activeCamera;
            var cameraTransform = activeCamera.transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = cameraTransform.forward.normalized;

            panel.transform.SetPositionAndRotation(
                cameraTransform.position + forward * distance + Vector3.up * verticalOffset,
                Quaternion.LookRotation(forward, Vector3.up));
            Debug.Log($"[Moodium Window] Placed {panel.name} in front of camera. position={panel.transform.position}, forward={forward}");
        }

        void CreateWindowHandle(Transform panel, Vector2 panelSize)
        {
            var handle = new GameObject("VisionOS Window Handle");
            handle.transform.SetParent(panel, false);
            var handleY = -panelSize.y * 0.5f - 0.035f;
            if (panel.name == "Moodium World Selection" ||
                panel.name == "Moodium Object World Selection")
                handleY = -0.47f; // Clear the Back button at y=-0.36m.
            handle.transform.localPosition = new Vector3(0f, handleY, -0.012f);
            handle.transform.localRotation = Quaternion.identity;

            var handleRenderer = CreateGlassVisual(
                handle.transform,
                "Window Handle Visual",
                new Vector2(0.12f, 0.018f),
                35);
            if (handleRenderer != null)
            {
                var handleMaterial = handleRenderer.material;
                var color = new Color(0.9f, 0.93f, 1f, 0.72f);
                if (handleMaterial.HasProperty("_Tint"))
                    handleMaterial.SetColor("_Tint", color);
                else if (handleMaterial.HasProperty("_BaseColor"))
                    handleMaterial.SetColor("_BaseColor", color);
                else if (handleMaterial.HasProperty("_Color"))
                    handleMaterial.SetColor("_Color", color);
            }
            var collider = handle.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.16f, 0.045f, 0.025f);
            var drag = handle.AddComponent<MoodiumWindowHandle>();
            drag.Configure(panel);
        }

        TMP_Text CreateText(
            Transform parent,
            string text,
            Vector3 localPosition,
            float fontSize,
            Vector2 rectSize,
            TMP_FontAsset fontOverride = null)
        {
            var textObject = new GameObject($"Text - {text.Replace("\n", " ")}");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition + new Vector3(0f, 0f, -0.035f);
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one * 0.1f;

            var tmp = textObject.AddComponent<TextMeshPro>();
            var selectedFont = fontOverride != null ? fontOverride : m_FontAsset;
            if (selectedFont != null)
                tmp.font = selectedFont;
            else if (!m_LoggedMissingFont)
            {
                m_LoggedMissingFont = true;
                Debug.LogError("[Moodium UI] TMP font asset is missing. Run Moodium/Setup Flow Font.");
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = rectSize;
            tmp.renderer.sortingOrder = 20;
            return tmp;
        }

        MoodiumSpatialButton CreateButton(
            Transform parent,
            string label,
            Vector3 localPosition,
            Vector2 size,
            System.Action onPressed)
        {
            var buttonObject = new GameObject($"Button - {label}");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.localPosition = localPosition;
            buttonObject.transform.localRotation = Quaternion.identity;

            var renderer = CreateGlassVisual(buttonObject.transform, "Button Glass Visual", size, 5);
            var hitTarget = buttonObject.AddComponent<BoxCollider>();
            hitTarget.size = new Vector3(size.x, size.y, 0.018f);

            var labelText = CreateText(
                buttonObject.transform,
                label,
                Vector3.zero,
                2.8f,
                new Vector2(Mathf.Max(2.5f, size.x * 9f), 0.85f));
            labelText.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            var button = buttonObject.AddComponent<MoodiumSpatialButton>();
            button.Configure(labelText, renderer, onPressed);
            return button;
        }

        MoodiumSpatialButton CreateModeCard(
            Transform parent,
            string chineseTitle,
            string englishTitle,
            string description,
            Sprite artwork,
            Vector3 localPosition,
            System.Action onPressed)
        {
            var card = new GameObject($"Mode Card - {englishTitle}");
            card.transform.SetParent(parent, false);
            card.transform.localPosition = localPosition;
            card.transform.localRotation = Quaternion.Euler(0f, localPosition.x < 0f ? 3f : -3f, 0f);
            var size = new Vector2(0.4f, 0.48f);
            var background = CreateModeCardFrame(card.transform, size);
            CreateModeCardArtwork(card.transform, artwork, size);

            var chinese = CreateText(
                card.transform,
                chineseTitle,
                new Vector3(0.01f, -0.105f, -0.018f),
                3.05f,
                new Vector2(3.45f, 0.68f),
                m_ChineseFontAsset != null ? m_ChineseFontAsset : m_FontAsset);
            var english = CreateText(
                card.transform,
                englishTitle,
                new Vector3(0.01f, -0.153f, -0.018f),
                1.72f,
                new Vector2(3.45f, 0.52f));
            var detail = CreateText(
                card.transform,
                description,
                new Vector3(0.01f, -0.198f, -0.018f),
                1.22f,
                new Vector2(3.45f, 0.45f),
                m_ChineseFontAsset != null ? m_ChineseFontAsset : m_FontAsset);
            chinese.alignment = TextAlignmentOptions.MidlineLeft;
            english.alignment = TextAlignmentOptions.MidlineLeft;
            detail.alignment = TextAlignmentOptions.MidlineLeft;
            english.color = new Color(0.94f, 0.95f, 1f, 0.86f);
            detail.color = new Color(0.94f, 0.95f, 1f, 0.72f);
            ApplyModeCardTextReadability(chinese);
            ApplyModeCardTextReadability(english);
            ApplyModeCardTextReadability(detail, false);

            var button = card.AddComponent<MoodiumSpatialButton>();
            button.Configure(chinese, background, onPressed, true);
            return button;
        }

        Renderer CreateModeCardFrame(Transform card, Vector2 size)
        {
            var frame = Instantiate(m_GlassPanelPrefab, card, false);
            frame.name = "Mode Card Glow Frame";
            frame.transform.localScale = new Vector3(size.x / 0.72f, size.y / 0.43f, 1f);

            foreach (var text in frame.GetComponentsInChildren<TMP_Text>(true))
                text.gameObject.SetActive(false);

            Renderer edge = null;
            foreach (var renderer in frame.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.gameObject.name == "Glass Surface")
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }
                renderer.sortingOrder += 18;
                if (renderer.gameObject.name == "Soft Edge Highlight")
                    edge = renderer;
            }

            if (edge != null)
            {
                var hitTarget = edge.gameObject.AddComponent<BoxCollider>();
                hitTarget.size = new Vector3(0.72f, 0.43f, 0.025f);
                var hover = edge.gameObject.AddComponent<VisionOSHoverEffect>();
                hover.Type = VisionOSHoverEffect.EffectType.Highlight;
                hover.Color = new Color(0.67f, 0.72f, 1f, 1f);
                hover.IntensityMultiplier = 0.55f;
            }
            return edge;
        }

        void CreateModeCardArtwork(Transform card, Sprite artwork, Vector2 cardSize)
        {
            if (artwork == null)
            {
                Debug.LogWarning($"[Moodium UI] Mode card artwork is missing on {card.name}.");
                return;
            }

            var canvasObject = new GameObject("Background Image", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(card, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0f, -0.008f);
            canvasObject.transform.localRotation = Quaternion.identity;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = m_MainCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 12;

            const float pixelsPerMeter = 1000f;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cardSize.x * 0.94f * pixelsPerMeter, cardSize.y * 0.94f * pixelsPerMeter);
            rect.localScale = Vector3.one / pixelsPerMeter;

            var imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            var image = imageObject.GetComponent<Image>();
            image.sprite = artwork;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.material = null;

            var tintObject = new GameObject("Glass Tint Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tintObject.transform.SetParent(canvasObject.transform, false);
            var tintRect = tintObject.GetComponent<RectTransform>();
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            var tint = tintObject.GetComponent<Image>();
            tint.sprite = artwork;
            tint.preserveAspect = true;
            tint.raycastTarget = false;
            tint.color = new Color(0.16f, 0.1f, 0.3f, 0.12f);
            tint.material = null;
        }

        static void ApplyModeCardTextReadability(TMP_Text text, bool bold = true)
        {
            if (text.color.a >= 0.99f)
                text.color = Color.white;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.outlineColor = Color.clear;
            text.outlineWidth = 0f;
            text.GetComponent<Renderer>().sortingOrder = 30;
        }

        Renderer CreateGlassVisual(Transform parent, string visualName, Vector2 size, int sortingOrder)
        {
            if (m_GlassPanelPrefab == null)
            {
                Debug.LogError("[Moodium UI] Glass Panel Prefab is missing. Run Moodium/Setup Glass Panel Test.");
                return null;
            }

            var visual = Instantiate(m_GlassPanelPrefab, parent, false);
            visual.name = visualName;
            visual.transform.localScale = new Vector3(size.x / 0.72f, size.y / 0.43f, 1f);

            foreach (var text in visual.GetComponentsInChildren<TMP_Text>(true))
                text.gameObject.SetActive(false);

            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            Renderer surface = null;
            foreach (var meshRenderer in renderers)
            {
                meshRenderer.sortingOrder += sortingOrder;
                if (meshRenderer.gameObject.name == "Glass Surface")
                    surface = meshRenderer;
            }
            return surface != null ? surface : (renderers.Length > 0 ? renderers[0] : null);
        }

        Material GetPickerGlassMaterial()
        {
            if (m_ObjectPickerGlassMaterial != null)
                return m_ObjectPickerGlassMaterial;
            if (m_GlassPanelPrefab == null)
                return null;
            foreach (var renderer in m_GlassPanelPrefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("Glass"))
                    return renderer.sharedMaterial;
            }
            return m_GlassPanelPrefab.GetComponentInChildren<Renderer>(true)?.sharedMaterial;
        }

        static Vector2 GetPanelSize(string panelName)
        {
            return panelName switch
            {
                "Moodium Mode Selection" => new Vector2(0.86f, 0.64f),
                "Moodium World Selection" => new Vector2(0.86f, 0.68f),
                "Moodium Object World Selection" => new Vector2(0.86f, 0.68f),
                "Moodium Model Library" => new Vector2(0.9f, 1.02f),
                "Moodium Interaction Mode" => new Vector2(0.76f, 0.5f),
                "Moodium Reality Enhancement Mode" => new Vector2(0.76f, 0.52f),
                "Spatial Physics Back" => new Vector2(0.54f, 0.2f),
                _ => new Vector2(0.8f, 0.55f)
            };
        }

        static string GetDisplayName(string prefabName)
        {
            return prefabName
                .Replace("Prefab", string.Empty)
                .Replace("_", " ")
                .Trim();
        }

        void HideSpatialMeshVisualization()
        {
            if (m_SpatialMeshManager == null)
                return;

            foreach (var renderer in m_SpatialMeshManager.GetComponentsInChildren<MeshRenderer>(true))
            {
                // Candy transformation is a separately managed visual-only overlay.
                // Never disable that layer here; only hide ARMeshManager's source mesh.
                if (renderer.gameObject.name == "Moodium Candy Visual Overlay")
                    continue;
                if (renderer.GetComponent<MeshCollider>() != null ||
                    renderer.gameObject.name.StartsWith("AR Default Mesh", System.StringComparison.Ordinal))
                    renderer.enabled = false;
            }
        }
    }
}
