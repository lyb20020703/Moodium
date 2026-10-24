# Reward Growth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace fixed hidden world rewards and manual release with four staged orb-to-bracelet collections and automatic, visible physical reward growth.

**Architecture:** `CandyWorldManager` remains the session integration point, while a new `Moodium.Rewards` layer holds table data, placement policy, runtime sequencing, orbs, bracelet visuals, and growth animation. The sequence reserves its world placement at stage time, then runs independent bracelet and world-growth timelines so wrist tracking cannot block the reward.

**Tech Stack:** Unity 6000.0.30f1; C#; PolySpatial 2.4.3; AR Foundation 6.0.3; XR Hands 1.5.0; Unity EditMode NUnit tests.

**Spec:** `docs/superpowers/specs/2026-10-23-reward-growth-design.md`

## Global Constraints

- Put new runtime code in `Assets/Scripts/Moodium/Rewards/` under namespace `Moodium.Rewards`; use existing spatial-button/input components rather than a new input system.
- Do not alter energy calculation/thresholds, image tracking, calibration, squeeze recognition, squeeze-progress video, or create a new AR session.
- Missing optional audio, particles, materials, text/audio presenters, or reward prefabs log warnings and must not block the remaining sequence.
- Preserve existing hand-open burst, panorama/ivy/footstep completion effects; only retire old fixed-stage tree/house generation and active Release wiring.
- Add EditMode tests under `Assets/Editor/`; Unity must compile with no errors and existing tests must remain passing.
- Do not commit, reset, revert, clean, stash, or overwrite unrelated user changes.

## Review Focus

- Non-finite/empty reward table rows must be skipped safely rather than corrupting a session; Task 1 adds a table validation test.
- Stage events can arrive twice or after session stop; Task 5 tests exactly-once delivery and unsubscribe behavior.
- Wrist tracking can remain unavailable for more than 20 seconds; Task 4 tests collection timeout while Task 5 tests growth deadline remains independent.
- Spatial mesh can be partial, blocked, or lack any valid point; Task 2 tests each relaxation step and the final visibility-only fallback.
- Runtime clones may contain unsupported particles, physics, scripts, or non-uniform render bounds; Task 3 tests sanitization and bounds fitting.

## File Structure

- Create `Assets/Scripts/Moodium/Rewards/WorldRewardTable.cs`: reward row data, size class, lookup and validation.
- Create `Assets/Scripts/Moodium/Rewards/RewardTextTable.cs`: bilingual reward copy lookup.
- Create `Assets/Scripts/Moodium/Rewards/RewardPlacementSolver.cs`: pure candidate generation/scoring plus injected physics query interface.
- Create `Assets/Scripts/Moodium/Rewards/RewardOrb.cs`: orb visual, mini-reward clone sanitization, pop/float/fly/touch state.
- Create `Assets/Scripts/Moodium/Rewards/RewardBracelet.cs`: wrist-bound four-slot collection visual and gem inspection state.
- Create `Assets/Scripts/Moodium/Rewards/RewardGrowIn.cs`: cue and base-aligned grow animation.
- Create `Assets/Scripts/Moodium/Rewards/RewardSequenceController.cs`: per-session stage dedupe and parallel collection/world timelines.
- Modify `Assets/Scripts/Moodium/CandyWorldManager.cs`: configure/start/stop the sequence, forward pinch/bag/video/spatial-mesh state, preserve unrelated 100% effects, remove active Release wiring and fixed-growth event progression.
- Modify `Assets/Scripts/Moodium/CandyEnergyWristUI.cs`: retain left-wrist pose service and remove legacy energy-ring/release presentation in favour of the bracelet host.
- Modify `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`: serialize/pass two tables and copy resources.
- Modify `Assets/Scripts/Moodium/NatureForestGrowthController.cs` and `Assets/Scripts/Moodium/CandyCompletionHouseController.cs`: stop fixed stage spawning, retain only reusable base-alignment/compatibility helpers as needed.
- Modify `Assets/Scripts/Interaction/CompletionHouseSoftInteraction.cs`: derive contact defaults/collider coverage from renderer bounds so any grown reward can opt in.
- Create two `WorldRewardTable` resources beneath `Assets/Resources/Moodium/Rewards/` and their `.meta` files.
- Create focused EditMode test files in `Assets/Editor/` for data, placement, orb, bracelet, growth, and sequence integration.

### Task 1: Reward data and bilingual copy

**Files:**
- Create: `Assets/Scripts/Moodium/Rewards/WorldRewardTable.cs`
- Create: `Assets/Scripts/Moodium/Rewards/RewardTextTable.cs`
- Create: `Assets/Editor/WorldRewardTableTests.cs`
- Create: `Assets/Resources/Moodium/Rewards/CandyWorldRewardTable.asset`
- Create: `Assets/Resources/Moodium/Rewards/NatureWorldRewardTable.asset`

**Interfaces:**
- Produces `WorldRewardTable.TryGetEntry(int stage, out RewardEntry entry)`, `RewardEntry.IsValid`, `RewardSizeClass`, and `RewardTextTable.Get(string key, string language, params object[] args)`.

- [ ] **Step 1: Write failing table/copy tests**

Test stage lookup accepts only 20/50/80/100, rejects an empty prefab row, and maps `gem.label` in Chinese and English using the requested display name/number.

- [ ] **Step 2: Run the focused EditMode test to verify it fails**

Run: Unity EditMode filter `WorldRewardTableTests`.

Expected: FAIL because reward data/copy types do not exist.

- [ ] **Step 3: Implement the data types and resources**

Use serialized `RewardEntry` records with `GameObject[] prefabs`, display name, `RewardSizeClass`, target height, and a negative miniature override sentinel for “auto”. Create Candy/Nature resources using the specified stage mappings and existing configured prefab references; include resource `.meta` files.

- [ ] **Step 4: Run the focused EditMode test to verify it passes**

Run: Unity EditMode filter `WorldRewardTableTests`.

Expected: PASS.

### Task 2: Pure forward-room placement policy

**Files:**
- Create: `Assets/Scripts/Moodium/Rewards/RewardPlacementSolver.cs`
- Create: `Assets/Editor/RewardPlacementSolverTests.cs`

**Interfaces:**
- Consumes `RewardEntry` from Task 1.
- Produces `RewardPlacementSolver.Solve(RewardPlacementRequest request, IRewardPlacementQueries queries) -> RewardPlacementResult`, where result contains position, yaw, target-scale multiplier, reservation radius, and fallback reason.

- [ ] **Step 1: Write failing placement tests**

Add simulated-query tests for: 20% table-side placement excluding bag/video bounds; 50/80 opposite 30–45-degree side selection at 1.2–1.8 m; 100% forward ±20 degrees at 2.0–2.8 m; table rejection for floor items; Small/Medium blocked visibility rejection; Large upper visibility acceptance; overlap/minimum-distance/±50-degree exclusion; ordered relaxations; visibility-only final fallback; yaw-only facing.

- [ ] **Step 2: Run the focused EditMode test to verify it fails**

Run: Unity EditMode filter `RewardPlacementSolverTests`.

Expected: FAIL because solver/query types do not exist.

- [ ] **Step 3: Implement the pure solver and Unity-neutral query contracts**

Generate 5-degree/.1-m candidates, prefer the stage ranges in the spec, reserve successful results, and execute the exact relaxation order. Keep raycasts, capsule checks, bounds and occlusion behind the injected `IRewardPlacementQueries` interface so tests remain EditMode-only.

- [ ] **Step 4: Run the focused EditMode test to verify it passes**

Run: Unity EditMode filter `RewardPlacementSolverTests`.

Expected: PASS.

### Task 3: Orb clone hygiene and physical grow animation

**Files:**
- Create: `Assets/Scripts/Moodium/Rewards/RewardOrb.cs`
- Create: `Assets/Scripts/Moodium/Rewards/RewardGrowIn.cs`
- Create: `Assets/Editor/RewardOrbTests.cs`
- Create: `Assets/Editor/RewardGrowInTests.cs`
- Modify: `Assets/Scripts/Interaction/CompletionHouseSoftInteraction.cs`

**Interfaces:**
- Consumes `RewardEntry` and `RewardPlacementResult`.
- Produces `RewardOrb.Create(...)`, `BeginCollection(...)`, `TryBeginFlyTo(...)`, `AttachToSlot(...)`, and `RewardGrowIn.Begin(...)`.

- [ ] **Step 1: Write failing orb/grow tests**

Assert a miniature clone disables colliders, rigidbodies, particles, animator, and non-render components while retaining renderers and fitting within the 0.045 m interior. Assert grow-in ends at requested scale with rendered base aligned to surface after its 1.3 s animation.

- [ ] **Step 2: Run focused EditMode tests to verify they fail**

Run: Unity EditMode filters `RewardOrbTests` and `RewardGrowInTests`.

Expected: FAIL because runtime components do not exist.

- [ ] **Step 3: Implement orb construction and grow-in**

Create the 0.07 m world-colour glass orb/pedestal using URP material first with a named fallback hook, sanitize/fit miniature clones, apply the 0.3 s pop, 20-degree/second rotation, floating, and touch bounce. Implement ground cue/optional effects and 1.3 s base-aligned 30%-to-final growth with 1.04 overshoot. Generalize `CompletionHouseSoftInteraction.Configure()` to create usable contact coverage from renderer bounds for arbitrary readable static meshes.

- [ ] **Step 4: Run focused EditMode tests to verify they pass**

Run: Unity EditMode filters `RewardOrbTests` and `RewardGrowInTests`.

Expected: PASS.

### Task 4: Bracelet presentation and reward inspection

**Files:**
- Create: `Assets/Scripts/Moodium/Rewards/RewardBracelet.cs`
- Create: `Assets/Editor/RewardBraceletTests.cs`
- Modify: `Assets/Scripts/Moodium/CandyEnergyWristUI.cs`

**Interfaces:**
- Consumes the existing wrist-frame/pose follow service and `RewardOrb` from Task 3.
- Produces `RewardBracelet.SetProgress(float)`, `TryCollect(RewardOrb orb, RewardEntry entry)`, `SetTrackedWrist(...)`, `InspectSlot(int)`, and `Clear()`.

- [ ] **Step 1: Write failing bracelet tests**

Verify four slots, collected/empty count, next-slot ring proportion, centre/outer scale mapping, 30-degree visibility alpha/scale targets, hidden untracked state, and gem label selection/highlight timeout state.

- [ ] **Step 2: Run the focused EditMode test to verify it fails**

Run: Unity EditMode filter `RewardBraceletTests`.

Expected: FAIL because bracelet types do not exist.

- [ ] **Step 3: Implement visual bracelet and migrate wrist UI host responsibilities**

Keep `CandyEnergyWristUI.TryGetWristFrame`/smooth pose; remove energy-ring title/percentage/release toggle creation and active release callbacks. Add 0.012 m translucent band, four 0.017 m arc-spaced back-of-hand cups, dashed empty circles, white next-slot ring, 1.15/.35 gaze treatment over .3 s, four-gem full pulse, and `MoodiumSpatialButton` inspection colliders sized to .012 m radius.

- [ ] **Step 4: Run the focused EditMode test to verify it passes**

Run: Unity EditMode filter `RewardBraceletTests`.

Expected: PASS.

### Task 5: Session sequence and manager lifecycle integration

**Files:**
- Create: `Assets/Scripts/Moodium/Rewards/RewardSequenceController.cs`
- Create: `Assets/Editor/RewardSequenceControllerTests.cs`
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`

**Interfaces:**
- Consumes Tasks 1–4 plus `CandyEnergyController.StageReached`, existing bag/video/spatial mesh services, rain controller, and existing completion effect entry points.
- Produces `RewardSequenceController.Configure(...)`, `BeginSession(...)`, `RecordPinch(...)`, `SetProgress(...)`, `StopAndClear()` and `CandyWorldManager.ConfigureRewardTables(...)`.

- [ ] **Step 1: Write failing sequence integration tests**

Assert each valid stage delivers once per session, session restart resets delivery, duplicate/post-stop events do nothing, world growth begins by t0+2.5 when wrist is absent, collected orb waits then times out at 20 s, full collection plays rain once, and optional resources only record warnings.

- [ ] **Step 2: Run the focused EditMode test to verify it fails**

Run: Unity EditMode filter `RewardSequenceControllerTests`.

Expected: FAIL because controller/configuration wiring does not exist.

- [ ] **Step 3: Implement the sequence and manager/app-flow wiring**

Subscribe/unsubscribe `StageReached` with the session, capture head/bag/video/mesh snapshot at t0, run independent collection and world coroutines, route stage progress to the bracelet, and use a Unity query adapter around spatial `MeshCollider`s. `MoodiumAppFlowController` serializes both table references and sends them into manager configuration. Keep existing 100% panorama/ivy/footstep/hand-burst calls. Remove manager's active `ReleasePressed` subscription and Release availability path.

- [ ] **Step 4: Run the focused EditMode test to verify it passes**

Run: Unity EditMode filter `RewardSequenceControllerTests`.

Expected: PASS.

### Task 6: Retire fixed staged reward sources and preserve soft interaction

**Files:**
- Modify: `Assets/Scripts/Moodium/NatureForestGrowthController.cs`
- Modify: `Assets/Scripts/Moodium/CandyCompletionHouseController.cs`
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Create: `Assets/Editor/RewardLegacyIntegrationTests.cs`

**Interfaces:**
- Consumes `RewardSequenceController` from Task 5.
- Produces compatibility-only helper APIs for base alignment/spawn where sequence requires them; neither legacy controller responds to normalized energy progress for stage creation.

- [ ] **Step 1: Write failing regression tests**

Assert manager energy changes no longer create fixed-offset trees or houses, legacy fixed backwards offsets are not used by the active flow, each grown reward receives generic soft interaction, and the retained unrelated completion effects still receive their existing 100% call.

- [ ] **Step 2: Run the focused EditMode test to verify it fails**

Run: Unity EditMode filter `RewardLegacyIntegrationTests`.

Expected: FAIL against the old manager-controller wiring.

- [ ] **Step 3: Remove active fixed spawning and retain only necessary helpers**

Stop configuring/starting progress-driven forest/house displays in `CandyWorldManager`; keep cleanup compatibility. Move or expose only reusable base-align functionality needed by new grow-in, and ensure every sequence-created physical reward adds/configures `CompletionHouseSoftInteraction`.

- [ ] **Step 4: Run the focused EditMode test to verify it passes**

Run: Unity EditMode filter `RewardLegacyIntegrationTests`.

Expected: PASS.

### Task 7: Full regression verification and inspector/resource audit

**Files:**
- Modify: any task files only if verification exposes an in-scope regression.
- Test: `Assets/Editor/` existing and new test suite.

**Interfaces:**
- Consumes all prior tasks.
- Produces verified project configuration and a concise deliverable report listing tables, defaults, completed work, and device-tuning items.

- [ ] **Step 1: Run all new reward tests together**

Run: Unity EditMode filter covering `WorldRewardTableTests|RewardPlacementSolverTests|RewardOrbTests|RewardGrowInTests|RewardBraceletTests|RewardSequenceControllerTests|RewardLegacyIntegrationTests`.

Expected: PASS with zero failures.

- [ ] **Step 2: Run the existing Unity EditMode suite and refresh compile**

Run: project EditMode suite, then refresh the Unity editor and inspect compilation output.

Expected: no new failures and no compilation errors; document any pre-existing unrelated failures without masking them.

- [ ] **Step 3: Audit serialized wiring and diffs**

Confirm both table assets have the four intended stage entries, `MoodiumAppFlowController` references both tables, manager has no active Release switch subscription, and `git diff --check` is clean.

- [ ] **Step 4: Record final device-tuning checklist**

List 20/50/80/100 angle/distance candidate ranges, glass-material fallback, wrist occlusion, room-size coverage, and seated/standing checks as Vision Pro validation items.

## Self-review

- Spec coverage: Tasks 1–7 cover table data, bilingual copy, orb sequence, bracelet, placement, grow animation, soft interaction, legacy replacement, resilience, tests, and device tuning. No deliberate feature gaps.
- Type consistency: `WorldRewardTable/RewardEntry` feed solver, orb, bracelet, and sequence; `RewardPlacementResult` feeds growth; manager is the only session integration boundary.
- Review focus coverage: each listed input class has an explicit owning task/test.
- Proportion: implementation details are specified by public contracts and acceptance assertions; no production implementation bodies are embedded.
