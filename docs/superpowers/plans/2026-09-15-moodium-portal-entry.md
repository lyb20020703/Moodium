# Moodium Portal Entry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Creative Space / Reality Enhancement mode chooser with an automatically opening portal whose inner 3D object is touched to open the existing Reality Enhancement world selector.

**Architecture:** Keep `Meshing 1.unity` as the sole runtime scene. Add a focused portal entrance controller and prefab, connect completion to `MoodiumAppFlowController`, and retain but stop constructing Creative Space runtime UI. Build Moodium-owned portal assets from the PolySpatial sample without modifying the sample.

**Tech Stack:** Unity 6.0.30f1, C#, PolySpatial visionOS, XR Hands, Unity Animator/AnimationClip, NUnit Edit Mode tests

**Spec:** `docs/superpowers/specs/2026-09-15-moodium-portal-entry-design.md`

## Global Constraints

- Keep `Assets/Samples/PolySpatial/Scenes/Portal.unity` and its source assets unchanged.
- Keep all Creative Space scripts and assets, but do not initialize or expose them in the runtime flow.
- Keep Reality Enhancement Candy/Nature world selection and current Object/Image Tracking behavior.
- Do not rely solely on `XRHandJointID.Palm`; use reliable fingertips, wrist, and metacarpals.
- Portal entry accepts exactly one touch after opening completes.
- Missing optional VFX/audio/prompt references must not prevent entry.

---

### Task 1: Portal entrance state machine

**Files:**
- Create: `Assets/Scripts/Moodium/Opening/PortalEntranceStateMachine.cs`
- Create: `Assets/Scripts/Moodium/Opening/PortalEntranceStateMachine.cs.meta`
- Create: `Assets/Editor/PortalEntranceStateMachineTests.cs`
- Create: `Assets/Editor/PortalEntranceStateMachineTests.cs.meta`

**Interfaces:**
- Produces: `PortalEntranceState`, and `PortalEntranceStateMachine` with `BeginOpening()`, `MarkReady()`, `TryBeginEntering()`, `MarkCompleted()`, and `Reset()`.
- Consumed by: `MoodiumPortalEntranceController` in Task 2.

- [ ] **Step 1: Write failing state-transition tests**

Cover the legal sequence `Hidden -> Opening -> Ready -> Entering -> Completed`, reject contact before `Ready`, and reject repeated contact after the first accepted touch.

- [ ] **Step 2: Run the focused Edit Mode tests and verify failure**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/yibei/Desktop/appcontest2026/Moodium -runTests -testPlatform EditMode -testFilter PortalEntranceStateMachineTests -testResults /tmp/moodium-portal-state-tests.xml -logFile /tmp/moodium-portal-state-tests.log -quit
```

Expected: test compilation fails because the state machine does not exist.

- [ ] **Step 3: Implement the minimal state machine**

Use an enum and guarded transitions. `TryBeginEntering()` returns `true` only while `State == PortalEntranceState.Ready` and immediately sets `Entering`.

- [ ] **Step 4: Rerun the focused tests**

Expected: all `PortalEntranceStateMachineTests` pass.

### Task 2: Runtime portal controller and direct hand contact

**Files:**
- Create: `Assets/Scripts/Moodium/Opening/MoodiumPortalEntranceController.cs`
- Create: `Assets/Scripts/Moodium/Opening/MoodiumPortalEntranceController.cs.meta`
- Create: `Assets/Editor/MoodiumPortalEntranceControllerTests.cs`
- Create: `Assets/Editor/MoodiumPortalEntranceControllerTests.cs.meta`

**Interfaces:**
- Consumes: `PortalEntranceStateMachine` from Task 1, `XRHandSubsystem`, and a serialized `EntryModelRoot`.
- Produces: `event Action Completed`, `Show()`, `HideImmediate()`, `TryAcceptContact(Vector3 worldPoint)`, `State`, and an entry-model bounds resolver.

- [ ] **Step 1: Write failing controller tests**

Tests instantiate an inactive controller GameObject with simple transforms and verify:

- `Show()` enters `Opening`;
- contact during `Opening` is rejected;
- completing the opening enables one accepted bounds contact;
- a second contact is rejected;
- missing optional prompt, particles, and audio do not prevent completion;
- renderer bounds are converted to a usable world-space contact volume.

- [ ] **Step 2: Run the controller tests and verify failure**

Use the Task 1 Unity command with `-testFilter MoodiumPortalEntranceControllerTests`.

- [ ] **Step 3: Implement the controller**

The controller:

- waits for `Camera.main`, then places itself using horizontal camera forward;
- plays the portal Animator from its closed state;
- waits for a serialized opening duration before changing to `Ready`;
- polls the active `XRHandSubsystem` only in `Ready`;
- checks index/middle/ring/little/thumb tips, wrist, and metacarpals against entry bounds;
- invokes `TryAcceptContact` for testable contact handling;
- locks on the first accepted contact;
- plays optional Animator trigger, particle burst, and AudioSource;
- waits for the serialized exit duration, hides the root, marks completed, and raises `Completed` once.

Expose `NotifyOpeningCompleteForTests()` and `CompleteTransitionForTests()` as internal test hooks only if coroutine timing cannot be advanced cleanly in Edit Mode.

- [ ] **Step 4: Run controller and state tests**

Expected: both test fixtures pass.

### Task 3: Replace mode selection with portal routing

**Files:**
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- Modify: `Assets/Scripts/Moodium/Opening/OpeningManager.cs`
- Create: `Assets/Editor/MoodiumPortalFlowTests.cs`
- Create: `Assets/Editor/MoodiumPortalFlowTests.cs.meta`

**Interfaces:**
- Consumes: `MoodiumPortalEntranceController.Show()` and `.Completed`.
- Produces: `ShowPortalEntrance()` and a Reality world-selector route callable after portal completion.

- [ ] **Step 1: Write failing flow-structure tests**

Use reflection/source assertions consistent with the project’s existing Edit Mode tests to verify:

- startup hands off to `ShowPortalEntrance`, not `ShowModeSelection`;
- `BuildFlowUI()` does not call `BuildModeSelection`, `BuildWorldSelectionPanel`, `BuildNoObjectPanel`, or build Creative Space interaction UI;
- portal completion routes to the Reality world selector;
- Reality Back routes to the Reality world selector;
- the portal is hidden when Reality tracking starts.

- [ ] **Step 2: Run the flow tests and verify failure**

Use the Unity command with `-testFilter MoodiumPortalFlowTests`.

- [ ] **Step 3: Refactor application flow minimally**

Add a serialized portal controller reference. Build only the Reality world selector and Reality Back panel. Subscribe once to portal completion, expose `ShowPortalEntrance()`, and route completion to the existing object-world selector. Keep Creative Space methods and serialized fields available but unreachable so existing assets remain intact.

Change `OpeningManager` final handoff to enable the app flow and call `ShowPortalEntrance()` explicitly. Ensure `Start()` does not race by showing another panel after the opening has prepared the flow.

- [ ] **Step 4: Run portal flow tests plus existing Reality flow tests**

Run filters `MoodiumPortalFlowTests`, `DualTrackingRealityFlowTests`, and `RealityModeTableVisibilityTests`. Expected: all pass.

### Task 4: Create Moodium-owned portal prefab and connect the scene

**Files:**
- Create: `Assets/Materials/MoodiumPortal/MoodiumPortalOcclusion.mat`
- Create: `Assets/Materials/MoodiumPortal/MoodiumPortalOcclusion.mat.meta`
- Create: `Assets/Animations/MoodiumPortal/MoodiumPortalOpen.anim`
- Create: `Assets/Animations/MoodiumPortal/MoodiumPortalOpen.anim.meta`
- Create: `Assets/Animations/MoodiumPortal/MoodiumPortal.controller`
- Create: `Assets/Animations/MoodiumPortal/MoodiumPortal.controller.meta`
- Create: `Assets/prefab/MoodiumOpening/MoodiumPortalEntrance.prefab`
- Create: `Assets/prefab/MoodiumOpening/MoodiumPortalEntrance.prefab.meta`
- Modify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity`
- Create or modify: a focused editor setup utility under `Assets/Editor/`

**Interfaces:**
- Consumes: controller serialized fields from Task 2 and the app-flow serialized portal reference from Task 3.
- Produces: a scene-connected portal using a replaceable `EntryModelRoot` placeholder.

- [ ] **Step 1: Add an editor validation test**

Open `Meshing 1.unity` in an Edit Mode test and verify there is exactly one `MoodiumPortalEntranceController`, its mandatory visual/model references are assigned, its object is initially hidden, and `MoodiumAppFlowController` references it.

- [ ] **Step 2: Run validation and verify failure**

Expected: the scene has no portal controller.

- [ ] **Step 3: Build the portal assets through Unity Editor APIs**

Create a Moodium editor setup method that:

- copies the sample occlusion material into the Moodium material folder;
- creates a compact four-plane portal hierarchy sized for 1.2–1.5 metre placement;
- creates a simple sphere placeholder under `EntryModelRoot` using a Moodium-owned material;
- creates the opening animation/controller or configures a deterministic scripted opening;
- adds the prompt and optional particle/audio holders;
- saves `MoodiumPortalEntrance.prefab`;
- instantiates it once in `Meshing 1.unity`, initially inactive;
- assigns it to `MoodiumAppFlowController` using `SerializedObject`;
- saves the scene without changing the PolySpatial sample Portal scene.

- [ ] **Step 4: Run the setup through the active Unity Editor or batch editor**

After execution, refresh assets and save the scene. Do not hand-edit Unity YAML for prefab generation.

- [ ] **Step 5: Run scene validation**

Expected: all portal scene-reference assertions pass.

### Task 5: Full verification and regression check

**Files:**
- Modify only files required to correct failures found by verification.

**Interfaces:**
- Consumes: completed Tasks 1–4.
- Produces: a compiling, scene-connected portal entry flow.

- [ ] **Step 1: Run all Moodium Edit Mode tests**

Run Unity Edit Mode tests for the project and save XML/log outputs under `/tmp`.

- [ ] **Step 2: Check Unity compilation logs**

Verify there are no C# compiler errors, missing scripts, failed asset imports, or broken serialized references.

- [ ] **Step 3: Inspect the final diff**

Confirm:

- no files under `Assets/Samples/PolySpatial/Portal/` or `Assets/Samples/PolySpatial/Scenes/Portal.unity` changed;
- existing unrelated dirty worktree changes were preserved;
- Creative Space files were not deleted;
- `Meshing 1.unity` contains one connected portal entrance;
- the new flow retains both tracked-object and tracked-image managers.

- [ ] **Step 4: Report device-only checks separately**

Document that portal placement comfort, visionOS occlusion appearance, and physical hand contact require final verification on Apple Vision Pro even when Edit Mode tests pass.
