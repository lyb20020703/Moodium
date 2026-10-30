# 全套失败逐项对比

新增失败：0。旧清单 45 项，本轮 39 项。

## 本轮不再失败

- CalibratedSensoryBagPresentationTests.VisualFit_MapsFrameWidthAndHeightOntoPrefabTablePlaneXAndZ
- CalibratedSensoryBagPresentationTests.VisualRoot_FollowsStickerOrientation_WhileFittingBagXZ
- DualTrackingRealityFlowTests.ImageTrackedCapsuleIncreasesSharedCandyEnergy
- MoodiAnimatedTests.UpgradedPrefabKeepsOriginalIdentityAndStaticBackup
- MoodiVoiceTests.TeachingLinesAreSaidOnlyOnceAndNotAfterTheUserLearned
- PalmPressWorldSpaceContactTests.DetachedTrackedCapsuleStillIncreasesCandyEnergy

## 保留的失败

- **BubbleRingTests.FeedbackUsesOneDrawMeshAndReleasesGeneratedAssets**：  Expected: 1
- **CandyBurstControllerTests.NatureBurstWrapper_DisablesLegacyMaterialAndBoundsCompatibility**：  Expected: False
- **CandyWorldProgressControllerTests.DisabledSpatialTransformationDoesNotStartCandyMeshOverlay**：Unhandled log message: '[Assert] Cancelling DisplayDialog: Hierarchy not allowed An ARMeshManager must be a child of an XROrigin.
- **ImageTrackingCapsuleSpawnerTests.SceneUsesTenCentimetreTestImageLibrary**：  Expected: 1
- **MoodCheckControllerTests.Adjust_PreservesPositionAndReturnsToDragging**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.MovingPlate_UpdatesMoodNameAndDescriptionImmediately**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.NoAvailableWorld_KeepsSampleValidWithoutRecommendation**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.Panel_IsPlacedOnceAheadAndBelowCameraUsingYawOnly**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.PostCheck_StartsAtPrePositionAndCompletesAfterComparisonContinue**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.PreCheck_TransitionsThroughConfirmationRecommendationAndCompletionOnce**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckControllerTests.SkipAndImmediateDismiss_AreIdempotentAndDoNotComplete**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckFlowIntegrationTests.ExitWithPreSampleOpensPostAndSkipClosesVisit**：  Expected: Dragging
- **MoodCheckFlowIntegrationTests.SelectedWorldStartsVisitRegardlessOfRecommendation**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodCheckFlowIntegrationTests.StickerRequest_StoresConfirmedPreSampleAndRecommendation**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **MoodiBreathingFloatTests.CandyWorldUI_MoodiMeshy_HasBreathingFloat**：  Expected: (0.00, 0.02, -0.11)
- **MoodiBreathingFloatTests.NatureWorldUI_MoodiMeshy_HasBreathingFloat**：  Expected: (0.06, 0.02, -0.09)
- **MoodiumOpeningPersistenceTests.OpeningPrefab_KeepsAllRuntimeIntroAssetsAssigned**：  Expected: 32.266666666666666d +/- 0.0001d
- **MoodiVoiceTests.EveryLineHasAChineseAndAnEnglishClip**：  Expected: 48
- **MoodiVoiceTests.FinaleTeachingAndCooldownMatchTheJourney**：  Expected: greater than or equal to 0
- **MoodiVoiceTests.HidingOrStoppingMoodiCancelsItsDelayedInvitation**：UnityEngine.MissingComponentException : There is no 'AudioSource' attached to the "Sticker Moodi" game object, but a script is trying to access it.
- **MoodiVoiceTests.RescueTeachingWaitIncludesQueuedSpeechAndStopClearsIt**：System.NullReferenceException : Object reference not set to an instance of an object
- **NatureLeafParticleControllerTests.SoftSqueezeFeedbackCreatesTheExistingWhiteCircularBurstAtTheSoftCenter**：  Expected: not null
- **NatureLeafParticleControllerTests.WhiteBurstCircleTextureHasTransparentCornersAndAnOpaqueCenter**：UnityEngine.UnityException : Texture 'Moodium Runtime White Circle' is not readable, the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings.
- **OpeningGestureGuideTests.OpeningPrefabShowsGestureGuideThreeSecondsAfterCandyInFrontOfCandy**：  Expected: same as <Gesture (UnityEngine.GameObject)>
- **PolySpatialVideoRenderTextureUpdaterTests.Updater_OnlyTransfersACreatedRenderTexture**：  Expected: True
- **PortalFallingBagControllerTests.PortalPrefabContainsConfiguredBagRainAndInvisibleChamber**：  Expected: 3
- **PortalFallingBagControllerTests.PortalPrefabIncludesCandyBagInTheFiftyBagLibrary**：  Expected: 3
- **PortalFallingBagControllerTests.PortalPrefabProvidesFloorAndCapacityForPersistentBagStack**：  Expected: 30
- **PortalFallingBagControllerTests.PortalPrefabUsesLargerBalancedBagsForTheFullStack**：  Expected: 30
- **PortalFallingBagControllerTests.SpawnedBagReceivesPhysicsAndStaysInsideSpawnBounds**：  Expected: in range (-0.28,0.28)
- **RealityModeTableVisibilityTests.SelectingRealityCandyWorldDoesNotCreateSpatialTable**：Unhandled log message: '[Error] [Moodium Object World] Missing tracked soft prefab for Candy World.'. Use UnityEngine.TestTools.LogAssert.Expect
- **RealityWorldModelCarouselTests.RealityCarouselBuildsOnlyCandyAndNatureWithCandyInitiallySelected**：  Expected string length 5 but was 6. Strings differ at index 0.
- **RealityWorldModelCarouselTests.RealityCarouselNotifiesCandyAsInitialPreview**：  Expected is <System.String[1]>, actual is <System.Collections.Generic.List`1[System.String]> with 1 elements
- **SensoryBagBranchTests.DefaultConfirmationBypassesFrameAndCompletesOnlyOnce**：  Expected: (0.00, -0.05, 0.00)
- **SensoryBagBranchTests.NoBagMoodConfirmationSkipsRecommendationAndCompletesOnce**：Unhandled log message: '[Error] Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.'. Use UnityEngine.TestTools.LogAssert.Expect
- **SensoryBagCalibrationRectangleTests.ProjectWorldPointToAnchorPlane_DiscardsLocalDepth**：  Expected: -0.0299999993f +/- 9.99999975E-05f
- **StickerWorldSelectionControllerTests.MoodEntry_ChangesSummaryAndRaisesRequest**：  Expected: 1
- **StickerWorldSelectionControllerTests.RecommendationTag_AttachesOnlyToMatchingEntryWithoutDisablingButtons**：System.NullReferenceException : Object reference not set to an instance of an object
- **StickerWorldSelectionControllerTests.WorldEntry_UsesGlassSliderWithArrowInsteadOfTextOnlyButton**：System.NullReferenceException : Object reference not set to an instance of an object
