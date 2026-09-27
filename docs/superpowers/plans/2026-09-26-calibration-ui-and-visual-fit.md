# Calibration UI and Visual-Fit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make sticker-selection text and entry controls readable, make bag calibration visibly operable in Vision Pro, and turn confirmed calibration bounds into a tracked Soft visual plus independent interaction volume.

**Architecture:** Keep the existing ARTrackedImageManager → StickerWorldSelectionController → SensoryBagCalibrationCoordinator flow. Replace only runtime presentation layers: selection labels compensate their parent scale; the calibrator renders bright, separately scaled controls; a `CalibratedSensoryBagPresentation` creates `VisualRoot` and `InteractionRoot` from saved anchor-local calibration data.

**Tech Stack:** Unity 6000.0.30f1, C#, AR Foundation tracked images, PolySpatial spatial pointers, TextMeshPro, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-09-26-calibration-ui-and-visual-fit-design.md`

## Global Constraints

- Reuse the scene's existing ARFoundation image tracking and PolySpatial hand/spatial-pointer interaction; do not add an ARKit session or a tracking package.
- Do not modify CandyWorldManager, Impact/Press recognition, or world feedback behavior.
- Keep calibration positions and dimensions in image-anchor local space.
- Preserve all pre-existing dirty files; no reset, checkout, revert, clean, stash, or automatic Git commit.
- Do not change source prefab assets while fitting runtime Soft visuals.
- Phase 2 supplies geometry only; it does not activate new feedback behavior.

## Review Focus

- A non-uniform video-display parent must not squash any Chinese or English glyph; Task 1 tests compensated scale.
- A visionOS UUID-suffixed reference image must still create the correct entry and calibrated presentation; Task 4 tests anchor-local data independently of the raw name.
- A small/large calibration must never create a negative or zero interaction collider; Task 3 tests dimensions and bounds.
- Brief `TrackingState.None` must hide the runtime bag without forgetting saved calibration; Task 4 tests recovery using the same root.
- The Soft prefab's authored root scale must be included in native bounds calculation so the visual fit is correct; Task 3 tests a nested scaled renderer.

---

## File structure

- Modify `Assets/Scripts/Moodium/Flow/StickerWorldSelectionController.cs`: expose entry offset defaults, use independent visual/label transforms, and compensate the instruction label for the video display scale.
- Modify `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrator.cs`: replace dim primitive presentation with high-contrast border/handle/button visuals while preserving all existing drag APIs.
- Create `Assets/Scripts/Moodium/Calibration/SensoryBagInteractionVolume.cs`: own calibrated BoxCollider dimensions only.
- Create `Assets/Scripts/Moodium/Calibration/CalibratedSensoryBagPresentation.cs`: own anchor-local pose, runtime visual fit, visibility and cleanup for one bag.
- Modify `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationCoordinator.cs`: create/update the presentation after Confirm and route tracking loss/removal to it.
- Modify `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`: pass the selected Soft prefab to the coordinator after calibration rather than discarding calibration data.
- Modify existing EditMode tests and add `Assets/Editor/CalibratedSensoryBagPresentationTests.cs`.

### Task 1: Correct selection prompt and entry-control typography

**Files:**
- Modify: `Assets/Scripts/Moodium/Flow/StickerWorldSelectionController.cs`
- Test: `Assets/Editor/StickerWorldSelectionControllerTests.cs`

**Interfaces:**
- Produces `GetCompensatedLocalScale(float targetWorldScale, Vector3 parentLossyScale)` and a test-visible `EntryLocalPosition` property.
- Consumes existing `OpeningVideoSubtitlePresenter` scale-compensation convention.

- [ ] **Step 1: Write failing EditMode tests**

```csharp
[Test]
public void CompensatedTextScale_CancelsNonUniformParentScale()
{
    var result = StickerWorldSelectionController.GetCompensatedLocalScale(
        .045f, new Vector3(1.28f, .72f, 1f));
    Assert.That(result, Is.EqualTo(new Vector3(.045f / 1.28f, .045f / .72f, .045f)));
}

[Test]
public void DefaultEntryOffset_IsRaisedAndRecessedFromSticker()
{
    Assert.That(m_Controller.EntryLocalPosition.y, Is.GreaterThanOrEqualTo(.12f));
    Assert.That(m_Controller.EntryLocalPosition.z, Is.GreaterThanOrEqualTo(.10f));
}
```

- [ ] **Step 2: Run the focused test assembly and verify the new tests fail**

Run in Unity Test Runner: `StickerWorldSelectionControllerTests`.
Expected: compilation failure for missing helper/property or assertion failure for the existing zero offset.

- [ ] **Step 3: Implement scale-safe text hierarchy**

Create a logical `Enter World Button` root with its BoxCollider and `MoodiumSpatialButton`; put the flattened cube under `Button Visual`; put TextMeshPro under an unscaled `Button Label` sibling. For the selection instruction, calculate each local scale axis as target world size divided by the display parent's lossy scale.

```csharp
public static Vector3 GetCompensatedLocalScale(float target, Vector3 parentScale) => new(
    target / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)),
    target / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)),
    target / Mathf.Max(.0001f, Mathf.Abs(parentScale.z)));
```

Set default `m_EntryLocalPosition` to `(0f, .12f, .10f)` and retain serialized Inspector override support. Do not move any image anchor or alter reference-image matching.

- [ ] **Step 4: Run `StickerWorldSelectionControllerTests`**

Expected: all existing mapping/recovery tests plus the two added tests pass.

- [ ] **Step 5: Inspect the Game view**

Enter Play Mode, trigger sticker selection, and confirm the bilingual prompt and `Enter World` label remain proportionate when their parent video display uses scale `(1.28, .72, 1)`.

### Task 2: Replace dim calibration presentation with usable Vision Pro controls

**Files:**
- Modify: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrator.cs`
- Modify: `Assets/Editor/SensoryBagCalibratorTests.cs`

**Interfaces:**
- Preserves `BeginPointerDrag`, `MovePointerDrag`, `ResetCalibration`, `ConfirmCalibration`, and `FrameRoot`.
- Produces named roots `Calibration Confirm Button` and `Calibration Reset Button`, each with visible label and independently sized collider.

- [ ] **Step 1: Add failing visual hierarchy tests**

```csharp
[Test]
public void Begin_CreatesBrightFrameHandlesAndReadableActionLabels()
{
    var calibrator = CreateCalibrator();
    calibrator.Begin(m_Anchor.transform, DefaultData(), DefaultSettings());
    Assert.That(calibrator.FrameRoot.Find("Top Left Corner Handle")?.GetComponent<Renderer>().enabled, Is.True);
    Assert.That(calibrator.FrameRoot.Find("Calibration Confirm Button/Label"), Is.Not.Null);
    Assert.That(calibrator.FrameRoot.Find("Calibration Reset Button/Label"), Is.Not.Null);
}
```

- [ ] **Step 2: Run `SensoryBagCalibratorTests` and verify failure**

Expected: the action-label hierarchy assertion fails because current labels are direct children of flattened cube buttons.

- [ ] **Step 3: Implement bright border, capsule handles and action controls**

Keep the existing invisible frame drag collider. Build visible border segments separately from edge-hit targets; use a shared runtime material that writes a cool-white `_BaseColor`, `_Color`, and HDR `_EmissionColor` where the installed shader exposes it. Use larger default values: border thickness `.012m`, corner/edge visual size `.045m`, and hit depth `.04m`.

Use capsule primitives for visual handles and retain independent cube/collider hit targets. Build each action with an unscaled root and collider, a flattened visual child, and a TextMeshPro label child named `Label`; set labels to `确认 / Confirm` and `重置 / Reset`. Position both `.10m` below the frame, with a minimum `.16m × .06m` hit area.

Do not change `SensoryBagCalibrationRectangle` math or project pointer depth into the plane differently.

- [ ] **Step 4: Run `SensoryBagCalibratorTests`**

Expected: existing confirm/reset/anchor tests pass, and new hierarchy test proves visible controls are present.

- [ ] **Step 5: Editor interaction smoke test**

In Play Mode, confirm each edge and corner remains selectable, frame-body drag moves only centre offset, `Reset` restores defaults, and `Confirm` hides all frame controls.

### Task 3: Add a calibrated presentation and independent interaction volume

**Files:**
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagInteractionVolume.cs`
- Create: `Assets/Scripts/Moodium/Calibration/CalibratedSensoryBagPresentation.cs`
- Create: `Assets/Editor/CalibratedSensoryBagPresentationTests.cs`

**Interfaces:**
- `SensoryBagInteractionVolume.Configure(float width, float height, float depth)` sets `BoxCollider.size = new Vector3(width,height,depth)`.
- `CalibratedSensoryBagPresentation.Configure(Transform anchor, SensoryBagCalibrationData data, GameObject visualPrefab)` creates `VisualRoot` and `InteractionRoot`.
- `SetTrackingVisible(bool visible)` hides/shows roots without losing calibration.

- [ ] **Step 1: Write failing runtime hierarchy tests**

```csharp
[Test]
public void Configure_FitsVisualAndCreatesExactInteractionVolume()
{
    var p = root.AddComponent<CalibratedSensoryBagPresentation>();
    p.Configure(anchor.transform, data, CreatePrefabWithRenderer(new Vector3(2f, 1f, 1f)));
    Assert.That(p.InteractionVolume.Collider.size, Is.EqualTo(new Vector3(.25f, .17f, .05f)));
    Assert.That(p.VisualRoot.localScale.x / p.VisualRoot.localScale.y, Is.EqualTo((.25f / 2f) / (.17f / 1f)).Within(.0001f));
}

[Test]
public void TrackingVisibility_RecoversSamePresentationWithoutChangingData()
{
    p.SetTrackingVisible(false); p.SetTrackingVisible(true);
    Assert.That(p.CurrentCalibration.CenterOffset, Is.EqualTo(data.CenterOffset));
    Assert.That(p.VisualRoot.gameObject.activeSelf, Is.True);
}
```

- [ ] **Step 2: Run the new tests and verify failure**

Run in Unity Test Runner: `CalibratedSensoryBagPresentationTests`.
Expected: compile errors until the two new components exist.

- [ ] **Step 3: Implement `SensoryBagInteractionVolume`**

Give it one `BoxCollider`, disable trigger/physics response only if the existing runtime setup requires that, and expose read-only `Width`, `Height`, `Depth`, `Collider`. Its transform is at calibrated bag centre and its collider size represents local calibrated metres exactly.

- [ ] **Step 4: Implement `CalibratedSensoryBagPresentation`**

On configure, create:

```text
CalibratedBagRoot (presentation component)
├── VisualRoot
│   └── instantiated Soft prefab
└── InteractionRoot
    └── SensoryBagInteractionVolume
```

Set both child roots from `new Vector3(data.CenterOffset.x, data.CenterOffset.y, data.PlaneLocalOffset.z)` and `data.LocalOrientation`. Compute renderer bounds in visual-prefab-root local coordinates by encapsulating each `Renderer.localToWorldMatrix` transformed into root space; derive visual x/y native size, clamp to `.0001f`, and set VisualRoot scale to `(data.Width/nativeWidth, data.Height/nativeHeight, 1f)`. Keep prefab authored scale inside the instantiated visual. Parent presentation to anchor or LateUpdate-sync to its world pose without changing the data.

- [ ] **Step 5: Run `CalibratedSensoryBagPresentationTests`**

Expected: visual fit includes renderer scale, interaction collider matches data, and visibility recovery preserves data/root identity.

### Task 4: Connect calibration confirmation to the presentation lifecycle

**Files:**
- Modify: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationCoordinator.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- Modify: `Assets/Editor/SensoryBagCalibrationCoordinatorTests.cs`

**Interfaces:**
- Coordinator gains `ConfigureVisualPrefab(SensoryBagWorldKind kind, GameObject prefab)` and `GetPresentation(TrackableId id)`.
- App flow calls the coordinator configuration before `BeginCalibration` and does not call legacy image-spawner model generation for the selected calibration anchor.

- [ ] **Step 1: Add failing coordinator lifecycle tests**

```csharp
[Test]
public void Confirm_CreatesCalibratedPresentationForSelectedWorld()
{
    coordinator.ConfigureVisualPrefab(SensoryBagWorldKind.Candy, softPrefab);
    coordinator.BeginCalibration(entry);
    coordinator.GetActiveCalibrator(entry.TrackableId).ConfirmCalibration();
    Assert.That(coordinator.GetPresentation(entry.TrackableId).InteractionVolume.Width, Is.EqualTo(.25f));
}

[Test]
public void TrackingLoss_HidesPresentationButKeepsConfirmedCalibration()
{
    coordinator.HandleImageAnchorUpdate(id, imageName, anchor, TrackingState.None);
    Assert.That(coordinator.TryGetConfirmedCalibration(id, out _), Is.True);
    Assert.That(coordinator.GetPresentation(id).VisualRoot.gameObject.activeSelf, Is.False);
}
```

- [ ] **Step 2: Run `SensoryBagCalibrationCoordinatorTests` and verify failure**

Expected: missing presentation API compile failure.

- [ ] **Step 3: Implement coordinator ownership and app-flow configuration**

Store one `CalibratedSensoryBagPresentation` per `TrackableId`. On calibration confirmed, replace only a presentation for the same id, preserve `m_ConfirmedData`, and invoke existing `CalibrationCompleted`. On stable tracking update, rebind anchor/show presentation; on `None`, hide it; on permanent scene teardown destroy it.

In `MoodiumAppFlowController.OnCalibrationCompleted`, select the world only after configuring the corresponding visual prefab through the coordinator. Do not edit `CandyWorldManager`; do not enable the old image-spawner instance for the calibrated id.

- [ ] **Step 4: Run coordinator and presentation tests**

Expected: all calibration coordinator tests and presentation tests pass.

- [ ] **Step 5: Run relevant EditMode regression set**

Run in Unity Test Runner:

```text
StickerWorldSelectionControllerTests
SensoryBagCalibratorTests
SensoryBagCalibrationRectangleTests
SensoryBagCalibrationDataTests
SensoryBagCalibrationCoordinatorTests
CalibratedSensoryBagPresentationTests
```

Expected: all selected tests pass. Record unrelated suite failures separately; do not modify unrelated systems.

### Task 5: Scene and Vision Pro verification without committing unrelated work

**Files:**
- Modify only if needed: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity` (serialized references to an existing coordinator/prefabs only)
- No Git commit.

- [ ] **Step 1: Inspect the scene's existing App Flow / calibration coordinator references**

Confirm serialized references point to the scene's existing `ARTrackedImageManager`, `ImageTrackingCapsuleSpawner`, `CandySoft`, and `NatureSoft`; do not replace the scene tracking manager or image library.

- [ ] **Step 2: Wire only missing references**

If a serialized field cannot be configured at runtime, assign the existing scene assets. Do not edit any unrelated scene object, light, video, portal, or world manager.

- [ ] **Step 3: Editor end-to-end test**

1. Enter the video sticker-selection state.
2. Simulate/observe either supported tracked image.
3. Verify the raised/recessed world UI and normal text ratio.
4. Enter a world; move/resize frame; use Reset; Confirm.
5. Verify generated `CalibratedBagRoot` has `VisualRoot` and `InteractionRoot` and inspector dimensions match the frame.

- [ ] **Step 4: Vision Pro device acceptance test**

1. Build and install only after Unity compiles without errors.
2. At `请对准贴纸 / Look at the sticker`, present one supported sticker and inspect UI distance.
3. Enter world, drag every handle and body using gaze + pinch, and press visible bilingual Confirm.
4. Compare Soft visual outline with real bag after confirmation.
5. Slightly move/occlude sticker and verify it recovers without resetting dimensions.

- [ ] **Step 5: Report changed paths and leave Git state untouched**

Use `git status --short` and report only files touched by this plan. Do not stage, commit, reset, stash, clean, checkout, or revert any path.

## Plan self-review

- Spec coverage: selection typography/pose is Task 1; clear frame/handles/buttons is Task 2; visual fit and interaction volume is Task 3; existing-tracking lifecycle integration is Task 4; editor/device validation is Task 5.
- Placeholder scan: no TBD/TODO/"similar to" steps; each task contains explicit test and implementation direction.
- Type consistency: Task 3 defines `SensoryBagInteractionVolume.Configure`, `CalibratedSensoryBagPresentation.Configure`, `SetTrackingVisible`, `InteractionVolume`, `VisualRoot`, and `CurrentCalibration`; Task 4 consumes those exact names.
- Review focus coverage: parent scale Task 1; UUID normalisation/no-name dependency Task 4; volume dimensions and prefab authored scale Task 3; tracking recovery Task 4.
