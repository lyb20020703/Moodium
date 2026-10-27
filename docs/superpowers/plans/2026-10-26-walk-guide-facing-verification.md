# Walking companion viewer-facing fix — 2026-10-26

Root cause: MoodiCompanionAnimator.UpdateFacing prioritized horizontal travel velocity, so a guide travelling away naturally turned its back to the viewer.

Change: per-instance SetFacingViewer override, assigned only by WalkTrailGuideController.BeginSession. Existing measured facial direction and yaw smoothing rotate only the animated child around its body center. Route, guide height, movement-based Walk animation, and other prefab instances are unchanged.

TDD: two real prefab regression cases (moving and stationary) failed before the override existed; both passed afterwards. Each also moves the viewer to the opposite side and checks root pose is unchanged. Focused suite 33/35; two prior MoodiBreathingFloatTests position assertions fail.

Full run: 473 total, 441 passed, 32 failed. Includes the previous 26 failures, five CalibrationUIv2Tests from concurrent calibration UI work (outside this fix), and ARMeshManager test modal timeout. No new facing-test failures. Test-generated MoodCheck prefab serialization restored; concurrent calibration/font changes preserved. Not pushed.

Failing cases in full run:

- `CalibratedSensoryBagPresentationTests.VisualFit_MapsFrameWidthAndHeightOntoPrefabTablePlaneXAndZ`
- `CalibratedSensoryBagPresentationTests.VisualRoot_FollowsStickerOrientation_WhileFittingBagXZ`
- `CalibrationUIv2Tests.FrameIsDrawnWithSpritesAndEdgesCanBeGrabbedAlongTheirWholeLength`
- `CalibrationUIv2Tests.HeightControlAndTipCardSitOutsideTheFrameAboveTheTable`
- `CalibrationUIv2Tests.HeightControlShowsTheLiveTableHeight`
- `CalibrationUIv2Tests.SpritesAreImportedAtRealSize`
- `CalibrationUIv2Tests.TipCardUsesTheWorldPictogramAndLanguage`
- `CandyBurstControllerTests.NatureBurstWrapper_DisablesLegacyMaterialAndBoundsCompatibility`
- `CandyWorldProgressControllerTests.DisabledSpatialTransformationDoesNotStartCandyMeshOverlay`
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

