# Sticker invitation voice integration

Read source README and both incremental diffs from Moodium-MoodiStickerVoice.zip. Existing walking voice dependency is present. Applied incremental edits rather than overwriting controller files; retained concurrent walking/candy material work.

Imported six WAVs (S1c/S1n/S2, Chinese/English) with unique GUIDs and matching existing mono/decompressed/preloaded voice import settings. Source recordings outside the Unity folder were not imported. Invitations use opening-session language, .6 second delay, global4s/world20s cooldowns, recommendation override, and delayed/queued speech cancellation on disable/Stop.

Added supplied invite-selection/cooldown tests; updated voice manifest count to28, and added language-override and delayed-cancellation tests. Pending Unity execution: computer-use cannot connect because the Mac is locked. Therefore RED/GREEN runtime tests and device audio balance are NOT verified. Managed project compilation succeeded with zero errors; this alone does not establish Unity asset import or playback success. New test file may not yet be included by Unity's generated project until refresh.

Next: user unlocks Mac; refresh Unity/import assets, run MoodiVoiceTests, MoodiStickerInviteTests, StickerWorldSelectionControllerTests plus full suite, examine results, and check tracking-loss/mood-panel-hide cancellation. No commit or push requested.

## Resumed verification — 2026-10-27

Mac unlocked and Unity project reopened. Added Moodium/Validate Sticker Voice menu. Unity imported all six clips; voice manifest test validated all28 IDs in bothlanguages, short duration and mono audio. Session-language override, delayed cancellation, invite selection and cooldown tests pass. EditMode tests explicitly invoke Awake/OnDisable because ordinary MonoBehaviours do not receive these lifecycle callbacks in EditMode; the initial test failures were harness initialization, not production cancellation defects.

Focused run 2026-10-27 12:37 CST:19 total,16passed,3 prior StickerWorldSelectionController failures below. Full run12:39–12:40 CST:489total,463passed,26failed matching the existing baseline. All MoodiVoiceTests and MoodiStickerInviteTests passed. Managed Editor build:156warnings,0errors; git diff --check clean. Restored only test-generated MoodCheck prefab/dynamic Inter font changes, preserving other pending walking/Candy material work. No GitHub push. Headset spatial balance and actual tracking-loss behavior remain device QA, not claimed as tested.

Full-suite failing cases:

- `CalibratedSensoryBagPresentationTests.VisualFit_MapsFrameWidthAndHeightOntoPrefabTablePlaneXAndZ`
- `CalibratedSensoryBagPresentationTests.VisualRoot_FollowsStickerOrientation_WhileFittingBagXZ`
- `CandyBurstControllerTests.NatureBurstWrapper_DisablesLegacyMaterialAndBoundsCompatibility`
- `DualTrackingRealityFlowTests.ImageTrackedCapsuleIncreasesSharedCandyEnergy`
- `ImageTrackingCapsuleSpawnerTests.SceneUsesTenCentimetreTestImageLibrary`
- `MoodCheckFlowIntegrationTests.ExitWithPreSampleOpensPostAndSkipClosesVisit`
- `MoodiBreathingFloatTests.CandyWorldUI_MoodiMeshy_HasBreathingFloat`
- `MoodiBreathingFloatTests.NatureWorldUI_MoodiMeshy_HasBreathingFloat`
- `MoodiumOpeningPersistenceTests.OpeningPrefab_KeepsAllRuntimeIntroAssetsAssigned`
- `NatureForestGrowthControllerTests.StopAndClearRemovesSpawnedForestTrees`
- `NatureLeafParticleControllerTests.SoftSqueezeFeedbackCreatesTheExistingWhiteCircularBurstAtTheSoftCenter`
- `NatureLeafParticleControllerTests.WhiteBurstCircleTextureHasTransparentCornersAndAnOpaqueCenter`
- `OpeningGestureGuideTests.OpeningPrefabShowsGestureGuideThreeSecondsAfterCandyInFrontOfCandy`
- `PalmPressWorldSpaceContactTests.DetachedTrackedCapsuleStillIncreasesCandyEnergy`
- `PortalFallingBagControllerTests.PortalPrefabContainsConfiguredBagRainAndInvisibleChamber`
- `PortalFallingBagControllerTests.PortalPrefabIncludesCandyBagInTheFiftyBagLibrary`
- `PortalFallingBagControllerTests.PortalPrefabProvidesFloorAndCapacityForPersistentBagStack`
- `PortalFallingBagControllerTests.PortalPrefabUsesLargerBalancedBagsForTheFullStack`
- `PortalFallingBagControllerTests.SpawnedBagReceivesPhysicsAndStaysInsideSpawnBounds`
- `RealityModeTableVisibilityTests.SelectingRealityCandyWorldDoesNotCreateSpatialTable`
- `RealityWorldModelCarouselTests.RealityCarouselBuildsOnlyCandyAndNatureWithCandyInitiallySelected`
- `RealityWorldModelCarouselTests.RealityCarouselNotifiesCandyAsInitialPreview`
- `SensoryBagCalibrationRectangleTests.ProjectWorldPointToAnchorPlane_DiscardsLocalDepth`
- `StickerWorldSelectionControllerTests.MoodEntry_ChangesSummaryAndRaisesRequest`
- `StickerWorldSelectionControllerTests.RecommendationTag_AttachesOnlyToMatchingEntryWithoutDisablingButtons`
- `StickerWorldSelectionControllerTests.WorldEntry_UsesGlassSliderWithArrowInsteadOfTextOnlyButton`

