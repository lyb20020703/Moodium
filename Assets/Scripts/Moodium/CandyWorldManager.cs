using Moodium.Flow;
using Moodium.Interaction;
using Moodium.NatureWorld;
using Moodium.Reality;
using Moodium.Calibration;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Moodium.CandyWorld
{
    public sealed class CandyWorldManager : MonoBehaviour
    {
        CandyEnergyController m_Energy;
        CandyBurstController m_BurstController;
        CandyEnergyWristUI m_EnergyUI;
        CandySpatialMeshTransformationController m_SpatialTransformation;
        CandyFrostParticleController m_Frost;
        CandyRainRewardController m_RainReward;
        CandyWorldProgressController m_ProgressController;
        NatureForestGrowthController m_NatureForestGrowth;
        NatureIvySpatialGrowthController m_NatureIvyGrowth;
        SpatialMeshTouchRipple m_SpatialTouchFeedback;
        CandyWrapperCrackDetector m_WrapperCrackDetector;
        WorldEnvironmentPrompt m_EnvironmentPrompt;
        CandyHandOpenBurstGesture m_HandOpenBurstGesture;
        ARMeshManager m_SpatialMeshManager;
        GameObject[] m_NatureTreePrefabs;
        GameObject m_NatureIvyGeneratorPrefab;
        Material m_NatureIvyGrowMaterial;
        bool m_NatureWorldActive;
        bool m_HasBagInteraction;
        bool m_HasShownEnvironmentPrompt;
        bool m_Running;

        TMP_FontAsset m_ChineseFont;
        TMP_FontAsset m_EnglishFont;

        public float Energy => m_Energy != null ? m_Energy.Energy : 0f;

        /// <summary>Sets the selected-language fonts used by lightweight World prompts.</summary>
        public void ConfigureEnvironmentPromptFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            m_ChineseFont = chineseFont;
            m_EnglishFont = englishFont;
            m_EnvironmentPrompt?.Configure(Camera.main, m_ChineseFont, m_EnglishFont);
        }

        // Keep existing editor tests and external callers source-compatible. The full
        // overload below is used by MoodiumAppFlowController to provide crack assets.
        public void Configure(Camera camera, GameObject glassPanelPrefab, TMP_FontAsset font,
            MoodiumWorldDefinition world, ARMeshManager meshManager, Material candyTransformationMaterial,
            Sprite wristRoundedSprite, Sprite wristCircleSprite, GameObject candyFrostPrefab)
        {
            Configure(camera, glassPanelPrefab, font, world, meshManager, candyTransformationMaterial,
                wristRoundedSprite, wristCircleSprite, candyFrostPrefab, null, null, null);
        }

        public void Configure(Camera camera, GameObject glassPanelPrefab, TMP_FontAsset font,
            MoodiumWorldDefinition world, ARMeshManager meshManager, Material candyTransformationMaterial,
            Sprite wristRoundedSprite, Sprite wristCircleSprite, GameObject candyFrostPrefab,
            Texture2D candyCrackTexture, Material candyCrackMaterial, GameObject candyCrackPrefab)
        {
            m_SpatialMeshManager = meshManager;
            m_Energy = GetComponent<CandyEnergyController>();
            if (m_Energy == null)
                m_Energy = gameObject.AddComponent<CandyEnergyController>();
            m_BurstController = GetComponent<CandyBurstController>();
            if (m_BurstController == null)
                m_BurstController = gameObject.AddComponent<CandyBurstController>();
            m_BurstController.Configure(world != null ? world.Prefabs : null);
            m_RainReward = GetComponent<CandyRainRewardController>();
            if (m_RainReward == null)
                m_RainReward = gameObject.AddComponent<CandyRainRewardController>();
            m_RainReward.Configure(camera, world != null ? world.Prefabs : null);
            m_SpatialTransformation = GetComponent<CandySpatialMeshTransformationController>();
            if (m_SpatialTransformation == null)
                m_SpatialTransformation = gameObject.AddComponent<CandySpatialMeshTransformationController>();
            m_SpatialTransformation.Configure(meshManager, candyTransformationMaterial);
            m_SpatialTouchFeedback = GetComponent<SpatialMeshTouchRipple>();
            if (m_SpatialTouchFeedback == null)
                m_SpatialTouchFeedback = gameObject.AddComponent<SpatialMeshTouchRipple>();
            m_SpatialTouchFeedback.Configure(
                meshManager,
                m_Energy,
                GetComponent<CandyEnvironmentShaderController>());
            m_SpatialTouchFeedback.SetInteractionEnabled(false);
            m_WrapperCrackDetector = GetComponent<CandyWrapperCrackDetector>();
            if (m_WrapperCrackDetector == null)
                m_WrapperCrackDetector = gameObject.AddComponent<CandyWrapperCrackDetector>();
            m_WrapperCrackDetector.Configure(meshManager, m_SpatialTransformation, candyCrackTexture, candyCrackMaterial, candyCrackPrefab);
            m_WrapperCrackDetector.SetInteractionEnabled(false);
            EnsureEnvironmentPrompt();
            m_EnvironmentPrompt.Configure(camera, m_ChineseFont, m_EnglishFont);
            m_EnvironmentPrompt.gameObject.SetActive(false);
            m_HandOpenBurstGesture = GetComponent<CandyHandOpenBurstGesture>();
            if (m_HandOpenBurstGesture == null)
                m_HandOpenBurstGesture = gameObject.AddComponent<CandyHandOpenBurstGesture>();
            m_HandOpenBurstGesture.Configure(m_Energy);
            m_HandOpenBurstGesture.SetGestureEnabled(false);

            if (m_Frost == null && candyFrostPrefab != null)
            {
                var frostObject = Instantiate(candyFrostPrefab, transform);
                frostObject.name = "Candy Frost Atmosphere";
                m_Frost = frostObject.GetComponent<CandyFrostParticleController>();
            }

            m_ProgressController = GetComponent<CandyWorldProgressController>();
            if (m_ProgressController == null)
                m_ProgressController = gameObject.AddComponent<CandyWorldProgressController>();
            m_ProgressController.Configure(
                camera, m_SpatialTransformation, m_Frost, m_RainReward,
                world != null ? world.Prefabs : null);

            m_NatureForestGrowth = GetComponent<NatureForestGrowthController>();
            if (m_NatureForestGrowth == null)
                m_NatureForestGrowth = gameObject.AddComponent<NatureForestGrowthController>();
            m_NatureIvyGrowth = GetComponent<NatureIvySpatialGrowthController>();
            if (m_NatureIvyGrowth == null)
                m_NatureIvyGrowth = gameObject.AddComponent<NatureIvySpatialGrowthController>();

            if (m_EnergyUI == null)
            {
                var ui = new GameObject("Candy Energy Left Wrist HUD");
                ui.transform.SetParent(transform, false);
                m_EnergyUI = ui.AddComponent<CandyEnergyWristUI>();
                m_EnergyUI.Build(camera, font, wristRoundedSprite, wristCircleSprite);
            }
            StopExperience();
        }

        public void StartExperience(MoodiumWorldDefinition world)
        {
            m_BurstController.Configure(world != null ? world.Prefabs : null);
            m_RainReward.Configure(Camera.main, world != null ? world.Prefabs : null);
            m_ProgressController.Configure(
                Camera.main, m_SpatialTransformation, m_Frost, m_RainReward,
                world != null ? world.Prefabs : null);
            m_NatureWorldActive = world != null &&
                                  world.WorldId.Equals("nature", System.StringComparison.OrdinalIgnoreCase);
            m_EnergyUI?.SetWorldTheme(m_NatureWorldActive);
            m_HasBagInteraction = false;
            m_HasShownEnvironmentPrompt = false;
            if (m_EnvironmentPrompt != null)
                m_EnvironmentPrompt.gameObject.SetActive(false);
            // The pink candy-wrapper overlay belongs only to Candy World. Nature keeps
            // the real spatial mesh unobscured while its own trees and ivy grow in.
            m_ProgressController.SetSpatialTransformationEnabled(!m_NatureWorldActive);
            if (m_NatureWorldActive)
            {
                m_NatureForestGrowth.Configure(Camera.main != null ? Camera.main.transform : null, m_NatureTreePrefabs);
                m_NatureForestGrowth.StartGrowth();
                m_NatureIvyGrowth.Configure(
                    Camera.main,
                    m_SpatialMeshManager,
                    m_NatureIvyGeneratorPrefab,
                    m_NatureIvyGrowMaterial,
                    3);
                m_NatureIvyGrowth.StartGrowth();
            }
            else
            {
                m_NatureForestGrowth.StopAndClear();
                m_NatureIvyGrowth.StopAndClear();
            }
            m_Energy.ResetEnergy();
            m_ProgressController.StartProgress();
            m_SpatialTouchFeedback?.SetInteractionEnabled(true);
            m_WrapperCrackDetector?.SetInteractionEnabled(!m_NatureWorldActive);
            m_HandOpenBurstGesture?.SetGestureEnabled(true);
            m_EnergyUI.gameObject.SetActive(true);
            if (m_Running)
                return;
            m_Running = true;
            ChocolateCapsuleInteraction.AnyCapsulePinched += OnCapsulePinched;
            m_Energy.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(0f, 0f);
            Debug.Log("[Candy World] Reality Enhancement Mode experience started.");
        }

        public void StopExperience()
        {
            if (m_Running)
            {
                ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
                m_Energy.EnergyChanged -= OnEnergyChanged;
            }
            m_Running = false;
            m_SpatialTouchFeedback?.SetInteractionEnabled(false);
            m_WrapperCrackDetector?.SetInteractionEnabled(false);
            m_WrapperCrackDetector?.ClearAll();
            if (m_EnvironmentPrompt != null)
                m_EnvironmentPrompt.gameObject.SetActive(false);
            m_HandOpenBurstGesture?.SetGestureEnabled(false);
            if (m_EnergyUI != null)
                m_EnergyUI.gameObject.SetActive(false);
            m_ProgressController?.StopAndClear();
            m_NatureForestGrowth?.StopAndClear();
            m_NatureIvyGrowth?.StopAndClear();
            m_BurstController?.StopAndClear();
        }

        void OnDestroy()
        {
            StopExperience();
        }

        void OnCapsulePinched(ChocolateCapsuleInteraction capsule, Vector3 position)
        {
            if (!m_Running || capsule == null || !capsule.gameObject.activeInHierarchy)
                return;
            var trackedSource = capsule.GetComponentInParent<ARTrackedObject>() != null ||
                                capsule.GetComponentInParent<ARTrackedImage>() != null;
            if (!trackedSource)
            {
                var poseFollower = capsule.GetComponent<TrackedObjectPoseFollower>();
                if (poseFollower != null && poseFollower.Anchor != null)
                    trackedSource = poseFollower.Anchor.GetComponentInParent<ARTrackedObject>() != null ||
                                    poseFollower.Anchor.GetComponentInParent<ARTrackedImage>() != null;
            }
            // Phase 2 presents the visual under a calibrated image-anchor root instead
            // of directly below ARTrackedImage. It is still the same tracked sensory bag
            // and must participate in the existing Candy/Nature feedback event chain.
            if (!trackedSource && capsule.GetComponentInParent<CalibratedSensoryBagPresentation>() != null)
                trackedSource = true;
            if (!trackedSource)
            {
                Debug.Log("[Candy World] Ignored a Chocolate Capsule that is not attached to a tracked source.");
                return;
            }
            // The interaction event's cached renderer center may predate calibration
            // scaling. Resolve the current world-space bounds now so every candy burst
            // starts in the visible middle of the calibrated Soft model.
            var visualCenter = ResolveCurrentCapsuleCenter(capsule, position);
            m_ProgressController.SetOrigin(visualCenter);
            m_Energy.RegisterPinch();
            m_HasBagInteraction = true;
            m_BurstController.Burst(visualCenter, capsule.transform);
            if (m_NatureWorldActive)
                m_NatureIvyGrowth.TryGrowNearInteraction(visualCenter);
            TryShowEnvironmentPrompt(visualCenter, m_Energy.NormalizedEnergy);
        }

        static Vector3 ResolveCurrentCapsuleCenter(ChocolateCapsuleInteraction capsule, Vector3 fallback)
        {
            var primaryCollider = capsule.GetComponent<Collider>();
            if (primaryCollider != null && primaryCollider.enabled)
                return primaryCollider.bounds.center;

            var renderers = capsule.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return fallback;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        public void ConfigureNatureForestTrees(GameObject[] treePrefabs)
        {
            m_NatureTreePrefabs = treePrefabs;
        }

        public void ConfigureNatureIvy(GameObject generatorPrefab, Material growMaterial)
        {
            m_NatureIvyGeneratorPrefab = generatorPrefab;
            m_NatureIvyGrowMaterial = growMaterial;
        }

        void OnEnergyChanged(float energy, float normalizedProgress)
        {
            m_EnergyUI.SetProgress(energy, normalizedProgress);
            m_ProgressController.SetProgress(energy, normalizedProgress);
            if (m_NatureWorldActive)
                m_NatureForestGrowth.SetProgress(normalizedProgress);
            TryShowEnvironmentPrompt(m_SpatialTransformation != null ? m_SpatialTransformation.Origin : transform.position,
                normalizedProgress);
        }

        void TryShowEnvironmentPrompt(Vector3 worldAnchor, float progress)
        {
            if (!m_Running || m_HasShownEnvironmentPrompt || m_EnvironmentPrompt == null ||
                !WorldEnvironmentPromptPolicy.ShouldShow(m_NatureWorldActive, progress, m_HasBagInteraction))
                return;
            var english = PlayerPrefs.GetString("Moodium.Language", "Chinese")
                .Equals("English", System.StringComparison.OrdinalIgnoreCase);
            m_EnvironmentPrompt.Configure(Camera.main, m_ChineseFont, m_EnglishFont);
            m_EnvironmentPrompt.Show(m_NatureWorldActive, english, worldAnchor);
            m_HasShownEnvironmentPrompt = true;
            Debug.Log($"[Moodium Environment Guide] Shown {(m_NatureWorldActive ? "Nature ivy" : "Candy crack")} prompt.");
        }

        // The prompt must be a child object.  Adding it to this manager's GameObject
        // made SetActive(false) disable the entire CandyWorldManager, so the guide
        // could never reliably reach its activation point.
        void EnsureEnvironmentPrompt()
        {
            if (m_EnvironmentPrompt != null && m_EnvironmentPrompt.gameObject != gameObject)
                return;

            var legacyPrompt = m_EnvironmentPrompt != null
                ? m_EnvironmentPrompt
                : GetComponent<WorldEnvironmentPrompt>();
            var promptRoot = new GameObject("World Environment Interaction Prompt");
            promptRoot.transform.SetParent(transform, false);
            promptRoot.SetActive(false);
            m_EnvironmentPrompt = promptRoot.AddComponent<WorldEnvironmentPrompt>();

            // This is only a runtime-created legacy component.  Destroying the
            // component, rather than its owner, preserves the active world manager.
            if (legacyPrompt != null && legacyPrompt.gameObject == gameObject)
                Destroy(legacyPrompt);
        }
    }
}
