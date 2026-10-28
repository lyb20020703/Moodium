# Bubble World Finale verification — 2026-10-28

## Implemented lifecycle

WorldJourneyDirector owns Rescue, Explore, Bubble, Ending. Rescue ends on Rescued; absent rescue uses first full energy. Energy alone does not summon a bubble. Touched enters Bubble, Left(Room) returns to Explore, Left(Exit) invokes existing exit action without restoring room. Repeated exit requests are ignored. StopExperience unsubscribes events, clears rings, cached bubble assets and transient suspension.

Walk invitation: Landed then Free for >=2 seconds, or >=20 seconds since full while Moodi has no palm control. Seated table invitation is emitted before floor/path discovery, so missing reconstructed floor cannot block it. Existing help unlock delay remains 11 seconds.

## Placement and presentation

Table ring uses CalibratedSensoryBagPresentation.CenterWorldPosition.y (the confirmed calibration support plane), with calibration Width/Height/Depth bounds; no renderer-min height fallback. Missing calibrated bag produces no ring. Obstacles are active rendered mode/table UI, guide cards and RewardGrowIn/RewardOrb bounds. Placement helper expands obstacles 0.03 m per face and tries right, left, then behind, with 0.12 m bag-edge clearance and 0.07 m radius.

Garden event carries discovery-time head position. Rings downward-raycast only generated AR spatial-mesh colliders; helper requires normal.y >=0.85 and floor >=1 m below head, choosing the lowest hit. Actual StepGrown events supply recent steps. Existing gardens, reward renderers and rings supply occupancy. Clearance 0.5 m, fallback route <=1.5 m, maximum 3 floor rings. Floor diameter 1 m; entry 0.45 m, exit 0.7 m, dwell 2 s. Table diameter 0.14 m; palm cylinder radius 0.08 m and height 0.15 m, dwell 1.5 s. Gaze/pinch and editor mouse entry retained.

First floor introduction makes Moodi circle 0.22 m around a point 0.35 m above the ring for 3 s; table introduction approaches above it. F1/F1t/F2 use the shared voice system. Ring colors and 3 s pulse are owned by BubbleRingController.

## Room hide / suspend checklist

| Content | Mechanism |
|---|---|
| Grass, candy beans | Register children of grass root; update and direct SpawnGrass blocked |
| Gardens, lollipops, walk ring/dots/cards | Register walk-root children except Moodi and voice sources |
| Forest trees | Register forest-root children |
| Candy house, little house, honey jar | Register retained instances |
| Ivy branches/leaves | Register generator hierarchy; update and delayed generation wait during suspension |
| Stage physical rewards | Register retained spawned rewards |
| Table mode/help UI | Register actual TableRoot; pause UI ticking, button callbacks, squeeze contacts/scoring |
| Guide cards | Close current docked card; block reopening while suspended |
| Bubble rings | NotifyBubbleEntered hides; NotifyBubbleReturned restores with cooldown |
| Sugar wrapper | SetSpatialTransformationEnabled(false), restore for candy world |
| Frost / leaves | StopEmittingAndClear, remember previously playing systems, replay without resetting origin/intensity |
| Candy rain | StopAndClear; no automatic replay |
| Spatial touch, open-hand burst | Disable APIs; restore after room animation |
| Palm Moodi | Disable companion input during Bubble; walking companion stays visible |
| Wrist | Release blocked, band glow clear; exit remains available |
| Gems / bracelet | Collected gems retained; InspectSlot returns empty while blocked |

RoomContentVisibility budgets 20 registered entries per frame. Original active state, local scale and sprite colors are retained through reversals. Hide lasts 0.6 s after capture batches; restore lasts 0.8 s. Sprite alpha interpolates; opaque content uses scale 1 →0.9 during hide and 0.9 →1 during restore, with inactive switch at the end. Batching can extend wall-clock completion for large lists. Clear restores captured state before releasing the registry. Gameplay suspension ends only after restore completes.

## Bubble accompaniment

F3 first reveal +1.5 s. Breath reads currently pinched bag components, drives SetBreath(0/1), default enabled; F4 once. Bubble controller owns 0.4 s growth to 1.015 /1.2 s relaxation, sky exclusion, wander hysteresis >0.8 m / <0.6 m and fade 0.4 s to 25% /0.6 s to full. Wandered invokes F5 (voice cooldown 20 s). F6 is session-once after 90 s revealed. F7 after room restoration. Return animation, home bubble and cache ownership remain in BubbleWorldController.

## Verification evidence

Latest menu-driven Unity result (2026-10-28 08:19:12–08:19:55 UTC): 581 total, 556 passed, 25 failed, 0 skipped. Relevant fixtures: BubbleRing 5/5, BubbleWorld 26/26, GuideHelp 8/8, MoodiRescue 8/8, MoodiVoice 9/9, WalkTrailGuide 6/6, WorldJourneyFinale 7/7: 69/69 passed. Full suite is not green; remaining failures include existing prefab expectations, calibration, opening/selection and EditMode particle destruction. The previous StartExperience null references no longer occur. Report: temporary Moodium-Wrist-AllTests.xml.

- Runtime Roslyn compile: exit 0, /tmp/Moodium-finale-compile.log; output /tmp/Moodium-finale-check.dll.
- Editor Roslyn compile: exit 0, /tmp/Moodium-finale-editor-compile.log; output /tmp/Moodium-finale-editor-check.dll. This is compilation, not test execution.
- Latest user-run Unity full suite: **581 total /556 passed /25 failed**. New Journey tests passed. This supersedes the earlier 578/553/25 run. Test counts and outcomes here are reported by the user from the current Unity menu run, not inferred from stale XML.
- Earlier current-session run: 578 total /553 passed /25 failed; WorldJourney/BubbleRing/BubbleWorld/MoodiVoice groups were reported passing.
- StartExperience null WorldId dereference corrected via string.Equals. Latest full-suite run confirms both energy-test NREs disappeared (DualTrackingRealityFlowTests.ImageTrackedCapsuleIncreasesSharedCandyEnergy and PalmPressWorldSpaceContactTests.DetachedTrackedCapsuleStillIncreasesCandyEnergy). They now reach the pre-existing ParticleBurstEffect Destroy-in-EditMode error; they are not counted as passing. Per user instruction this old particle feature is outside the change scope.
- Full suite remains red (25 failures); no claim of all tests passing. No expansion into unrelated legacy failures.
- Added interrupted-hide reversal, inactive-state preservation, multi-frame clear and bracelet blocking tests. Existing pure placement tests cover floor fallback and table right/left/back choices.
- No stale 07:30Z XML counted as current evidence. No additional batch Unity launched after user's instruction.
- Scoped code/interface review and targeted diff whitespace check completed; no commits or resets.
- Device validation still requires Vision Pro: real reconstruction, seated reachability, bilingual font appearance, physical hand input, visual recovery and performance.

## Subsequent ring batching review

After the reported 581/556/25 run, the ring agent merged each ring's main annulus, inward fill and four motes into one dynamic mesh and one enabled MeshRenderer. Maximum four rings: four render objects, 848 vertices and 800 triangles total. Shared 32×32 RGBA32 glow texture; StopAndClear releases the texture, per-ring meshes/materials and objects. Existing public integration interfaces, discovery head snapshot and AR spatial-mesh filtering remain present.

Current runtime and Editor Roslyn recompilation both returned exit 0 following this update. The 581/556/25 result predates this batching update; updated ring tests have compiled but have not been re-executed here. Real GPU draw calls and visual appearance remain device measurements, not verified performance claims. See ../superpowers/reports/2026-10-28-bubble-ring-render-cost.md.
