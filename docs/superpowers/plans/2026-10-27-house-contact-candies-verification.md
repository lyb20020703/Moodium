# House-contact candy verification — 2026-10-27

Implemented the approved three-prop interaction through the shared CandyCompletionHouseController.SpawnHouse path. Contact event is driven by the existing mesh-surface solver, emits from surface+1.5cm normal, at most once per .8seconds per prop. No root pose/energy/shader/audio change. Manager passes its existing CandyBurstController; contact requests force the Candy pool, preserve projectile collision ignores and video avoidance.

TDD: the first contact-origin test observed RED (missing event), then passed. Real CandyHouse/CandyLittleHouse/Honey Jar deformation+emission tests pass; HoneyJar component is added as in runtime SpawnHouse, not required on its prefab. Added tilted/scaled two-finger held-contact and release cases. Sloped six-axis contact queries can land a few millimetres sideways; test uses1cm proximity and a strict.015m signed-normal offset instead of assuming perpendicular nearest projection.

Final full EditMode run14:10:39–14:11:59 CST:497total,471passed,26failed. All8 added cases pass; baseline failures remain below. Managed Editor build passed with0errors; git diff --check clean. Restored only test-generated MoodCheck prefabs and Inter font. Other pending voice/walk/material edits preserved; no commit/push.

Independent final review: no blocking regression. Ruling:32 is an admission budget for new contact bursts counting currently live+reserved contacts; legacy CandySoft requests may subsequently exceed it, intentionally preserving the original CandySoft behavior. Cost: not a hard global cap when both systems emit concurrently.

Deferred test limitations: coroutine integration for simultaneous props/source destruction/stop-restart and three-prop spawn-to-emitter wiring are inspected but not exercised end-to-end in PlayMode. An already accepted small batch can finish if a prop is merely disabled (destroyed sources stop); world shutdown cancels the emitter. Device appearance/performance/physical hand feel need headset QA. No review claim covers unrelated pending work.

Full-suite existing failures:

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

