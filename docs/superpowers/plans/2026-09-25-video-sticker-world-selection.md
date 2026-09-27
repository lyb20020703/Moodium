# Video Sticker World Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the opening video alive after every tutorial branch, use a looped video-backed image-tracked sticker state to present Candy/Nature world entry prefabs, then route a selected world through Phase 1 calibration before its existing interaction flow.

**Architecture:** Preserve the one existing `ARTrackedImageManager` and make three consumers state-aware: the runtime video session, a new sticker-entry controller, and the existing calibration coordinator. `MoodiumAppFlowController` owns transitions from opening handoff to sticker selection, calibration confirmation, world interaction and world exit; it no longer routes these paths to the old carousel.

**Tech Stack:** Unity 6000.0.30f1, C#, AR Foundation 6.0.3, PolySpatial 2.4.3, visionOS XR 2.4.3, TextMeshPro, NUnit Editor tests.

**Spec:** `docs/superpowers/specs/2026-09-25-video-sticker-world-selection-design.md`

## Global Constraints

- Keep the existing `ARTrackedImageManager`, Image Tracking library, hand tracking and PolySpatial spatial-pointer pipeline; do not create an ARKit session.
- Keep the Phase 1 calibration geometry and UI behavior unchanged; use it only after a world entry is selected.
- Do not modify Candy/Nature feedback behavior, gesture/impact code, Soft prefab art, or Phase 2 geometry fitting.
- Use `OpeningVideoTimeline.Chinese.QuestionLoop` exactly (`00:41:10–00:47:00`) as the post-tutorial selection loop for either language.
- Preserve dirty unrelated workspace changes. Do not reset, checkout, revert, clean, stash, overwrite unrelated files, or auto-commit.

## Review Focus

- Two physical stickers tracked together must create two independently selectable entries; removal of one must not remove the other. Task 2 covers this.
- The `VideoPlayer` must survive the tutorial-to-selection handoff and must not enter the old tutorial-failure route after a valid completed branch. Task 3 covers this.
- During sticker selection, detected images must not start calibration or spawn Soft prefabs. Task 4 covers this.
- Calibration must use the exact selected anchor, not whichever image happened to update last. Task 4 covers this.
- Back from a world must clean Nature runtime growth and tracked instances before restoring the selection loop rather than reopening the legacy carousel. Task 5 covers this.

---

### Task 1: Define Explicit Opening Video Session and Sticker-Entry Contracts

**Files:**
- Create: `Assets/Scripts/Moodium/Opening/OpeningVideoSession.cs`
- Create: `Assets/Scripts/Moodium/Flow/StickerWorldEntry.cs`
- Create: `Assets/Editor/OpeningVideoSessionTests.cs`
- Create: `Assets/Editor/StickerWorldEntryTests.cs`

**Interfaces:**
- Produces `OpeningVideoSession`, constructed with the existing `VideoPlayer`, display root, prompt root, selected language, and disposal callback.
- Produces `StickerWorldEntry`, containing `TrackableId`, reference-image name, `MoodiumWorldDefinition`, anchor Transform and entry root.
- Consumed by Tasks 2–5.

- [ ] **Step 1: Write failing contract tests**

```csharp
[Test]
public void Session_HideAndShowDisplay_DoesNotStopOrDisposePlayer()
{
    var player = new GameObject().AddComponent<VideoPlayer>();
    var display = new GameObject("Display");
    var session = new OpeningVideoSession(player, display.transform, null, english: true, () => { });
    session.SetVisible(false);
    Assert.That(display.activeSelf, Is.False);
    Assert.That(session.IsDisposed, Is.False);
    session.SetVisible(true);
    Assert.That(display.activeSelf, Is.True);
}

[Test]
public void Entry_RetainsTheSelectedTrackedAnchorAndWorld()
{
    var anchor = new GameObject("CandyAnchor").transform;
    var entry = new StickerWorldEntry(new TrackableId(3, 7), "CandyWorldPhoneTracking", candyWorld, anchor, null);
    Assert.That(entry.ReferenceImageName, Is.EqualTo("CandyWorldPhoneTracking"));
    Assert.That(entry.Anchor, Is.SameAs(anchor));
    Assert.That(entry.World, Is.SameAs(candyWorld));
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter "FullyQualifiedName~OpeningVideoSessionTests|FullyQualifiedName~StickerWorldEntryTests"`

Expected: compile failure because the contracts do not exist.

- [ ] **Step 3: Implement small ownership-only contracts**

```csharp
public sealed class OpeningVideoSession
{
    public VideoPlayer Player { get; }
    public Transform DisplayRoot { get; }
    public bool English { get; }
    public bool IsDisposed { get; private set; }
    public void SetVisible(bool visible) => DisplayRoot.gameObject.SetActive(visible);
    public void DisposeOnce() { /* invoke supplied teardown once */ }
}
```

Keep it free of timeline state and image tracking. `StickerWorldEntry` is a simple immutable context record/class; it must not begin calibration or instantiate interaction content.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter "FullyQualifiedName~OpeningVideoSessionTests|FullyQualifiedName~StickerWorldEntryTests"`

Expected: all new contract tests pass.

### Task 2: Implement Multi-Anchor Sticker World Entry Controller

**Files:**
- Create: `Assets/Scripts/Moodium/Flow/StickerWorldSelectionController.cs`
- Create: `Assets/Editor/StickerWorldSelectionControllerTests.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumSpatialInteractionController.cs`

**Interfaces:**
- Consumes `OpeningVideoSession` and `StickerWorldEntry` from Task 1.
- Produces `Begin(OpeningVideoSession, ARTrackedImageManager, bool)`, `End()`, and `WorldEntrySelected`.
- Consumed by Tasks 4–5.

- [ ] **Step 1: Write failing entry lifecycle tests**

```csharp
[Test]
public void TwoTrackedImages_CreateTwoIndependentWorldEntries()
{
    controller.Configure(manager, candyPrefab, naturePrefab, candyWorld, natureWorld, font, roundedSprite);
    controller.Begin(session, manager, english: false);
    controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);
    controller.HandleTrackedImageUpdate(NatureId, "NatureWorldPhoneTracking", natureAnchor, TrackingState.Tracking);
    Assert.That(controller.ActiveEntryCount, Is.EqualTo(2));
    Assert.That(controller.GetEntry(CandyId).World, Is.SameAs(candyWorld));
    Assert.That(controller.GetEntry(NatureId).World, Is.SameAs(natureWorld));
}

[Test]
public void TrackingLoss_HidesOnlyAffectedEntry_AndRecoveryReusesIt()
{
    controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.None);
    Assert.That(controller.GetEntry(CandyId).Root.activeSelf, Is.False);
    Assert.That(controller.GetEntry(NatureId).Root.activeSelf, Is.True);
    controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);
    Assert.That(controller.ActiveEntryCount, Is.EqualTo(2));
}
```

Add tests that validate Chinese labels are `请对准贴纸` / `进入世界`, English labels are `Look at the sticker` / `Enter`, and unsupported reference images create no entry.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~StickerWorldSelectionControllerTests`

Expected: compile failure because the controller does not exist.

- [ ] **Step 3: Implement tracked-image entry mapping**

Subscribe once to `ARTrackedImageManager.trackablesChanged` while selection is active. Map only:

```csharp
"CandyWorldPhoneTracking" => (m_CandyWorld, m_CandyWorldUiPrefab)
"NatureWorldPhoneTracking" => (m_NatureWorld, m_NatureWorldUiPrefab)
```

Instantiate one root per `TrackableId`, parent it to the updated anchor, and set configurable local position, rotation and scale. Create the button as a child with a BoxCollider plus the existing Moodium spatial button/pointer interaction route. On click emit the exact `StickerWorldEntry`; do not call app flow directly from the entry component.

Route only the new entry button component through `MoodiumSpatialInteractionController`; preserve every existing pointer branch.

- [ ] **Step 4: Implement safe tracking state behavior**

On `TrackingState.None`, hide but retain the entry. On removed, destroy and remove only that entry. `End()` unsubscribes, removes all entries, hides the instruction and does not dispose the passed video session.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~StickerWorldSelectionControllerTests`

Expected: all entry lifecycle/localization tests pass.

### Task 3: Keep Completed Opening Video Alive and Loop the Sticker-Selection Segment

**Files:**
- Modify: `Assets/Scripts/Moodium/Opening/OpeningManager.cs`
- Modify: `Assets/Scripts/Moodium/Opening/OpeningVideoTimeline.cs`
- Create: `Assets/Editor/OpeningStickerSelectionHandoffTests.cs`

**Interfaces:**
- Consumes `OpeningVideoSession` from Task 1.
- Produces `MoodiumAppFlowController.BeginStickerWorldSelection(OpeningVideoSession)` call after tutorial completion.
- Consumed by Task 5.

- [ ] **Step 1: Write failing handoff/timeline tests**

```csharp
[Test]
public void StickerSelectionLoop_UsesChineseQuestionLoopFramesForBothLanguages()
{
    Assert.That(OpeningVideoTimeline.StickerSelectionLoop.StartFrame,
        Is.EqualTo(OpeningVideoTimeline.Chinese.QuestionLoop.StartFrame));
    Assert.That(OpeningVideoTimeline.StickerSelectionLoop.EndFrame,
        Is.EqualTo(OpeningVideoTimeline.Chinese.QuestionLoop.EndFrame));
}

[Test]
public void OpeningCompletion_HandsSessionToAppFlow_WithoutStoppingOrDestroyingDisplay()
{
    var source = File.ReadAllText("Assets/Scripts/Moodium/Opening/OpeningManager.cs");
    StringAssert.Contains("BeginStickerWorldSelection", source);
    StringAssert.DoesNotContain("ShowObjectWorldSelectionFromOpening();", source);
}
```

Include an integration-style test using a prepared `VideoPlayer` and a fake session controller to prove the loop seeks `00:47:00` back to `00:41:10`.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~OpeningStickerSelectionHandoffTests`

Expected: failure because no sticker-loop timeline alias or handoff exists.

- [ ] **Step 3: Add explicit timeline alias and video handoff**

Add:

```csharp
public static readonly Segment StickerSelectionLoop = Chinese.QuestionLoop;
```

After `PlayHardwareTutorial` marks completion, build an `OpeningVideoSession` around the existing player/display/prompt/teardown resources, seek to `StickerSelectionLoop.StartFrame`, and call `m_AppFlow.BeginStickerWorldSelection(session)`. Transfer disposal responsibility to the session instead of executing the current immediate player stop/display destruction block.

Do not change tutorial branch seeks, language/hardware choices or their timeout behavior.

- [ ] **Step 4: Implement visionOS-safe loop behavior in the selection controller**

Use frame comparison and existing seek/play behavior:

```csharp
if (CurrentFrame(player) >= loop.EndFrame || !player.isPlaying)
    SeekToFrameAndPlay(loop.StartFrame);
```

Seek completion must resume playback. A selection loop must never invoke `ShowTutorialLoadFailure` or `OpeningTutorialFlowGate` failure behavior.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~OpeningStickerSelectionHandoffTests`

Expected: exact frame boundaries and non-destructive handoff tests pass.

### Task 4: Gate Existing Calibration and Begin It Only From the Selected Entry

**Files:**
- Modify: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationCoordinator.cs`
- Modify: `Assets/Scripts/ImageTrackingCapsuleSpawner.cs`
- Create: `Assets/Editor/StickerSelectionCalibrationGateTests.cs`

**Interfaces:**
- Consumes `StickerWorldEntry` from Task 1 and selected callback from Task 2.
- Produces `BeginCalibration(StickerWorldEntry)`, `CancelActiveCalibration()`, and `CalibrationCompleted` carrying selected entry plus `SensoryBagCalibrationData`.
- Consumed by Task 5.

- [ ] **Step 1: Write failing mode-gating tests**

```csharp
[Test]
public void StickerSelectionTracking_DoesNotCreateCalibrationSession()
{
    coordinator.EndCalibrationMode();
    var consumed = coordinator.HandleImageAnchorUpdate(CandyId, "CandyWorldPhoneTracking", anchor, TrackingState.Tracking);
    Assert.That(consumed, Is.False);
    Assert.That(coordinator.ActiveSessionCount, Is.EqualTo(0));
}

[Test]
public void BeginCalibration_UsesOnlyTheChosenEntryAnchor()
{
    coordinator.BeginCalibration(chosenNatureEntry);
    coordinator.HandleImageAnchorUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);
    coordinator.HandleImageAnchorUpdate(NatureId, "NatureWorldPhoneTracking", natureAnchor, TrackingState.Tracking);
    Assert.That(coordinator.ActiveSessionCount, Is.EqualTo(1));
    Assert.That(coordinator.GetActiveCalibrator(NatureId).Anchor, Is.SameAs(natureAnchor));
}
```

Add a test that confirmation emits the same selected `TrackableId`, `MoodiumWorldDefinition`, and anchor-local data that began calibration.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~StickerSelectionCalibrationGateTests`

Expected: failure because coordinator currently consumes every Candy/Nature image update.

- [ ] **Step 3: Add explicit coordinator modes**

Add a small enum such as `Inactive` and `CalibratingSelectedEntry`. `HandleImageAnchorUpdate` returns `false` in `Inactive`, so `ImageTrackingCapsuleSpawner` retains normal legacy behavior outside this new flow. In selected calibration mode it consumes only the selected trackable/reference-image pair and creates/resumes exactly one calibrator.

On confirm, retain existing saved calibration data and raise a context-rich completion event. On cancel/exit, hide/destroy active frame state without erasing confirmed anchor-local data unnecessarily.

- [ ] **Step 4: Keep `ImageTrackingCapsuleSpawner` state-aware without changing feedback**

Only adjust the spawner integration needed to call the coordinator and avoid legacy Soft spawning while selected calibration owns that same anchor. Do not change `ChocolateCapsuleInteraction`, feedback subscriptions, audio, progress or tracked-object logic.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~StickerSelectionCalibrationGateTests`

Expected: selection is passive, only the chosen anchor calibrates, and confirmation preserves entry context.

### Task 5: Make App Flow Own Selection → Calibration → World → Selection

**Files:**
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- Modify: `Assets/Scripts/Moodium/Opening/OpeningManager.cs`
- Create: `Assets/Editor/MoodiumStickerWorldFlowTests.cs`

**Interfaces:**
- Consumes `OpeningVideoSession`, `StickerWorldSelectionController`, `StickerWorldEntry`, and calibration completion event from Tasks 1–4.
- Produces `BeginStickerWorldSelection(OpeningVideoSession)` and World-back behavior returning to selection.

- [ ] **Step 1: Write failing end-to-end state tests**

```csharp
[Test]
public void WorldExit_ReturnsToStickerSelection_NotLegacyCarousel()
{
    var source = File.ReadAllText("Assets/Scripts/Moodium/MoodiumAppFlowController.cs");
    StringAssert.Contains("BeginStickerWorldSelection", source);
    StringAssert.Contains("ReturnToStickerWorldSelection", source);
    StringAssert.DoesNotContain("ShowObjectWorldSelection();\n        }\n\n        void SelectObjectWorld", source);
}

[Test]
public void CalibrationConfirmation_StartsOnlyTheSelectedRealityWorld()
{
    flow.BeginStickerWorldSelection(session);
    flow.OnStickerWorldEntrySelected(natureEntry);
    flow.OnCalibrationCompleted(natureEntry, confirmedNatureData);
    Assert.That(flow.SelectedWorld.WorldId, Is.EqualTo("nature"));
    Assert.That(flow.CurrentState, Is.EqualTo(MoodiumOpeningFlowState.WorldInteraction));
}
```

Add a test that `ReturnToStickerWorldSelection` calls the existing `StopCandyWorldExperience` before re-enabling image tracking and video selection.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~MoodiumStickerWorldFlowTests`

Expected: failure because the app flow has no sticker-selection state or entry point.

- [ ] **Step 3: Add top-level flow state and owner methods**

Add a private enum/state field for `StickerWorldSelection`, `WorldCalibration`, and `WorldInteraction`. Implement:

```csharp
public void BeginStickerWorldSelection(OpeningVideoSession session);
void OnStickerWorldEntrySelected(StickerWorldEntry entry);
void OnCalibrationCompleted(StickerWorldEntry entry, SensoryBagCalibrationData data);
void ReturnToStickerWorldSelection();
```

`BeginStickerWorldSelection` hides legacy panels, stops stale world state, enables only image tracking, begins selection controller and shows the session. `OnStickerWorldEntrySelected` ends selection UI, hides session and begins calibration. Confirmation calls the existing world-start path only after setting the selected world and storing calibration data. Do not make data affect Soft scale yet.

- [ ] **Step 4: Replace only relevant old routing points**

Route opening completion and Reality World Back through `ReturnToStickerWorldSelection`. Preserve legacy `ShowObjectWorldSelection` implementation for non-opening/editor paths unless it is provably unused; do not delete unrelated world picker code or creative-space flows.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~MoodiumStickerWorldFlowTests`

Expected: state transitions select the intended anchor/world and Back restores video-backed tracking selection only after cleanup.

### Task 6: Scene Wiring, Regression Validation and Manual Test Checklist

**Files:**
- Modify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity`
- Modify: `Assets/Editor/MoodiumPortalFlowTests.cs`
- Modify: `Assets/Editor/DualTrackingRealityFlowTests.cs`
- Create: `Assets/Editor/StickerWorldSelectionSceneValidationTests.cs`

**Interfaces:**
- Consumes all runtime components from Tasks 1–5.
- Produces configured scene references for the existing tracked-image manager, Candy/Nature World UI prefabs, selected-world definitions, calibration coordinator and fonts/button sprite.

- [ ] **Step 1: Write failing scene validation tests**

```csharp
[Test]
public void MeshingScene_HasOneConfiguredStickerWorldSelectionController()
{
    var scene = EditorSceneManager.OpenScene("Assets/Samples/PolySpatial/Scenes/Meshing 1.unity");
    var controllers = Object.FindObjectsByType<StickerWorldSelectionController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    Assert.That(controllers.Length, Is.EqualTo(1));
    Assert.That(controllers[0].CandyWorldUiPrefab, Is.Not.Null);
    Assert.That(controllers[0].NatureWorldUiPrefab, Is.Not.Null);
}
```

Add scene assertions that the controller references the same `ARTrackedImageManager` used by Moodium flow and that both `CandyWorldUI.prefab` and `NatureWorldUI.prefab` are configured.

- [ ] **Step 2: Run scene tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~StickerWorldSelectionSceneValidationTests`

Expected: failure because no configured controller is present in the scene.

- [ ] **Step 3: Wire existing scene objects through Unity serialization**

Add the new controller to the existing Moodium flow root in `Meshing 1.unity`. Assign the current tracked-image manager, Candy/Nature world definitions, `Assets/prefab/CandyWorldUI.prefab`, `Assets/prefab/NatureWorldUI.prefab`, existing fonts and rounded-button sprite. Assign the existing calibration coordinator and app flow references. Do not manually modify prefab model meshes or reference-image library ordering.

- [ ] **Step 4: Execute automated regression suite**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter "FullyQualifiedName~StickerWorld|FullyQualifiedName~Opening|FullyQualifiedName~SensoryBagCalibration|FullyQualifiedName~DualTrackingRealityFlow|FullyQualifiedName~MoodiumPortalFlow"`

Expected: all focused new and affected existing tests pass.

- [ ] **Step 5: Run Editor manual flow**

1. Run `Meshing 1.unity`.
2. Complete either language/hardware tutorial path.
3. Verify video loops `00:41:10–00:47:00` with the correct localized instruction.
4. Simulate Candy and Nature tracked-image updates; verify both entry prefabs and buttons coexist.
5. Click one entry with the mouse; verify video/other entry hides and only its calibration frame appears.
6. Confirm calibration; verify only the selected World begins.
7. Exit World; verify runtime world content is cleared and video-backed sticker selection returns.

- [ ] **Step 6: Run Vision Pro manual flow**

1. Build and install the same scene on Vision Pro.
2. Finish each of the four language/hardware branches and verify all reach the same sticker loop rather than a legacy carousel.
3. Show Candy, Nature, then both stickers together; confirm each world model follows its own physical sticker.
4. Use gaze + pinch and direct hand touch to press each entry button.
5. Verify a selected world opens its calibration UI at the selected sticker, not the other visible sticker.
6. Confirm calibration, enter the world, exit it, and verify the video/display/selection state returns without a frozen frame or stale Nature trees.
