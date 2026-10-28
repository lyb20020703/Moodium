# Bubble World Finale implementation

Approved design: ../specs/2026-10-28-bubble-world-finale-design.md

## Implemented

1. Journey Rescue → Explore ⇄ Bubble → Ending, exit deduplication and deferred bubble exit.
2. Room visibility registration, 20 entries per frame, retained original active/scale/sprite colors, reversible 0.6 s hide and 0.8 s restoration. Sprite transparency and opaque scale fallback. Resume gameplay after restoration finishes.
3. Walk invitation waits for palm Landed → Free + 2 s, or full energy + 20 s while not on palm. Invitation event independent of finding reconstructed ground, enabling seated table access.
4. Ring wiring uses calibration table plane and calibrated bag bounds, scene UI/reward obstacle bounds, discovery-time head snapshot, recent grown step positions, existing garden and reward occupancy, and reconstructed spatial-mesh ray hits.
5. Bubble Touched/Revealed/Left/Wandered events wired; first F3, squeeze F4, wander F5, session F6 and return F7. F1/F1t move Moodi above the first ring (floor orbit); F2 accompanies summon.
6. Room suspension covers walking, palm companion, squeeze scoring/UI callbacks, ivy delayed generation, particles, spatial touch/open-hand/release, bracelet inspection and wrist glow; collected gems and wrist exit retained.
7. Help caption bilingual text plus renewed first-ring dot. StopExperience reverses transient suspension and clears lifecycle subscriptions.
8. Regression tests for stage routing, invite thresholds, interrupted visibility, original inactive objects, clear during multi-frame hiding and bracelet gate. Legacy 3-second invite test updated.
9. StartExperience handles null WorldId using string.Equals (two older energy tests hit this initialization bug).

## Verification

Runtime and Editor assemblies independently compiled using Unity bundled Roslyn and current Editor response files, output under /tmp only. No additional Unity batch instance started after the user began menu-driven testing. Latest user-run full suite: 581 total / 556 passed / 25 failed; new Journey tests passed. Both StartExperience NREs disappeared and now expose the existing ParticleBurstEffect Destroy-in-EditMode error. Per user instruction leave that old feature unchanged. Full suite is not green; no further scoped implementation work is pending. Vision Pro device validation remains an acceptance step. See ../../verification/2026-10-28-bubble-world-finale.md for actual parameters and limits of verification.

No commits, resets or unrelated file cleanup.
