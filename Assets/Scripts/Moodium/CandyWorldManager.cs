using Moodium.Flow;
using Moodium.Interaction;
using Moodium.NatureWorld;
using Moodium.Reality;
using Moodium.Calibration;
using Moodium.Opening;
using Moodium.Squeeze;
using Moodium.MoodCheck;
using Moodium.Rewards;
using Moodium.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Moodium.CandyWorld
{
    public sealed class CandyWorldManager : MonoBehaviour
    {
        FloatingSqueezeProgress m_FloatingProgress;
        CandyEnergyController m_Energy;
        CandyBurstController m_BurstController;
        CandyEnergyWristUI m_EnergyUI;
        CandySpatialMeshTransformationController m_SpatialTransformation;
        CandyFrostParticleController m_Frost;
        NatureLeafParticleController m_NatureLeaves;
        CandyRainRewardController m_RainReward;
        WalkTrailGuideController m_WalkTrailGuide;
        GameObject m_WalkTrailGuidePrefab;
        public void ConfigureWalkTrailGuide(GameObject prefab) => m_WalkTrailGuidePrefab = prefab;
        CandyWorldProgressController m_ProgressController;
        NatureForestGrowthController m_NatureForestGrowth;
        NatureFootstepGrassController m_NatureFootstepGrass;
        NatureCompletionPanoramaController m_NatureCompletionPanorama;
        // Completion reward in both worlds: a bubble holding a tiny scanned world that grows around the viewer.
        BubbleWorldController m_BubbleWorld;
        WorldJourneyDirector m_Journey;
        RoomContentVisibility m_RoomVisibility;
        BubbleRingController m_Rings;
        BubbleWorldNarrator m_Narrator;
        [SerializeField] bool m_SqueezeBreath = true;
        bool m_BreathTaught, m_BubbleEntryScheduled;
        bool m_ReopenBubbleEntry;
        float m_RevealedAt = -1f;
        bool m_StayReminder;
        public WorldJourneyDirector Journey => m_Journey;
        CandyCompletionHouseController m_CandyCompletionHouse;
        NatureIvySpatialGrowthController m_NatureIvyGrowth;
        SpatialMeshTouchRipple m_SpatialTouchFeedback;
        CandyHandOpenBurstGesture m_HandOpenBurstGesture;
        ISqueezeProgressDisplay m_SqueezeProgressVideo;
        // Squeeze rescue story: Moodi falls into the bag, the user squeezes it back out.
        MoodiRescueDirector m_Rescue;
        Coroutine m_BindVideoRoutine;
        bool m_TutorialShown;
        // After 100%: the "?" dot + hint wait for Moodi's thank-you and palm teaching (no more explore card at eye level).
        const float RescueExploreDelay = Moodium.Guide.GuideHelpController.ExploreHintDelay;
        Moodium.Guide.GuideHelpController m_Help;
        SqueezeModeController m_SqueezeModes;
        RewardSequenceController m_SqueezeStageRewards;
        MoodCheckContext m_SqueezeContext;
        float m_NextEnergyUIUpdate;
        public void ConfigureSqueezeContext(MoodCheckContext context)=>m_SqueezeContext=context;
        ARMeshManager m_SpatialMeshManager;
        GameObject[] m_NatureTreePrefabs;
        GameObject m_NatureFootstepGrassPrefab;
        GameObject m_NatureFootstepAlternatePrefab;
        GameObject m_CandyFootstepBeanPrefab;
        Texture2D m_NatureCompletionPanoramaTexture;
        Texture2D m_CandyCompletionPanoramaTexture;
        GameObject m_CandyCompletionHousePrefab;
        GameObject m_CandyLittleCompletionHousePrefab;
        GameObject[] m_NatureBurstPrefabs;
        GameObject[] m_CandyBurstPrefabs;
        GameObject m_NatureIvyGeneratorPrefab;
        Material m_NatureIvyGrowMaterial;
        AudioClip m_RewardTouchSound;
        AudioClip m_CandyPrefabDeformationSound;
        System.Action m_ExitAction;
        bool m_NatureWorldActive;
        bool m_Running;
        Moodium.Guide.WorldGuideCards m_WorldGuide;
        bool m_ExploreGuideShown;
        bool m_RewardReleased;
        float m_LastNatureProgress;
        Vector3 m_LastNatureSoftOrigin;
        [Header("Nature Vine Debug")]
        [SerializeField] bool m_NatureVineDebug;
        [Header("Nature Burst Debug")]
        [SerializeField] bool m_ForceNatureBurst;
        [SerializeField] NatureBurstDebugPrefabSelection m_ForceNatureBurstPrefab = NatureBurstDebugPrefabSelection.Random;
        [Tooltip("Temporary diagnostic: use the proven Candy burst prefab pool while in Nature World.")]
        [SerializeField] bool m_UseCandyBurstPrefabsInNatureForDebug;

        TMP_FontAsset m_ChineseFont;
        TMP_FontAsset m_EnglishFont;

        public float Energy => m_Energy != null ? m_Energy.Energy : 0f;

        public void ConfigureRewardTouchSound(AudioClip touchSound) => m_RewardTouchSound = touchSound;
        public void ConfigureCandyPrefabDeformationSound(AudioClip sound) => m_CandyPrefabDeformationSound = sound;
        public void ConfigureExitAction(System.Action exitAction) => m_ExitAction = exitAction;
        public static bool WristHiddenFor(bool running, WorldJourneyStage? stage) => running && stage == WorldJourneyStage.Bubble;

        void Update()
        {
            // No wrist band inside the bubble worlds (from the touch that enters until back in the room).
            if (m_EnergyUI != null) m_EnergyUI.Suppressed = WristHiddenFor(m_Running, m_Journey != null ? m_Journey.Stage : (WorldJourneyStage?)null);
            if (m_Running && m_Journey != null)
            {
                m_Rings?.SetInteractionState(m_Journey.Stage == WorldJourneyStage.Explore,
                    m_WalkTrailGuide != null && m_WalkTrailGuide.HandHasMoodi, m_Help != null && m_Help.IsOpen,
                    m_BubbleWorld != null && m_BubbleWorld.CurrentPhase != BubbleWorldController.Phase.Armed && m_BubbleWorld.CurrentPhase != BubbleWorldController.Phase.Idle);
                if (m_Journey.Stage == WorldJourneyStage.Bubble && m_RevealedAt >= 0f)
                {
                    if (!m_StayReminder && Time.time - m_RevealedAt >= 90f) { m_StayReminder = true; m_WalkTrailGuide?.Say("F6"); }
                    var held = false;
                    foreach (var heldCapsule in FindObjectsByType<ChocolateCapsuleInteraction>(FindObjectsSortMode.None)) held |= heldCapsule.IsPinched;
                    m_BubbleWorld.SetBreath(m_SqueezeBreath && held ? 1f : 0f);
                    // Breath tip waits until Moodi's world intro has finished (the intro already invites slow breathing).
                    if (m_SqueezeBreath && held && !m_BreathTaught && (m_Narrator == null || !m_Narrator.IsSpeaking)) { m_BreathTaught = true; m_WalkTrailGuide?.Say("F4"); }
                }
            }
            if (m_Journey != null && !m_Journey.CanGrowInRoom) return;
            if (!m_ForceNatureBurst)
                return;
            if (!m_NatureWorldActive || !m_Running)
                return;

            var capsule = FindActiveCalibratedNatureCapsule();
            if (capsule == null)
                return;
            m_ForceNatureBurst = false;
            var center = ResolveCurrentCapsuleCenter(capsule, capsule.transform.position);
            Debug.Log($"[Nature Burst Debug] Force Nature Burst source={capsule.name}; visualCenter={center}; selection={m_ForceNatureBurstPrefab}.");
            m_BurstController?.ForceNatureBurstPure(center, capsule.transform, m_ForceNatureBurstPrefab);
        }

        /// <summary>Sets the selected-language fonts used by lightweight World prompts.</summary>
        public void ConfigureEnvironmentPromptFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            m_ChineseFont = chineseFont;
            m_EnglishFont = englishFont;
            m_EnergyUI?.ConfigureLanguageFonts(chineseFont, englishFont);
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
            Texture2D candyCrackTexture, Material candyCrackMaterial, GameObject candyCrackPrefab,
            GameObject natureLeafFallPrefab = null)
        {
            m_SpatialMeshManager = meshManager;
            m_Energy = GetComponent<CandyEnergyController>();
            if (m_Energy == null)
                m_Energy = gameObject.AddComponent<CandyEnergyController>();
            m_BurstController = GetComponent<CandyBurstController>();
            if (m_BurstController == null)
                m_BurstController = gameObject.AddComponent<CandyBurstController>();
            m_BurstController.Configure(world != null ? world.Prefabs : null);
            m_BurstController.SetNatureWorld(false);
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
            m_HandOpenBurstGesture = GetComponent<CandyHandOpenBurstGesture>();
            if (m_HandOpenBurstGesture == null)
                m_HandOpenBurstGesture = gameObject.AddComponent<CandyHandOpenBurstGesture>();
            m_HandOpenBurstGesture.Configure(m_Energy);
            m_HandOpenBurstGesture.SetGestureEnabled(false);

            m_SqueezeProgressVideo = GetComponent<SqueezeProgressDisplayRouter>();
            if (m_SqueezeProgressVideo == null)
                m_SqueezeProgressVideo = gameObject.AddComponent<SqueezeProgressDisplayRouter>();
            var openingVideoConfig = Resources.Load<OpeningVideoSequenceConfig>("MoodiumOpening/OpeningVideoSequenceConfig");
            m_SqueezeProgressVideo.Configure(
                camera,
                openingVideoConfig != null ? openingVideoConfig.CandySqueezeClip : null,
                openingVideoConfig != null ? openingVideoConfig.NatureSqueezeClip : null);

            if (m_Frost == null && candyFrostPrefab != null)
            {
                var frostObject = Instantiate(candyFrostPrefab, transform);
                frostObject.name = "Candy Frost Atmosphere";
                m_Frost = frostObject.GetComponent<CandyFrostParticleController>();
            }
            if (m_NatureLeaves == null && natureLeafFallPrefab != null)
            {
                var leafObject = Instantiate(natureLeafFallPrefab, transform);
                leafObject.name = "Nature Leaf Atmosphere";
                m_NatureLeaves = leafObject.GetComponent<NatureLeafParticleController>();
                leafObject.SetActive(false);
            }

            m_ProgressController = GetComponent<CandyWorldProgressController>();
            if (m_ProgressController == null)
                m_ProgressController = gameObject.AddComponent<CandyWorldProgressController>();
            m_ProgressController.Configure(
                camera, m_SpatialTransformation, m_Frost, m_RainReward,
                world != null ? world.Prefabs : null, m_CandyPrefabDeformationSound);

            m_NatureForestGrowth = GetComponent<NatureForestGrowthController>();
            if (m_NatureForestGrowth == null)
                m_NatureForestGrowth = gameObject.AddComponent<NatureForestGrowthController>();
            m_NatureFootstepGrass = GetComponent<NatureFootstepGrassController>();
            if (m_NatureFootstepGrass == null)
                m_NatureFootstepGrass = gameObject.AddComponent<NatureFootstepGrassController>();
            m_NatureCompletionPanorama = GetComponent<NatureCompletionPanoramaController>();
            if (m_NatureCompletionPanorama == null)
                m_NatureCompletionPanorama = gameObject.AddComponent<NatureCompletionPanoramaController>();
            m_CandyCompletionHouse = GetComponent<CandyCompletionHouseController>();
            if (m_CandyCompletionHouse == null)
                m_CandyCompletionHouse = gameObject.AddComponent<CandyCompletionHouseController>();
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
            m_EnergyUI.ReleasePressed -= ReleaseAccumulatedEnergy;
            m_EnergyUI.ReleasePressed += ReleaseAccumulatedEnergy;
            m_EnergyUI.ExitPressed -= RequestExit;
            m_EnergyUI.ExitPressed += RequestExit;
            StopExperience();
        }

        public void StartExperience(MoodiumWorldDefinition world)
        {
            m_NatureWorldActive = world != null &&
                                  string.Equals(world.WorldId, "nature", System.StringComparison.OrdinalIgnoreCase);
            var useCandyPoolInNature = m_NatureWorldActive && m_UseCandyBurstPrefabsInNatureForDebug &&
                                        m_CandyBurstPrefabs != null && m_CandyBurstPrefabs.Length > 0;
            if (m_NatureWorldActive && m_UseCandyBurstPrefabsInNatureForDebug && !useCandyPoolInNature)
                Debug.LogWarning("[Nature Burst Debug] Candy prefab pool was not configured; falling back to the Nature pool.");
            m_BurstController.Configure(useCandyPoolInNature
                ? m_CandyBurstPrefabs
                : world != null ? world.Prefabs : null);
            m_BurstController.SetNatureWorld(CandyBurstController.ShouldUseNaturePrefabPool(
                m_NatureWorldActive, useCandyPoolInNature));
            if (useCandyPoolInNature)
                Debug.Log("[Nature Burst Debug] Nature World is using the validated Candy prefab burst pool.");
            m_RainReward.Configure(Camera.main,
                m_NatureWorldActive ? m_NatureBurstPrefabs : world != null ? world.Prefabs : null,
                m_NatureWorldActive);
            m_RainReward.ConfigureTouchAudio(m_RewardTouchSound);
            // The frosting particle system belongs exclusively to Candy World.  Do not
            // give it to Nature's progress controller, otherwise it can start emitting
            // behind the Nature-specific leaf atmosphere at completion.
            m_ProgressController.Configure(
                Camera.main, m_SpatialTransformation, m_NatureWorldActive ? null : m_Frost, m_RainReward,
                world != null ? world.Prefabs : null, m_CandyPrefabDeformationSound);
            m_EnergyUI?.SetWorldTheme(m_NatureWorldActive);
            m_RewardReleased = false;
            m_EnergyUI?.SetReleaseAvailable(false);
            m_EnergyUI?.SetExitAvailable(true);
            m_LastNatureProgress = 0f;
            m_LastNatureSoftOrigin = Vector3.zero;
            // The pink candy-wrapper overlay belongs only to Candy World. Nature keeps
            // the real spatial mesh unobscured while its own trees and ivy grow in.
            m_ProgressController.SetSpatialTransformationEnabled(!m_NatureWorldActive);
            if (m_NatureWorldActive)
            {
                if (m_Frost != null)
                    m_Frost.gameObject.SetActive(false);
                if (m_NatureLeaves != null)
                {
                    m_NatureLeaves.gameObject.SetActive(true);
                    m_NatureLeaves.StartLeaves();
                }
                m_NatureForestGrowth.Configure(Camera.main != null ? Camera.main.transform : null, m_NatureTreePrefabs,
                    m_SpatialMeshManager);
                m_NatureForestGrowth.ConfigureSpawnSound(m_RewardTouchSound);
                m_NatureForestGrowth.StartGrowth();
                m_NatureFootstepGrass.Configure(Camera.main != null ? Camera.main.transform : null,
                    m_SpatialMeshManager, m_NatureFootstepGrassPrefab, m_NatureFootstepAlternatePrefab);
                m_NatureFootstepGrass.StartTracking();
                m_NatureCompletionPanorama.StopAndClear();
                m_CandyCompletionHouse.StopAndClear();
                m_NatureIvyGrowth.Configure(
                    Camera.main,
                    m_SpatialMeshManager,
                    m_NatureIvyGeneratorPrefab,
                    m_NatureIvyGrowMaterial,
                    48);
                m_NatureIvyGrowth.SetDebugMode(m_NatureVineDebug);
                m_NatureIvyGrowth.StartGrowth();
            }
            else
            {
                if (m_NatureLeaves != null)
                {
                    m_NatureLeaves.StopLeaves();
                    m_NatureLeaves.gameObject.SetActive(false);
                }
                if (m_Frost != null)
                    m_Frost.gameObject.SetActive(true);
                m_NatureForestGrowth.StopAndClear();
                m_NatureFootstepGrass.ConfigureCandy(Camera.main != null ? Camera.main.transform : null,
                    m_SpatialMeshManager, m_CandyFootstepBeanPrefab);
                m_NatureFootstepGrass.StartTracking();
                m_NatureCompletionPanorama.StopAndClear();
                m_CandyCompletionHouse.Configure(Camera.main != null ? Camera.main.transform : null,
                    m_SpatialMeshManager, m_CandyCompletionHousePrefab, m_CandyLittleCompletionHousePrefab,
                    m_RewardTouchSound);
                m_CandyCompletionHouse.ConfigureDeformationSound(m_CandyPrefabDeformationSound);
                m_CandyCompletionHouse.ConfigureContactBursts(m_BurstController);
                GameObject honeyJar = null;
                if (world != null && world.Prefabs != null)
                    foreach (var prefab in world.Prefabs)
                        if (prefab != null && CandyWorldProgressController.NormalizePrefabName(prefab.name)
                            .Equals("HoneyJar", System.StringComparison.OrdinalIgnoreCase))
                        {
                            honeyJar = prefab;
                            break;
                        }
                m_CandyCompletionHouse.ConfigureHoneyJar(honeyJar);
                m_CandyCompletionHouse.StartDisplay();
                m_NatureIvyGrowth.StopAndClear();
            }
            if (m_BubbleWorld == null && !TryGetComponent(out m_BubbleWorld))
                m_BubbleWorld = gameObject.AddComponent<BubbleWorldController>();
            m_BubbleWorld.ConfigureTouchSound(m_RewardTouchSound);
            m_BubbleWorld.Begin(Camera.main, BubbleWorldData.FolderFor(!m_NatureWorldActive));
            m_WalkTrailGuide = GetComponent<WalkTrailGuideController>() ?? gameObject.AddComponent<WalkTrailGuideController>();
            m_WalkTrailGuide.BeginSession(m_NatureFootstepGrass, m_RainReward,
                Camera.main != null ? Camera.main.transform : null,
                m_EnergyUI != null ? m_EnergyUI.transform : null, !m_NatureWorldActive,
                world != null ? world.Prefabs : null, m_WalkTrailGuidePrefab);
            m_Journey = GetComponent<WorldJourneyDirector>() ?? gameObject.AddComponent<WorldJourneyDirector>();
            m_Journey.Begin();
            m_RoomVisibility = GetComponent<RoomContentVisibility>() ?? gameObject.AddComponent<RoomContentVisibility>();
            m_RoomVisibility.Clear();
            m_Rings = GetComponent<BubbleRingController>() ?? gameObject.AddComponent<BubbleRingController>();
            m_Rings.Configure(Camera.main != null ? Camera.main.transform : null, m_BubbleWorld, !m_NatureWorldActive);
            m_Rings.FirstRingShown += OnFirstRing;
            m_Rings.HintRequested += OnRingHint;
            m_WalkTrailGuide.WalkInvited += OnWalkInvited;
            m_WalkTrailGuide.GardenDiscovered += OnGardenDiscovered;
            m_WalkTrailGuide.StepRecorded += m_Rings.RecordStep;
            m_BubbleWorld.Touched += OnBubbleTouched;
            m_BubbleWorld.Left += OnBubbleLeft;
            m_BubbleWorld.Revealed += OnBubbleRevealed;
            m_BubbleWorld.Wandered += OnBubbleWandered;
            m_BubbleWorld.LayerRevealed += OnBubbleLayerRevealed;
            m_Narrator = GetComponent<BubbleWorldNarrator>() ?? gameObject.AddComponent<BubbleWorldNarrator>();
            m_Narrator.ResetSession();
            m_BreathTaught = m_StayReminder = false; m_BubbleEntryScheduled = false; m_ReopenBubbleEntry = false; m_RevealedAt = -1f;
            m_Energy.ResetEnergy();
            m_SqueezeModes=GetComponent<SqueezeModeController>()??gameObject.AddComponent<SqueezeModeController>();
            m_SqueezeModes.BeginSession(m_Energy,m_SqueezeContext,m_NatureWorldActive,IsValidSqueezeSource);
            m_SqueezeStageRewards=GetComponent<RewardSequenceController>()??gameObject.AddComponent<RewardSequenceController>();
            m_SqueezeStageRewards.ConfigureTableFeedback(m_Energy,m_SqueezeModes.StageFeedback);
            m_SqueezeStageRewards.BeginSession();
            m_NextEnergyUIUpdate=0;
            m_ProgressController.StartProgress();
            m_SqueezeProgressVideo?.Begin(m_NatureWorldActive);
            if (!m_Running) m_TutorialShown = false;
            BeginSqueezeRescue();
            m_SpatialTouchFeedback?.SetInteractionEnabled(true);
            m_HandOpenBurstGesture?.SetGestureEnabled(true);
            m_EnergyUI.gameObject.SetActive(true);
            if (m_Running)
                return;
            m_ExploreGuideShown=false;
            m_WorldGuide=GetComponent<Moodium.Guide.WorldGuideCards>()??gameObject.AddComponent<Moodium.Guide.WorldGuideCards>();
            // The tutorial no longer pops up: it opens from the "?" beside the mode buttons. Moodi's request to be
            // squeezed out (MoodiRescueDirector.IntroFinished) only cues the "?" once, on the first visit.
            m_Help=GetComponent<Moodium.Guide.GuideHelpController>()??gameObject.AddComponent<Moodium.Guide.GuideHelpController>();
            m_Help.Begin(m_SqueezeModes,m_WorldGuide,m_NatureWorldActive,Moodium.Guide.WorldGuideCards.IsEnglish());
            if (m_Rescue == null || m_Rescue.IntroDone) ShowWorldTutorial();
            m_Running = true;
            ChocolateCapsuleInteraction.AnyCapsulePinched += OnCapsulePinched;
            m_Energy.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(0f, 0f);
            Debug.Log("[Candy World] Reality Enhancement Mode experience started.");
        }

        public void StopExperience()
        {
            if (m_BubbleWorld != null) { m_BubbleWorld.Touched -= OnBubbleTouched; m_BubbleWorld.Left -= OnBubbleLeft; m_BubbleWorld.Revealed -= OnBubbleRevealed; m_BubbleWorld.Wandered -= OnBubbleWandered; m_BubbleWorld.LayerRevealed -= OnBubbleLayerRevealed; }
            m_Narrator?.Stop();
            if (m_WalkTrailGuide != null) { m_WalkTrailGuide.WalkInvited -= OnWalkInvited; m_WalkTrailGuide.GardenDiscovered -= OnGardenDiscovered; if (m_Rings != null) m_WalkTrailGuide.StepRecorded -= m_Rings.RecordStep; }
            if (m_Rings != null) { m_Rings.FirstRingShown -= OnFirstRing; m_Rings.HintRequested -= OnRingHint; m_Rings.StopAndClear(); }
            m_RoomVisibility?.Clear(); m_Journey?.End();
            m_EnergyUI?.SetRoomInteractionEnabled(true);
            foreach (var bracelet in FindObjectsByType<Moodium.Rewards.RewardBracelet>(FindObjectsSortMode.None)) bracelet.SetInteractionEnabled(true);
            m_Frost?.SetSuspended(false); m_NatureLeaves?.SetSuspended(false);
            m_NatureIvyGrowth?.SetSuspended(false); m_NatureFootstepGrass?.SetSuspended(false);
            m_WalkTrailGuide?.SetSuspended(false); m_SqueezeModes?.SetSuspended(false); m_Help?.SetSuspended(false);
            m_Help?.Stop();
            m_WorldGuide?.Hide(true);
            m_SqueezeModes?.StopSession();
            m_SqueezeStageRewards?.StopAndClear();
            m_WalkTrailGuide?.StopAndClear();
            StopSqueezeRescue();
            if (m_Running)
            {
                ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
                m_Energy.EnergyChanged -= OnEnergyChanged;
            }
            m_Running = false;
            if (m_FloatingProgress != null) m_FloatingProgress.gameObject.SetActive(false);
            m_SpatialTouchFeedback?.SetInteractionEnabled(false);
            m_HandOpenBurstGesture?.SetGestureEnabled(false);
            if (m_EnergyUI != null)
            {
                m_EnergyUI.SetReleaseAvailable(false);
                m_EnergyUI.SetExitAvailable(false);
                m_EnergyUI.gameObject.SetActive(false);
            }
            m_ProgressController?.StopAndClear();
            m_SqueezeProgressVideo?.ResetAndHide();
            m_NatureLeaves?.StopLeaves();
            m_NatureForestGrowth?.StopAndClear();
            m_NatureFootstepGrass?.StopAndClear();
            m_NatureCompletionPanorama?.StopAndClear();
            m_BubbleWorld?.StopAndClear();
            m_CandyCompletionHouse?.StopAndClear();
            m_NatureIvyGrowth?.StopAndClear();
            m_BurstController?.StopAndClear();
        }

        void OnDestroy()
        {
            ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
            if(m_Energy!=null)m_Energy.EnergyChanged-=OnEnergyChanged;
            if (m_EnergyUI != null)
            {
                m_EnergyUI.ReleasePressed -= ReleaseAccumulatedEnergy;
                m_EnergyUI.ExitPressed -= RequestExit;
            }
            StopExperience();
        }

        void BeginSqueezeRescue()
        {
            if(m_SqueezeProgressVideo is SqueezeProgressDisplayRouter router)
                router.Mesh.HasStory=m_WalkTrailGuidePrefab!=null;
            // Show the jelly dome and video as soon as the calibrated soft model exists, not on the first press.
            if (m_BindVideoRoutine != null) StopCoroutine(m_BindVideoRoutine);
            m_BindVideoRoutine = StartCoroutine(BindSqueezeVideoWhenSoftAppears());
            if (m_Rescue == null && !TryGetComponent(out m_Rescue))
                m_Rescue = gameObject.AddComponent<MoodiRescueDirector>();
            m_Rescue.IntroFinished -= ShowWorldTutorial;
            m_Rescue.IntroFinished += ShowWorldTutorial;
            m_Rescue.Rescued -= OnMoodiRescued;
            m_Rescue.Rescued += OnMoodiRescued;
            m_Energy.StageReached -= OnEnergyStageReached;
            m_Energy.StageReached += OnEnergyStageReached;
            m_Rescue.Begin(m_WalkTrailGuidePrefab, m_SqueezeProgressVideo, Camera.main != null ? Camera.main.transform : null);
        }

        void StopSqueezeRescue()
        {
            if (m_BindVideoRoutine != null) StopCoroutine(m_BindVideoRoutine);
            m_BindVideoRoutine = null;
            if (m_Energy != null) m_Energy.StageReached -= OnEnergyStageReached;
            if (m_Rescue == null) return;
            m_Rescue.IntroFinished -= ShowWorldTutorial;
            m_Rescue.Rescued -= OnMoodiRescued;
            m_Rescue.StopAndClear();
        }

        System.Collections.IEnumerator BindSqueezeVideoWhenSoftAppears()
        {
            var deadline = Time.time + 10f;
            while (Time.time < deadline)
            {
                var capsule = FindActiveCalibratedNatureCapsule(); // finds the calibrated Candy or Nature soft
                if (capsule != null)
                {
                    m_SqueezeProgressVideo?.BindToSoft(capsule);
                    if (m_FloatingProgress == null)
                    {
                        var progress = new GameObject("Floating Squeeze Progress", typeof(RectTransform));
                        progress.transform.SetParent(transform, false);
                        m_FloatingProgress = progress.AddComponent<FloatingSqueezeProgress>();
                    }
                    m_FloatingProgress.ConfigureFonts(m_ChineseFont,m_EnglishFont);
                    m_FloatingProgress.SetProgress(0f);
                    m_FloatingProgress.Bind(capsule.transform, Camera.main, m_NatureWorldActive);
                    break;
                }
                yield return null;
            }
            m_BindVideoRoutine = null;
        }

        void ShowWorldTutorial()
        {
            if (m_TutorialShown || this == null || !isActiveAndEnabled) return;
            m_TutorialShown = true;
            m_Help?.OnIntroFinished();   // first visit: the "?" pulses twice; no card
        }

        void OnEnergyStageReached(int stage)
        {
            if (m_Rescue != null) m_Rescue.NotifyStage(stage);
        }

        void OnMoodiRescued(Vector3 position)
        {
            m_Journey?.Rescued();
            m_WalkTrailGuide?.AppearFromRescue(position);
        }

        void RequestExit()
        {
            if (m_Journey == null) { m_ExitAction?.Invoke(); return; }
            if (!m_Journey.RequestExit()) return;
            if (m_Journey.Stage == WorldJourneyStage.Bubble) m_BubbleWorld.Leave(LeaveTarget.Exit);
            else m_ExitAction?.Invoke();
        }

        void OnCapsulePinched(ChocolateCapsuleInteraction capsule, Vector3 position)
        {
            if(this==null)return;
            if (m_Journey != null && m_Journey.Stage == WorldJourneyStage.Bubble) return;
            if ((m_Journey != null && !m_Journey.CanGrowInRoom) || !m_Running || capsule == null || !capsule.gameObject.activeInHierarchy)
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
            m_SqueezeProgressVideo?.BindToSoft(capsule);
            m_ProgressController.SetOrigin(visualCenter);
            if (m_NatureWorldActive)
            {
                m_LastNatureSoftOrigin = ResolveCurrentCapsuleBottom(capsule, visualCenter);
                m_NatureIvyGrowth?.SetNatureSoftOrigin(m_LastNatureSoftOrigin);
                m_NatureLeaves?.SetOrigin(visualCenter);
            }
            m_FloatingProgress?.NotifyPressed();
            if(m_SqueezeModes!=null)m_SqueezeModes.AcceptPinch(capsule);
            else m_Energy.RegisterPinch();
            if (m_Rescue != null) m_Rescue.NotifyPinch();
            SpawnSoftSqueezeFeedback(visualCenter);
            m_BurstController.Burst(visualCenter, capsule.transform);
        }

        /// <summary>
        /// Shared tactile-looking feedback for each accepted CandySoft or NatureSoft squeeze.
        /// The input component's existing pinch cooldown ensures this is not emitted every frame.
        /// </summary>
        public static GameObject SpawnSoftSqueezeFeedback(Vector3 center)
        {
            var burst = ParticleBurstEffect.Spawn(center);
            MoodiumRuntimeObjectRegistry.RegisterCreative(burst);
            return burst;
        }

        public static bool IsValidSqueezeSource(ChocolateCapsuleInteraction capsule)
        {
            if(capsule==null||!capsule.gameObject.activeInHierarchy||!capsule.InteractionEnabled)return false;
            if(capsule.GetComponentInParent<ARTrackedObject>()!=null||capsule.GetComponentInParent<ARTrackedImage>()!=null||
                capsule.GetComponentInParent<CalibratedSensoryBagPresentation>()!=null)return true;
            var follower=capsule.GetComponent<TrackedObjectPoseFollower>();
            return follower!=null&&follower.Anchor!=null&&(follower.Anchor.GetComponentInParent<ARTrackedObject>()!=null||
                follower.Anchor.GetComponentInParent<ARTrackedImage>()!=null);
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

        static ChocolateCapsuleInteraction FindActiveCalibratedNatureCapsule()
        {
            var capsules = FindObjectsByType<ChocolateCapsuleInteraction>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (var capsule in capsules)
            {
                if (capsule != null && capsule.gameObject.activeInHierarchy &&
                    capsule.GetComponentInParent<CalibratedSensoryBagPresentation>() != null)
                    return capsule;
            }
            return null;
        }

        static Vector3 ResolveCurrentCapsuleBottom(ChocolateCapsuleInteraction capsule, Vector3 fallback)
        {
            var primaryCollider = capsule.GetComponent<Collider>();
            if (primaryCollider != null && primaryCollider.enabled)
                return new Vector3(primaryCollider.bounds.center.x, primaryCollider.bounds.min.y, primaryCollider.bounds.center.z);
            var renderers = capsule.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return fallback;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        public void ConfigureNatureForestTrees(GameObject[] treePrefabs)
        {
            m_NatureTreePrefabs = treePrefabs;
        }

        public void ConfigureNatureFootstepGrass(GameObject grassPrefab)
        {
            m_NatureFootstepGrassPrefab = grassPrefab;
        }

        public void ConfigureNatureFootstepAlternate(GameObject alternatePrefab)
        {
            m_NatureFootstepAlternatePrefab = alternatePrefab;
        }

        public void ConfigureCandyFootstepBean(GameObject beanPrefab)
        {
            m_CandyFootstepBeanPrefab = beanPrefab;
        }

        public void ConfigureNatureCompletionPanorama(Texture2D panoramaTexture)
        {
            m_NatureCompletionPanoramaTexture = panoramaTexture;
        }

        public void ConfigureCandyCompletionPanorama(Texture2D panoramaTexture)
        {
            m_CandyCompletionPanoramaTexture = panoramaTexture;
        }

        public void ConfigureCandyCompletionHouse(GameObject housePrefab)
        {
            m_CandyCompletionHousePrefab = housePrefab;
        }

        public void ConfigureCandyLittleCompletionHouse(GameObject housePrefab)
        {
            m_CandyLittleCompletionHousePrefab = housePrefab;
        }

        public void ConfigureNatureBurstPrefabs(GameObject[] prefabs)
        {
            m_NatureBurstPrefabs = prefabs;
            m_BurstController?.ConfigureNaturePrefabs(prefabs);
        }

        public void ConfigureCandyBurstPrefabs(GameObject[] prefabs)
        {
            m_CandyBurstPrefabs = prefabs;
        }

        public void ConfigureNatureBurstPrefabSource(bool useCandyPoolInNatureForDebug)
        {
            m_UseCandyBurstPrefabsInNatureForDebug = useCandyPoolInNatureForDebug;
        }

        /// <summary>Configures the inspector-controlled one-shot pure Nature burst diagnostic.</summary>
        public void ConfigureNatureBurstDebug(bool forceOnStart, NatureBurstDebugPrefabSelection prefab)
        {
            m_ForceNatureBurst = forceOnStart;
            m_ForceNatureBurstPrefab = prefab;
        }

        public void ConfigureNatureIvy(GameObject generatorPrefab, Material growMaterial)
        {
            m_NatureIvyGeneratorPrefab = generatorPrefab;
            m_NatureIvyGrowMaterial = growMaterial;
        }

        void OnEnergyChanged(float energy, float normalizedProgress)
        {
            if (m_Journey != null && !m_Journey.CanGrowInRoom) return;
            if (normalizedProgress >= 1f)
            {
                m_Journey?.EnergyFull(m_Rescue != null);
                if (!m_BubbleEntryScheduled) { m_BubbleEntryScheduled = true; StartCoroutine(SummonBubbleAtEnergyFull()); }
            }
            if(!m_ExploreGuideShown&&normalizedProgress>=1f&&m_Help!=null)
            {
                m_ExploreGuideShown=true;
                // Quiet 100% hint: dot on the "?" + one line for 6 s, after Moodi's thank-you (or 2.5 s without it).
                m_Help.OnEnergyFull(m_Rescue != null ? RescueExploreDelay : 2.5f);
            }
            m_WalkTrailGuide?.SetProgress(normalizedProgress);
            if (m_Rescue != null) m_Rescue.NotifyProgress(normalizedProgress);
            if(Time.unscaledTime>=m_NextEnergyUIUpdate||normalizedProgress>=1f||normalizedProgress<=0)
            {
                m_NextEnergyUIUpdate=Time.unscaledTime+.1f;
                m_EnergyUI.SetProgress(energy, normalizedProgress);
                m_FloatingProgress?.SetProgress(normalizedProgress);
                m_ProgressController.SetProgress(energy, normalizedProgress);
                m_SqueezeProgressVideo?.SetProgress(normalizedProgress);
            }
            m_EnergyUI?.SetReleaseAvailable(EnergyReleasePolicy.ShouldShowToggle(normalizedProgress));
            if (m_NatureWorldActive)
            {
                m_NatureIvyGrowth?.SetProgress(normalizedProgress);
                m_NatureForestGrowth.SetProgress(normalizedProgress);
                m_NatureFootstepGrass?.SetProgress(normalizedProgress);
                m_NatureCompletionPanorama?.SetProgress(normalizedProgress);
                m_BubbleWorld?.SetProgress(normalizedProgress);
                m_NatureLeaves?.UpdateLeafIntensity(normalizedProgress);
                if (m_LastNatureProgress < 1f && normalizedProgress >= 1f && m_LastNatureSoftOrigin != Vector3.zero)
                    m_NatureIvyGrowth?.TryStartCompletionBurst(m_LastNatureSoftOrigin);
                m_LastNatureProgress = normalizedProgress;
            }
            else
            {
                m_NatureFootstepGrass?.SetProgress(normalizedProgress);
                m_CandyCompletionHouse?.SetProgress(normalizedProgress);
                m_NatureCompletionPanorama?.SetProgress(normalizedProgress);
                m_BubbleWorld?.SetProgress(normalizedProgress);
            }
        }

        void OnFirstRing() => m_Help?.OnFirstRingShown();
        void OnRingHint(string id, Vector3 point) => m_WalkTrailGuide?.PresentRing(id, point);
        System.Collections.IEnumerator SummonBubbleAtEnergyFull()
        {
            // Energy 100% is now the bubble-world entry point. Wait only for the
            // current voice line and for the rescue flow to finish changing stages.
            var timeout = Time.unscaledTime + 12f;
            while (m_Running && (MoodiVoice.IsAnyoneSpeaking || m_Journey == null || m_Journey.Stage != WorldJourneyStage.Explore) && Time.unscaledTime < timeout)
                yield return null;
            if (!m_Running || m_BubbleWorld == null || m_Journey == null || m_Journey.Stage != WorldJourneyStage.Explore) yield break;
            var viewer = Camera.main != null ? Camera.main.transform : transform;
            var bagObject = FindActiveCalibratedNatureCapsule();
            var calibrated = bagObject != null ? bagObject.GetComponentInParent<CalibratedSensoryBagPresentation>() : null;
            var hover = calibrated != null ? calibrated.CenterWorldPosition + Vector3.up * .3f : viewer.position + viewer.forward * .6f - Vector3.up * .25f;
            m_BubbleWorld.Summon(hover - Vector3.up * .35f, hover);
            Debug.Log("[World Journey] Bubble summoned immediately at energy 100%.");
        }
        void OnGardenDiscovered(Vector3 headSnapshot)
        {
            var occupied = new System.Collections.Generic.List<Vector3>();
            if (m_WalkTrailGuide != null) occupied.AddRange(m_WalkTrailGuide.OccupiedGardenPositions);
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.gameObject.activeInHierarchy && (r.GetComponentInParent<Moodium.Rewards.RewardOrb>() != null ||
                    r.transform.name.Contains("Garden") || r.GetComponentInParent<Moodium.Rewards.RewardGrowIn>() != null)) occupied.Add(r.bounds.center);
            m_Rings?.SetOccupiedPositions(occupied);
            m_Rings?.OnGardenDiscovered(headSnapshot);
            StartCoroutine(SummonBubbleAfterJourneyDelay(headSnapshot));
        }

        System.Collections.IEnumerator SummonBubbleAfterJourneyDelay(Vector3 headSnapshot)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (!m_Running || m_BubbleWorld == null || m_Journey == null || m_Journey.Stage != WorldJourneyStage.Explore || MoodiVoice.IsAnyoneSpeaking) yield break;
            var viewer = Camera.main != null ? Camera.main.transform : transform;
            var bag = FindActiveCalibratedNatureCapsule();
            var hover = bag != null ? bag.GetComponentInParent<CalibratedSensoryBagPresentation>().CenterWorldPosition + Vector3.up * .3f : viewer.position + viewer.forward * .6f - Vector3.up * .25f;
            var from = hover - Vector3.up * .35f;
            m_BubbleWorld.Summon(from, hover);
            Debug.Log("[World Journey] Bubble summoned from squeeze-top fallback after garden discovery.");
        }
        void OnWalkInvited()
        {
            var bag = FindActiveCalibratedNatureCapsule();
            var calibrated = bag != null ? bag.GetComponentInParent<CalibratedSensoryBagPresentation>() : null;
            Bounds? bounds = null;
            if (calibrated != null)
            {
                var data = calibrated.CurrentCalibration;
                var center = calibrated.CenterWorldPosition;
                // Confirmed calibration transform is the supporting table plane, not the model's renderer bottom.
                bounds = new Bounds(center, new Vector3(data.Width, data.Depth, data.Height));
            }
            var obstacles = new System.Collections.Generic.List<Bounds>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (calibrated != null && r.transform.IsChildOf(calibrated.transform))) continue;
                if (r.GetComponentInParent<SqueezeTableView>() != null || r.GetComponentInParent<Moodium.Guide.WorldGuideCards>() != null ||
                    r.GetComponentInParent<Moodium.Rewards.RewardGrowIn>() != null || r.GetComponentInParent<Moodium.Rewards.RewardOrb>() != null)
                    obstacles.Add(r.bounds);
            }
            m_Rings?.OnWalkInvited(bounds, calibrated != null ? calibrated.CenterWorldPosition.y : 0f, obstacles);
        }
        void OnBubbleTouched()
        {
            if (m_Journey == null || !m_Journey.EnterBubble()) return;
            m_SqueezeProgressVideo?.SetBubbleSuspended(true);
            m_BurstController?.StopAndClear();
            foreach (var effect in FindObjectsByType<ParticleBurstEffect>(FindObjectsSortMode.None))
            {
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
            m_RevealedAt = -1f;
            m_Rings?.NotifyBubbleEntered();
            m_WalkTrailGuide?.SetSuspended(true);
            m_NatureFootstepGrass?.SetSuspended(true); m_NatureIvyGrowth?.SetSuspended(true);
            m_RainReward?.StopAndClear(); m_RewardReleased = false;
            m_EnergyUI?.SetRoomInteractionEnabled(false);
            foreach (var bracelet in FindObjectsByType<Moodium.Rewards.RewardBracelet>(FindObjectsSortMode.None)) bracelet.SetInteractionEnabled(false);
            m_EnergyUI?.SetReleaseAvailable(false); m_EnergyUI?.SetReleaseToggleState(false);
            m_SpatialTouchFeedback?.SetInteractionEnabled(false); m_HandOpenBurstGesture?.SetGestureEnabled(false);
            m_ProgressController?.SetSpatialTransformationEnabled(false);
            m_Frost?.SetSuspended(true); m_NatureLeaves?.SetSuspended(true);
            m_Help?.SetSuspended(true); m_SqueezeModes?.SetSuspended(true);
            m_RoomVisibility.Clear();
            RegisterBubbleHiddenModels();
            m_NatureFootstepGrass?.RegisterRoomVisibility(m_RoomVisibility);
            m_NatureForestGrowth?.RegisterRoomVisibility(m_RoomVisibility);
            m_CandyCompletionHouse?.RegisterRoomVisibility(m_RoomVisibility);
            m_NatureIvyGrowth?.RegisterRoomVisibility(m_RoomVisibility);
            m_WalkTrailGuide?.RegisterRoomVisibility(m_RoomVisibility);
            m_SqueezeStageRewards?.RegisterRoomVisibility(m_RoomVisibility);
            if (m_SqueezeModes?.View?.TableRoot != null) m_RoomVisibility.Register(m_SqueezeModes.View.TableRoot.gameObject);
            m_RoomVisibility.SetHidden(true);
        }

        void RegisterBubbleHiddenModels()
        {
            // These models are not owned by the room-effect controllers below, so
            // register them explicitly before the bubble transition starts. The
            // visibility service snapshots their original active state and restores
            // it correctly when the user returns to the room.
            foreach (var presentation in FindObjectsByType<CalibratedSensoryBagPresentation>(FindObjectsSortMode.None))
                if (presentation != null)
                    m_RoomVisibility.Register(presentation.gameObject);

            foreach (var moodi in FindObjectsByType<MoodiCompanionAnimator>(FindObjectsSortMode.None))
                if (moodi != null)
                    m_RoomVisibility.Register(moodi.gameObject);
        }
        void OnBubbleRevealed()
        {
            m_RevealedAt = Time.time;
        }
        // The world-specific narration is the welcome; the former F3 follow-up is no longer played.
        void OnBubbleLayerRevealed(string folder) { if (m_Running) m_Narrator?.PlayIntro(folder); }
        void OnBubbleWandered() { m_Narrator?.Stop(); m_WalkTrailGuide?.Say("F5"); }
        void OnBubbleLeft(LeaveTarget target)
        {
            m_Narrator?.Stop();
            if (!m_Running) return;
            if (target == LeaveTarget.Exit) { m_Journey.End(); m_ExitAction?.Invoke(); return; }
            // Returning to the room keeps the same small-bubble entry available.
            // Reopen it after the room visibility transition so this can repeat.
            m_ReopenBubbleEntry = true;
            m_Journey.Returned();
            if (m_Journey.Stage == WorldJourneyStage.Ending) { m_ExitAction?.Invoke(); return; }
            m_RoomVisibility.SetHidden(false);
            StartCoroutine(CompleteRoomRestore());
        }
        System.Collections.IEnumerator CompleteRoomRestore()
        {
            while (m_RoomVisibility != null && m_RoomVisibility.IsTransitioning) yield return null;
            if (!m_Running || m_Journey == null || m_Journey.Stage != WorldJourneyStage.Explore) yield break;
            m_SqueezeProgressVideo?.SetBubbleSuspended(false);
            m_NatureFootstepGrass?.SetSuspended(false); m_NatureIvyGrowth?.SetSuspended(false); m_WalkTrailGuide?.SetSuspended(false);
            m_Help?.SetSuspended(false); m_SqueezeModes?.SetSuspended(false);
            m_SpatialTouchFeedback?.SetInteractionEnabled(true); m_HandOpenBurstGesture?.SetGestureEnabled(true);
            m_ProgressController?.SetSpatialTransformationEnabled(!m_NatureWorldActive);
            m_NatureLeaves?.SetSuspended(false); m_Frost?.SetSuspended(false);
            m_EnergyUI?.SetRoomInteractionEnabled(true);
            foreach (var bracelet in FindObjectsByType<Moodium.Rewards.RewardBracelet>(FindObjectsSortMode.None)) bracelet.SetInteractionEnabled(true);
            m_EnergyUI?.SetReleaseAvailable(true); m_Rings?.NotifyBubbleReturned();
            m_WalkTrailGuide?.Say("F7");
            if (m_ReopenBubbleEntry)
            {
                m_ReopenBubbleEntry = false;
                m_BubbleWorld?.ResummonAtLastEntry();
            }
        }

        void ReleaseAccumulatedEnergy()
        {
            if ((m_Journey != null && !m_Journey.CanGrowInRoom) || !m_Running || m_Energy == null ||
                !EnergyReleasePolicy.ShouldShowToggle(m_Energy.NormalizedEnergy))
                return;

            if (m_RainReward == null) return;
            if (m_RainReward.IsPlaying)
            {
                m_RainReward.StopAndClear();
                m_EnergyUI?.SetReleaseToggleState(false);
                Debug.Log($"[Moodium Energy Release] { (m_NatureWorldActive ? "Nature" : "Candy") } reward rain stopped.");
            }
            else
            {
                m_RainReward.PlayReward();
                m_EnergyUI?.SetReleaseToggleState(true);
                Debug.Log($"[Moodium Energy Release] { (m_NatureWorldActive ? "Nature" : "Candy") } reward rain started.");
            }
        }

    }

    public static class EnergyReleasePolicy
    {
        public static bool ShouldOffer(float normalizedProgress, bool hasReleased) =>
            normalizedProgress >= .999f && !hasReleased;

        public static bool ShouldShowToggle(float normalizedProgress) => normalizedProgress >= .999f;
    }
}
