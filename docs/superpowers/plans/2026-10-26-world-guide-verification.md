# World Guide integration verification — 2026-10-26

## Delivered

- Installed all 34 original tutorial/exploration/button PNGs, importer settings, runtime scripts and editor checks. Byte comparison against the original archive passed for every PNG; patch 01 updates design/tool sources only. Exporter helper render_component.js is now archived alongside the patched exporter.
- CandyWorldManager shows four tutorial cards on entry, schedules the world/language exploration card once on first 100% energy (2.5s delay), and cancels/hides everything on exit. An unfinished tutorial is allowed to finish rather than being silently replaced.
- Native pinch-drag and Editor mouse-drag are forwarded through MoodiumSpatialInteractionController; separate button colliders remain in front, canceled swipes snap back, closing disables interaction immediately, rapid page requests are gated during transitions. Existing scoring, video mute, completion rewards and walking guide are not replaced.
- Walking invitation retains the user's full-energy +3s policy, independent of explore-card display. The two may coexist; no hidden postponement of walking.
- Circular squeeze video stores tracked forward as well as surface normal. Push hands animate/offset in the tracked frame while preserving the original prefab's child pose. No global extra 90deg correction or change to bag/model root transforms.
- Cards use shared sprite leases. Detach renderers before final-owner Sprite/Texture unload; closing one instance cannot unload another instance's card. Actual device memory reclamation requires profiling because Editor import machinery can retain native texture objects.

## Evidence

- Orientation RED: all three newly added pose cases failed against original code (flat yaw90, pitched/rolled yaw, world-axis hand placement). They pass after the scoped fix. Video audio-output None tests remain passing.
- Tutorial import RED: supplied tests failed compilation with missing Moodium.Guide before scripts/assets were installed.
- Final targeted coverage: 40 tests passing in final full run; resource-last-owner fixture selects an unleased card because unrelated failing legacy tests can leave live managers behind. Earlier isolated 40/40 target run also passed; the fixture correction avoids treating a genuinely shared card as last-owned.
- Final full EditMode run: **430/456 passed, 26 failed**, 2026-10-26 05:17:51–05:19:33 UTC (13:17:51–13:19:33 Shanghai). Failure names match the prior 26-test baseline; no new failures. Raw result: /var/folders/r0/92tsbtv569lfgkrwytf1z9rr0000gn/T/Moodium-Wrist-AllTests.xml.
- Fresh Runtime/Editor build: 0 errors, 2 pre-existing warnings; /tmp/world-guide-delivery-build.log.
- Rendered 20 combinations (2 worlds × 2 languages × 5 card states) in Unity via Moodium > Preview World Guide Cards. Inspected Candy Chinese first tutorial, Nature English last tutorial and both exploration layouts; rounded colored buttons, labels and page dots align. Outputs: WorldGuide-<candy|nature>-<zh|en>-<0..4>.png in the system temp directory. Not headset captures.
- One independent final review found resource lifetime handling; shared ownership/unload added. No remaining actionable findings or deferred minors.
- Restored automatic MoodCheck prefab regeneration, dynamic Inter font atlas changes and a serializer-only scene field reorder from the test runs. Preserved existing three-second walking changes and the source-archive patch. No commit or GitHub push.

## Decisions carried forward

1. Work in the current live checkout using the established Native workflow, preserving unrelated edits. Cost: no isolated-branch barrier, mitigated by scoped diffs and restoring test side effects.
2. Keep walking at +3s, explore at +2.5s; overlap is possible. Cost: two simultaneous guides until the user closes exploration.
3. If tutorial is still open at completion, defer explore until it closes. Cost: that exploration card can appear later than +2.5s.

## Device verification still required

- Real entry → tutorial next/swipe/skip → energy100 → explore → exit/reentry in both worlds and languages; manager hook chain is code-reviewed, not end-to-end headset-tested.
- Native gaze-and-pinch button routing and drag comfort, upright card recenter at 50deg and legibility at .9m.
- Sticker/model yaw90 and tilted pitch/roll in the headset: video heading and hand prompt symmetry, with original local hand pose retained.
- Native transparent sorting and texture memory release/performance over repeated entries.
- Walking invite at +3s still requires a reconstructed clear floor/route; its timer eligibility does not bypass floor safety.

## Existing full-run failures

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

