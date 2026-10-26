# World Guide and tracked presentation implementation plan

> Execute inline with superpowers:executing-plans. User supplied the complete original archive and patch after requesting both fixes. Preserve current live Unity checkout, existing three-second walking change, and all unrelated edits. No commit or push.

**Goal:** Actually install the bilingual four-card tutorial and completion exploration card, and align circular squeeze video / push hand guides to the complete tracked model plane.
**Architecture:** Import 34 sprites with the supplied importer, adapt WorldGuideCards lifecycle and forward native/editor inputs through the existing interaction controller. CandyWorldManager owns tutorial/explore start and teardown. Video and hand hints follow tracked anchor orientation rather than world axes; original hand prefab local pose is preserved.
**Spec:** Complete Moodium-WorldGuide README/CONVENTIONS/tokens and patch CHANGES in /tmp/moodium-world-guide.T0lon0. User override: walking invitation after full energy 3 seconds, not 25 seconds.
**Tech:** Unity 6000.0.30f1, PolySpatial native SpriteRenderer, XR spatial pointer, Unity Input System, VideoPlayer.

## Constraints and decisions
- Tutorial 0.42m wide, .9m forward, .12m below eyes; recenter above 50deg; explore after 2.5s; current language; every entry; skip and final Start dismiss. Gameplay not paused.
- Keep walking deadline 3s independent of cards; do not silently postpone it. Cards and walking may coexist as requested timing implies.
- Patch changes only design/tool sources, never replace original 34 PNGs.
- Explore must not silently replace an unfinished tutorial; keep pending until tutorial completes, then present. Exit/reentry cancels old coroutines and pointer state.
- No raw eye tracking or custom shader. Preserve all existing world/model root transforms and original hand-model internal rotations.

## Review focus
- Repeated StartExperience, exit during fade/delay/swipe, rapid presses: no ghost UI or stale input.
- Buttons must remain ahead of swipe collider; canceled swipe must not change page; editor drag and native drag both work.
- Unload only owned sprite references after renderers are detached; two guide instances must not invalidate each other's textures.
- Tracked yaw/pitch/roll and calibration plane correction must not be lost; flat sticker yaw90 changes video heading and hand left/right placement by yaw90, without affecting video mute/progress.
- Card sprites and importer metadata must be present in builds, Chinese/English visual sizes exact.

### Task 1: Tracked presentation orientation
- [x] Add failing regression tests for video yaw preservation and hand pose following target rotation while preserving authored local pose.
- [x] Implement video frame heading and model-relative hand placement/animation, not a global additional 90deg correction.
- [x] Run orientation/video tests and compile.

### Task 2: Tutorial assets, state and input integration
- [x] Import supplied tests before runtime, observe missing namespace failure; import scripts/34 sprites, patch sources.
- [x] Add lifecycle tests for entry/skip/exit, pending explore, stale/cancelled swipes, multiple instances and sprite dimensions.
- [x] Adapt runtime lifecycle, manager hooks and native/editor pointer forwarding; keep current 3s walking policy.
- [x] Run tests and inspect rendered bilingual Candy/Nature cards and button alignment.

### Task 3: Verification and handoff
- [x] One fresh independent final review; important findings get failing tests and one fix pass.
- [x] Build/runtime/editor and full EditMode run; document named existing failures, restore test-only prefab/settings side effects.
- [x] Record device limitations and actual asset completeness, preserve all pre-existing edits. No push.
