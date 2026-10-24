# Mood Check and World Recommendation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add optional pre/post mood check-ins, non-binding world recommendations, local visit logging, and a progress-fading Low/Tense atmosphere to Moodium's existing Unity visionOS flow.

**Architecture:** A new `Moodium.MoodCheck` subsystem owns pure policy, session data, logging, world-space UI state, plate interaction, and the optional overlay. Existing opening, sticker-selection, spatial-input, app-flow, and world-manager classes receive narrow integration points and keep their current ownership boundaries.

**Tech Stack:** Unity 6000.0.30f1, C#/.NET Unity runtime, NUnit EditMode tests, TextMeshPro, PolySpatial 2.4.3, AR Foundation 6.0.3, XR Hands 1.5.0, Unity particle system.

**Spec:** `docs/superpowers/specs/2026-10-22-mood-check-world-recommendation-design.md`

## Global Constraints

- Preserve all existing uncommitted work; do not commit, reset, revert, clean, or stash.
- New runtime files live in `Assets/Scripts/Moodium/MoodCheck/` and use namespace `Moodium.MoodCheck`.
- Do not create or replace AR/ARKit sessions, input systems, or hand-collision input.
- Do not modify image-tracking logic, calibration math, Candy/Nature energy thresholds, or existing feedback chains.
- Recommendations never disable or bypass either available sticker-world entry.
- Skipping never writes a sample and never blocks tutorial or world-exit navigation.
- All user-visible copy comes from one Chinese/English table; use `OpeningVideoSession.English`, falling back to `PlayerPrefs["Moodium.Language"]`.
- Reuse `Moodium_Glass_UI`, `Moodium_Glass_UI_Edge`, configured TMP fonts, `MoodiumSpatialButton`, and `VisionOSHoverEffect`.
- Missing optional UI assets, particle resources, or log-write access must warn and degrade gracefully.
- The Low/Tense overlay is independently configurable and enabled by default.

## Review Focus

- Multiple simultaneous spatial pointers: only the pointer that began the plate drag may move or end it; Task 4 adds this test.
- Interruption during tutorial skip or object destruction: mood UI must dismiss without firing duplicate completion/navigation; Tasks 5 and 7 add these tests.
- World availability changing or neither world being available: recommendation must fall back or become absent while preserving the sample; Task 1 adds these tests.
- Re-entering sticker selection repeatedly: the zero-entry timer resets correctly and automatic presentation fires no more than once per app session; Task 6 adds these tests.
- Persistent-data failure or malformed pre-existing log content: append failure only warns and never changes completion; Task 2 adds this test.

---

## File Map

**New runtime files**

- `Assets/Scripts/Moodium/MoodCheck/MoodClassifier.cs` — pure classification, recommendation, presets, and comparison.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckSample.cs` — enums and serializable sample/visit metric values.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckCopy.cs` — centralized bilingual strings and formatting.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckContext.cs` — application session and active visit state.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckLog.cs` — JSON Lines DTO creation and append-only persistence.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckPlate.cs` — normalized plate position and visual state.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckPlateHandle.cs` — pointer capture facade for the plate.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckController.cs` — pre/post UI state machine and events.
- `Assets/Scripts/Moodium/MoodCheck/MoodCheckAutoPromptPolicy.cs` — deterministic 12-second sticker-selection policy.
- `Assets/Scripts/Moodium/MoodCheck/WorldMoodOverlayController.cs` — Low/Tense particle and clearing effect.

**New editor/setup and generated assets**

- `Assets/Editor/MoodCheckUISetup.cs` — idempotent texture configuration, rounded meshes, prefab generation, and app-flow wiring.
- `Assets/UI/Moodium/MoodCheck/**` — supplied PNG, SVG, and design-reference files plus generated meshes/materials/prefab.

**New tests**

- `Assets/Editor/MoodClassifierTests.cs`
- `Assets/Editor/MoodCheckLogTests.cs`
- `Assets/Editor/MoodCheckContextTests.cs`
- `Assets/Editor/MoodCheckPlateTests.cs`
- `Assets/Editor/MoodCheckControllerTests.cs`
- `Assets/Editor/MoodCheckAutoPromptPolicyTests.cs`
- `Assets/Editor/MoodCheckFlowIntegrationTests.cs`
- `Assets/Editor/WorldMoodOverlayControllerTests.cs`
- `Assets/Editor/MoodCheckUISetupTests.cs`

**Modified files**

- `Assets/Scripts/Moodium/MoodiumSpatialInteractionController.cs`
- `Assets/Scripts/Moodium/Opening/OpeningManager.cs`
- `Assets/Scripts/Moodium/Flow/StickerWorldSelectionController.cs`
- `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- `Assets/Scripts/Moodium/CandyWorldManager.cs`

## Shared Test Command

Use this command once per test fixture, substituting its fixture name for `<FILTER>`:

```bash
/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/yibei/Desktop/appcontest2026/Moodium -runTests -testPlatform EditMode -testFilter '<FILTER>' -testResults /tmp/moodium-mood-check-tests.xml -logFile /tmp/moodium-mood-check-tests.log -quit
```

A passing run exits 0 and the XML reports zero failures. On failure, inspect `/tmp/moodium-mood-check-tests.log` before changing code.

### Task 1: Pure Mood Domain and Localized Copy

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodClassifier.cs`
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckSample.cs`
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckCopy.cs`
- Test: `Assets/Editor/MoodClassifierTests.cs`

**Interfaces:**
- Produces: `MoodKind`, `MoodInputMethod`, `MoodCheckPhase`, `MoodComparison`, `MoodCheckSample`.
- Produces: `MoodClassifier.Classify(float valence, float arousal)`, `Intensity(float,float)`, `Preset(MoodKind)`, `Recommend(MoodKind,bool candyAvailable,bool natureAvailable)`, and `Compare(Vector2 before,Vector2 after,Vector2 target,float threshold = 0.15f)`.
- Produces: `MoodCheckCopy.Get(string key, bool english)` and `MoodCheckCopy.Format(string key,bool english,params object[] args)`.

- [ ] **Step 1: Write failing classification, boundary, preset, recommendation, comparison, and copy-completeness tests**

Assert `r < 0.22` is Neutral, exactly `0.22` follows its quadrant, the four signed quadrants map to Happy/Calm/Low/Tense, intensity clamps to 1, and presets equal `(±0.6, ±0.6)` or zero. Assert preferred/fallback/none recommendation results for both availability flags. Assert distance deltas `-0.15`, `+0.15`, and values inside the band map to Better/Worse/Same. Iterate every key referenced by the supplied copy table in both languages and assert a non-empty result.

- [ ] **Step 2: Run `MoodClassifierTests` and verify it fails because the domain types do not exist**

- [ ] **Step 3: Implement the domain types and exact pure APIs**

Clamp inputs before calculating intensity and classification. Represent “no recommendation” as an empty/null world ID, not a fabricated world. Keep UI wording out of `MoodClassifier`.

- [ ] **Step 4: Add the complete supplied bilingual copy table to `MoodCheckCopy`**

Include all `prompt.*`, `plate.*`, `axis.*`, `mood.*`, `confirm.*`, `rec.*`, `tag.*`, and `compare.*` keys from the prompt document.

- [ ] **Step 5: Run `MoodClassifierTests` and verify all tests pass**

### Task 2: Session Context and Append-Only Logging

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckContext.cs`
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckLog.cs`
- Test: `Assets/Editor/MoodCheckContextTests.cs`
- Test: `Assets/Editor/MoodCheckLogTests.cs`

**Interfaces:**
- Consumes: `MoodCheckSample` from Task 1.
- Produces: `MoodCheckContext.SessionId`, `PreSample`, `ActiveVisitId`, `PinchCount`, `BeginVisit(string enteredWorldId,float realtime)`, `RegisterPinch()`, `SnapshotMetrics(float realtime)`, `CompleteVisit()`, and `TryMarkAutoPromptShown()`.
- Produces: `MoodCheckLog.BuildJsonLine(MoodCheckSample sample)` and `MoodCheckLog.TryAppend(MoodCheckSample sample,string path = null)`.

- [ ] **Step 1: Write failing context lifecycle tests**

Assert one session GUID survives multiple visits, each visit gets a new GUID, duration never becomes negative, pinch counting only applies during an active visit, completion clears visit data without clearing the session, and `TryMarkAutoPromptShown()` returns true only once.

- [ ] **Step 2: Write failing serialization and I/O tests**

Assert required JSON field names, lowercase enum values, UTC timestamp, empty pre `enteredWorldId`, and post duration/pinch fields. Use a temporary directory to assert two writes produce two valid lines. Pass an unwritable/directory path and assert `TryAppend` returns false without throwing, including when a malformed existing file is present.

- [ ] **Step 3: Run `MoodCheckContextTests,MoodCheckLogTests` and verify failure**

- [ ] **Step 4: Implement `MoodCheckContext` with injected/reported realtime values**

Avoid direct `Time` access in the data class so duration tests remain deterministic.

- [ ] **Step 5: Implement `MoodCheckLog` with an explicit DTO and `File.AppendAllText`**

End every record with `\n`; catch `Exception`, issue `Debug.LogWarning`, and emit a `[Mood Check]` structured line without logging filesystem contents.

- [ ] **Step 6: Run both fixtures and verify all tests pass**

### Task 3: Import Assets and Build the Reusable UI Prefab

**Files:**
- Create: `Assets/Editor/MoodCheckUISetup.cs`
- Create/copy: `Assets/UI/Moodium/MoodCheck/png/*`, `svg/*`, `design-reference/*`
- Generate: `Assets/UI/Moodium/MoodCheck/Generated/MoodCheckPanel.prefab`
- Generate: `Assets/UI/Moodium/MoodCheck/Generated/*.asset`
- Test: `Assets/Editor/MoodCheckUISetupTests.cs`

**Interfaces:**
- Produces: `MoodCheckUISetup.BuildOrUpdate()` and an idempotently generated prefab with stable child names consumed by Task 5.
- Produces hierarchy: `PanelGlass`, `Header`, `Readout`, `Plate`, `AxisLabels`, `QuickChips`, `Footer` and the plate children named in the spec.

- [ ] **Step 1: Copy the supplied asset folders into the exact project destination without copying `.DS_Store`**

- [ ] **Step 2: Write failing importer and prefab-structure tests**

Assert every PNG is Sprite, sRGB, alpha transparency, mipmapped, Clamp wrapped, and retains 256/512 maximum size. Assert prefab dimensions, required hierarchy, plate/token colliders, sorting orders, `MoodiumSpatialButton` on every chip/action, and configured glass/edge materials.

- [ ] **Step 3: Run `MoodCheckUISetupTests` and verify failure**

- [ ] **Step 4: Implement idempotent rounded-rect mesh and prefab generation**

Reuse the mesh algorithm from `MoodiumGlassPanelSetup` in Editor code. Build at 1 px = 0.0007 m, including `0.462 × 0.566 m` panel, `0.28 m` plate, `0.123 m` neutral ring, `0.073 m` Moodi, and button colliders at least `0.045 m` high.

- [ ] **Step 5: Configure render sorting, fonts, sprites, glass, hover components, and serialized references**

Moodi sprites remain white/untinted; helper sprites use tintable white. Use sorting 0 for glass, 11–15 for plate layers, 18–22 for shadow/token, and 30 for text.

- [ ] **Step 6: Invoke `MoodCheckUISetup.BuildOrUpdate()` through Unity and rerun the fixture**

Verify the second invocation makes no duplicate assets or hierarchy nodes.

### Task 4: Plate Model and Spatial Pointer Routing

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckPlate.cs`
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckPlateHandle.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumSpatialInteractionController.cs`
- Test: `Assets/Editor/MoodCheckPlateTests.cs`

**Interfaces:**
- Consumes: Task 1 classification types and Task 3 prefab references.
- Produces: `MoodCheckPlate.SetNormalizedPosition(Vector2,bool immediate)`, `NormalizedPosition`, `CurrentMood`, `BeginDrag()`, `EndDrag()`, and `SetVisuals(...)`.
- Produces: `MoodCheckPlateHandle.PointerStarted(int,Vector3)`, `PointerMoved(int,Vector3)`, `PointerEnded(int,Vector3)`, and `CancelPointer(int)`.

- [ ] **Step 1: Write failing coordinate and pointer-capture tests**

Use rotated/non-uniformly transformed plate parents and assert `InverseTransformPoint` conversion, Z discard, division by `0.112 m`, unit-circle clamp, center handling, and exact world-to-local results. Assert a second pointer cannot move or end the first pointer's drag and cancellation releases capture.

- [ ] **Step 2: Run `MoodCheckPlateTests` and verify failure**

- [ ] **Step 3: Implement the plate's model and rate-limited visual updates**

Directly follow pointer position. Update material/tint only on mood change or intensity delta greater than `0.02`; interpolate tint/icons over `0.3–0.35 s`; hide trail below `0.08`.

- [ ] **Step 4: Implement handle capture and add the narrow spatial-input route**

Resolve with `GetComponentInParent<MoodCheckPlateHandle>()`; mirror existing Began/Moved/Ended dictionaries without changing other route precedence.

- [ ] **Step 5: Run `MoodCheckPlateTests` plus existing spatial/calibration interaction tests and verify pass**

### Task 5: Mood Check State Machine, Panel States, and Comparison

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckController.cs`
- Test: `Assets/Editor/MoodCheckControllerTests.cs`
- Modify/generated: `Assets/UI/Moodium/MoodCheck/Generated/MoodCheckPanel.prefab`

**Interfaces:**
- Consumes: Tasks 1–4.
- Produces: `BeginPreCheck(MoodCheckAvailability availability,bool english)`, `BeginPostCheck(MoodCheckSample pre,MoodVisitMetrics metrics,bool english)`, `DismissImmediate()`, `IsVisible`, `State`, `Completed`, and `Skipped`.
- Produces events: `event Action<MoodCheckSample> Completed`, `event Action Skipped`.

- [ ] **Step 1: Write failing state transition and exactly-once event tests**

Assert Hidden → Dragging → Confirming → Recommending for pre and Hidden → Dragging → Confirming → Comparing for post. Assert adjust preserves coordinates, quick chips set preset/input method and enter Confirming, skip writes nothing, repeated button calls do not duplicate events, and `DismissImmediate()` during any state emits no completion.

- [ ] **Step 2: Write failing placement, localization, and degraded-resource tests**

Assert placement is 0.75 m ahead and 0.12 m down using horizontal camera forward; it remains fixed after camera motion. Assert Chinese/English labels, non-uniform text scale compensation, initial post coordinate, no-world recommendation behavior, and functional skip/confirm when optional sprites are null.

- [ ] **Step 3: Run `MoodCheckControllerTests` and verify failure**

- [ ] **Step 4: Implement state ownership and button/event wiring**

The controller must never navigate app flow itself. Use explicit state guards so callbacks are idempotent.

- [ ] **Step 5: Implement selection, confirmation, compact recommendation, full recommendation, and comparison render states**

Use the exact prefab references and dimensions. Implement the specified token lift/squash, panel entry/exit, icon/tint transitions, recommendation breathing, comparison target result, duration, and pinch text with no flashing.

- [ ] **Step 6: Run `MoodCheckControllerTests` and verify all tests pass**

### Task 6: Sticker-Selection Entry, Timer, and Recommendation Tag

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/MoodCheckAutoPromptPolicy.cs`
- Modify: `Assets/Scripts/Moodium/Flow/StickerWorldSelectionController.cs`
- Test: `Assets/Editor/MoodCheckAutoPromptPolicyTests.cs`
- Modify: `Assets/Editor/StickerWorldSelectionControllerTests.cs`

**Interfaces:**
- Consumes: `MoodCheckContext`, `MoodCheckCopy`, and the current pre-sample.
- Produces on selection controller: `event Action MoodCheckRequested`, `SetMoodCheckSummary(MoodCheckSample sample)`, and `SetRecommendedWorld(string worldId)`.
- Produces policy: `Tick(float deltaSeconds,int activeEntryCount,bool hasSample,bool alreadyShown)` and `ResetNoEntryTimer()`.

- [ ] **Step 1: Write failing auto-prompt policy tests**

Assert 11.99 seconds does not trigger, 12.0 continuous zero-entry seconds triggers, positive entry count resets the timer, a later 12-second empty interval may trigger, and existing sample/already-shown suppresses triggering.

- [ ] **Step 2: Extend sticker-selection tests for the secondary button and recommendation tag**

Assert button text changes after a sample, clicking fires `MoodCheckRequested`, only matching world entries receive the tag, tag scale stays within 1–1.05, and every Enter button remains enabled.

- [ ] **Step 3: Run both fixtures and verify failure**

- [ ] **Step 4: Implement the pure timer policy and selection-controller presentation**

Place the secondary button below the compensated instruction. Keep the controller presentation-only; app flow owns whether the request opens a panel.

- [ ] **Step 5: Run both fixtures and existing `StickerWorldEntryTests` and verify pass**

### Task 7: Opening and App-Flow Orchestration

**Files:**
- Modify: `Assets/Scripts/Moodium/Opening/OpeningManager.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- Test: `Assets/Editor/MoodCheckFlowIntegrationTests.cs`
- Modify: `Assets/Editor/OpeningSkipBehaviorTests.cs`
- Modify: `Assets/Editor/OpeningVideoTimelineTests.cs`

**Interfaces:**
- Consumes: Tasks 1–6 and existing `OpeningVideoSession`, calibration, sticker entry, and wrist exit callbacks.
- Produces on app flow: `BeginEntryMoodCheck(Action continuation)`, `RequestStickerMoodCheck()`, `BeginMoodVisit(MoodiumWorldDefinition world)`, and post-check-aware `ReturnToStickerWorldSelection()`.
- Produces on opening manager: one injectable/coroutine mood-check wait between settled hardware choice and branch seek.

- [ ] **Step 1: Write failing tutorial sequencing and interruption tests**

Assert question loop continues while the panel is active; branch seek occurs only after complete/skip; compact recommendation remains through branch playback; `skipRequested()` dismisses immediately; destroyed controller/session exits safely; continuation fires once.

- [ ] **Step 2: Write failing app-flow visit and exit tests**

Assert sticker requests open pre-check, confirmed replacement updates the selection summary, calibration still precedes world start, selected world overrides recommendation, visit metrics begin once, no-pre exit returns directly, pre-sample exit stops the world before post-check, post skip returns without log, and compare Continue closes the visit then returns.

- [ ] **Step 3: Run `MoodCheckFlowIntegrationTests,OpeningSkipBehaviorTests,OpeningVideoTimelineTests` and verify failure**

- [ ] **Step 4: Insert the opening pre-check at the exact hardware-choice/branch boundary**

Reuse `SeekToFrameAndPlay` and the current player. Do not parent the panel under the display; retain only the compact recommendation visual during branch playback.

- [ ] **Step 5: Integrate context, controller, logging, sticker requests, visit start, and post-check into app flow**

Merge around the existing uncommitted wrist-exit code. Do not recreate the removed room-fixed Back panel.

- [ ] **Step 6: Run the integration fixtures plus existing opening, sticker-flow, calibration, and wrist-exit fixtures**

### Task 8: World Metrics and Low/Tense Mood Overlay

**Files:**
- Create: `Assets/Scripts/Moodium/MoodCheck/WorldMoodOverlayController.cs`
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Test: `Assets/Editor/WorldMoodOverlayControllerTests.cs`
- Modify: `Assets/Editor/CandyWorldProgressControllerTests.cs`

**Interfaces:**
- Consumes: active `MoodCheckContext` and its pre-sample.
- Produces: `WorldMoodOverlayController.Configure(bool enabled)`, `Begin(MoodCheckSample sample,Transform cameraRoot)`, `SetNormalizedProgress(float)`, and `Stop()`.
- Candy manager accepts/configures a context or visit callbacks without changing its existing public start overload used by tests.

- [ ] **Step 1: Write failing overlay policy and lifecycle tests**

Assert disabled, null, Happy, Calm, and Neutral samples create no overlay. Assert Low/Tense initial emission scales with intensity, progress 0.4 leaves factor 0.6, progress 1 stops emission and fires clearing once, repeated completion does not replay, and missing particle dependencies only warn.

- [ ] **Step 2: Write failing visit metric integration tests**

Assert accepted `AnyCapsulePinched` events increment the active visit exactly once, events outside a visit do not count, `StartExperience` begins overlay from current context, `OnEnergyChanged` forwards normalized progress, and stop unsubscribes/clears without affecting existing energy values.

- [ ] **Step 3: Run overlay and Candy progress fixtures and verify failure**

- [ ] **Step 4: Implement the PolySpatial-compatible runtime particle overlay and clearing effect**

Follow existing runtime particle creation patterns; use no large new asset. Keep effects visual only and omit weather/rain wording from UI copy.

- [ ] **Step 5: Integrate metric counting and overlay forwarding into `CandyWorldManager`**

Preserve its existing uncommitted language, wrist exit, reward, Nature, and energy changes.

- [ ] **Step 6: Run the overlay fixture and all Candy/Nature world manager/progress tests**

### Task 9: Full Setup, Regression Verification, and Visual QA

**Files:**
- Modify: `Assets/Editor/MoodCheckUISetup.cs` if wiring validation exposes gaps.
- Modify: only files from Tasks 1–8 needed to correct verified failures.
- Test: all new and affected existing EditMode fixtures.

**Interfaces:**
- Consumes: every prior task.
- Produces: configured project assets and a verified end-to-end Mood Check feature.

- [ ] **Step 1: Run the setup tool once in Unity and save only intended generated assets/references**

- [ ] **Step 2: Run every new Mood Check EditMode fixture**

Run the shared command separately for `MoodClassifierTests`, `MoodCheckLogTests`, `MoodCheckContextTests`, `MoodCheckPlateTests`, `MoodCheckControllerTests`, `MoodCheckAutoPromptPolicyTests`, `MoodCheckFlowIntegrationTests`, `WorldMoodOverlayControllerTests`, and `MoodCheckUISetupTests`; require zero failures in every result.

- [ ] **Step 3: Run the full EditMode suite**

Run the shared command without `-testFilter`; require exit 0 and zero XML failures. Diagnose and fix only regressions caused by this feature.

- [ ] **Step 4: Inspect Unity compile/import logs and generated prefab**

Verify zero compiler errors, missing scripts, missing required references, or duplicate generated nodes. Confirm all optional-missing cases warn rather than throw.

- [ ] **Step 5: Perform Editor mouse flow QA**

Exercise tutorial pre-check/skip, plate and quick chip, confirmation/adjust, recommendation, arbitrary sticker choice, exit, post-check/skip, comparison, repeated sticker selection, and local JSONL output.

- [ ] **Step 6: Perform visual QA against `Main.dc.html`, `Recommend.dc.html`, `Compare.dc.html`, `States.dc.html`, and `Token.dc.html`**

Check dimensions, spacing, fonts, unwarped text, glass transparency, sorting, Moodi untinted sprites, icon opacity, motion timing, and lack of flashing.

- [ ] **Step 7: Record device-only validation still required**

If a visionOS device is unavailable, explicitly list gaze + pinch, direct touch, BlurredBackground glass, mipmaps, and PolySpatial particles as pending device checks rather than claiming them complete.

- [ ] **Step 8: Review `git diff` and `git status` for scope and preservation**

Confirm pre-existing modified files remain present, no unrelated files changed, no temporary test XML is newly tracked, and no commit/reset/revert/clean/stash occurred.
