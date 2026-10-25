# Mood Band v2 verification — 2026-10-25

## Result

- Runtime and Editor compilation passed, 0 errors. Existing third-party warnings remain.
- Wrist-specific Unity EditMode tests: 25/25 passed, covering localized copy, raised wrist/dorsal gating, hidden colliders, tracking loss, two-press exit, stage resets, English title and release layout in both languages/toggle states.
- Full Unity EditMode run: 399/425 passed, 26 failed. No wrist-specific failure. Raw report: /var/folders/r0/92tsbtv569lfgkrwytf1z9rr0000gn/T/Moodium-Wrist-AllTests.xml.
- RED→GREEN: missing helper API; release label x86 vs knob x20 overlapping; English title 2 lines vs required 1.
- Rendered 10 isolated Candy/Nature ambient/open/full/confirm state previews in Chinese and English. Temporary images MoodBand-<state>-<cn|en>.png. Unity renders, NOT device captures.
- Independent review: important release layout overlap fixed; optional mesh-buffer reuse deferred until profiling.
- Full suite regenerated MoodCheck prefabs and changed PlayerSettings. Restored these test side effects to the clean baseline; preserved the user's pre-existing URP edit. Font changes retain new wrist text glyphs.

## Implementation choices

- Adapt current script in place, preserving script meta and manager API. No legacy orb/reward flow; sockets remain extension points.
- Head direction + raised wrist + dorsal orientation, never raw eye gaze. Defaults: .65 m reach, 45°/55° head cone, .45/.55 m below eyes, dorsal dot .25/.1. Device comfort may require tuning.
- Subtitle shows energy stage N/4 until actual gems are collected. This intentionally differs from the archive's older reward assumptions.
- Native hover and existing spatial button input remain; hidden/lost-pose/disabled cards reject presses. Exit needs confirmation within 2 s.
- Changes stay in the existing backed-up checkout; careful scoped diffs compensate for not using an isolated worktree.

## Device checklist — not yet performed

1. Band fit and real hand occlusion; sockets dorsal. Adjust Flip Dorsal/Wrist Position Offset if necessary.
2. Raised wrist opens comfortably, lowered/turned/lost wrist disables input immediately; no raw eye tracking permission.
3. Native look-and-pinch selection, exit confirmation timeout/cancellation, and editor mouse interaction.
4. At 100%, release toggle still controls existing rain. No duplicate reward sequence.
5. Chinese/English legibility, native transparent sorting and black-flash check.
6. Profile mesh replication/allocations during continuous breathing. Reusing mesh buffers is a deferred minor optimization.

## Full-run failures outside wrist scope

- CalibratedSensoryBagPresentationTests.VisualFit_MapsFrameWidthAndHeightOntoPrefabTablePlaneXAndZ
- CalibratedSensoryBagPresentationTests.VisualRoot_FollowsStickerOrientation_WhileFittingBagXZ
- CandyBurstControllerTests.NatureBurstWrapper_DisablesLegacyMaterialAndBoundsCompatibility
- DualTrackingRealityFlowTests.ImageTrackedCapsuleIncreasesSharedCandyEnergy
- ImageTrackingCapsuleSpawnerTests.SceneUsesTenCentimetreTestImageLibrary
- MoodCheckFlowIntegrationTests.ExitWithPreSampleOpensPostAndSkipClosesVisit
- MoodiBreathingFloatTests.CandyWorldUI_MoodiMeshy_HasBreathingFloat
- MoodiBreathingFloatTests.NatureWorldUI_MoodiMeshy_HasBreathingFloat
- MoodiumOpeningPersistenceTests.OpeningPrefab_KeepsAllRuntimeIntroAssetsAssigned
- NatureForestGrowthControllerTests.StopAndClearRemovesSpawnedForestTrees
- NatureLeafParticleControllerTests.SoftSqueezeFeedbackCreatesTheExistingWhiteCircularBurstAtTheSoftCenter
- NatureLeafParticleControllerTests.WhiteBurstCircleTextureHasTransparentCornersAndAnOpaqueCenter
- OpeningGestureGuideTests.OpeningPrefabShowsGestureGuideThreeSecondsAfterCandyInFrontOfCandy
- PalmPressWorldSpaceContactTests.DetachedTrackedCapsuleStillIncreasesCandyEnergy
- PortalFallingBagControllerTests.PortalPrefabContainsConfiguredBagRainAndInvisibleChamber
- PortalFallingBagControllerTests.PortalPrefabIncludesCandyBagInTheFiftyBagLibrary
- PortalFallingBagControllerTests.PortalPrefabProvidesFloorAndCapacityForPersistentBagStack
- PortalFallingBagControllerTests.PortalPrefabUsesLargerBalancedBagsForTheFullStack
- PortalFallingBagControllerTests.SpawnedBagReceivesPhysicsAndStaysInsideSpawnBounds
- RealityModeTableVisibilityTests.SelectingRealityCandyWorldDoesNotCreateSpatialTable
- RealityWorldModelCarouselTests.RealityCarouselBuildsOnlyCandyAndNatureWithCandyInitiallySelected
- RealityWorldModelCarouselTests.RealityCarouselNotifiesCandyAsInitialPreview
- SensoryBagCalibrationRectangleTests.ProjectWorldPointToAnchorPlane_DiscardsLocalDepth
- StickerWorldSelectionControllerTests.MoodEntry_ChangesSummaryAndRaisesRequest
- StickerWorldSelectionControllerTests.RecommendationTag_AttachesOnlyToMatchingEntryWithoutDisablingButtons
- StickerWorldSelectionControllerTests.WorldEntry_UsesGlassSliderWithArrowInsteadOfTextOnlyButton
