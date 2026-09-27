# Sensory Bag Calibration Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a recognized Candy or Nature image anchor drive a movable, resizable, coplanar bag-calibration frame whose confirmed data is retained for later bag geometry.

**Architecture:** Keep `ARTrackedImageManager` and `ImageTrackingCapsuleSpawner` as the image-anchor source. Add a calibration coordinator and isolated plane-editing components; route their handles through the existing PolySpatial spatial-pointer controller. No Soft Prefab is created while a calibration session is active.

**Tech Stack:** Unity 6000.0.30f1, C#, AR Foundation 6.0.3, PolySpatial 2.4.3, visionOS XR 2.4.3, XR Hands 1.5.0, NUnit Editor tests.

**Spec:** `docs/superpowers/specs/2026-09-25-sensory-bag-calibration-design.md`

## Global Constraints

- Do not create an ARKit session or replace `ARTrackedImageManager`, XR Hands, or existing image-tracking subscriptions.
- Do not modify `CandyWorldManager`, existing feedback wiring, or hand-impact/press logic in this phase.
- `CandyWorldPhoneTracking` and `NatureWorldPhoneTracking` must both enter the same calibration flow.
- All editable calibration geometry is Image Anchor local X/Y; anchor-local Z remains fixed.
- Default frame size is 0.25 m × 0.17 m; min/max dimensions and default depth are Inspector-configurable.
- Tracking loss retains session values and hides UI; it must not reset or destroy data on a transient loss.

## Review Focus

- Pointer depth varies while editing: projection must discard anchor-local Z; Task 2 tests this.
- A handle crosses the opposite edge: dimensions must clamp instead of flip; Task 2 tests this.
- A tracking update moves the anchor: local geometry must remain invariant; Task 3 tests this.
- A `TrackingState.None` update occurs briefly: unsaved calibration must remain in memory; Task 4 tests this.
- A recognized image is not Candy or Nature: no calibration should start and legacy behavior must not be silently broken; Task 4 tests this.

---

### Task 1: Calibration Data Contract

**Files:**
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationData.cs`
- Create: `Assets/Editor/SensoryBagCalibrationDataTests.cs`

**Interfaces:**
- Produces: `Moodium.Calibration.SensoryBagWorldKind`, `SensoryBagCalibrationData`, and `SensoryBagCalibrationSettings`.
- Consumed by: Tasks 2–4.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test]
public void DefaultData_UsesConfiguredBagSizeAndOffset()
{
    var settings = new SensoryBagCalibrationSettings(0.25f, 0.17f, 0.05f, new Vector2(.03f, -.02f));
    var data = SensoryBagCalibrationData.CreateDefault("CandyWorldPhoneTracking", SensoryBagWorldKind.Candy, settings);
    Assert.That(data.Width, Is.EqualTo(.25f));
    Assert.That(data.Height, Is.EqualTo(.17f));
    Assert.That(data.CenterOffset, Is.EqualTo(new Vector2(.03f, -.02f)));
}
```

- [ ] **Step 2: Run the test and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationDataTests`

Expected: compile failure because calibration types do not exist.

- [ ] **Step 3: Implement the serializable contract**

```csharp
public enum SensoryBagWorldKind { Candy, Nature }

[Serializable]
public struct SensoryBagCalibrationData
{
    public string ReferenceImageName;
    public SensoryBagWorldKind WorldKind;
    public float Width, Height, Depth;
    public Vector2 CenterOffset;
    public Vector3 PlaneLocalOffset;
    public Quaternion LocalOrientation;
}
```

Use a settings struct to hold defaults and clamp limits without coupling this data to a MonoBehaviour.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationDataTests`

Expected: all `SensoryBagCalibrationDataTests` pass.

### Task 2: Plane-Only Rectangle Model and Handle Math

**Files:**
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationRectangle.cs`
- Create: `Assets/Editor/SensoryBagCalibrationRectangleTests.cs`

**Interfaces:**
- Consumes: `SensoryBagCalibrationData`, `SensoryBagCalibrationSettings` from Task 1.
- Produces: `MoveTo`, `ResizeFromHandle`, `ProjectWorldPointToAnchorPlane`, and current data.
- Consumed by: Tasks 3–4.

- [ ] **Step 1: Write failing behavior tests**

```csharp
[Test]
public void RightEdgeResize_HoldsLeftEdgeFixed()
{
    var rectangle = CreateRectangle(width: .25f, height: .17f, center: Vector2.zero);
    rectangle.ResizeFromHandle(CalibrationHandleKind.Right, new Vector2(.20f, 0f));
    Assert.That(rectangle.Data.Width, Is.EqualTo(.325f).Within(.0001f));
    Assert.That(rectangle.Left, Is.EqualTo(-.125f).Within(.0001f));
}

[Test]
public void ProjectWorldPointToAnchorPlane_DiscardsLocalDepth()
{
    var local = SensoryBagCalibrationRectangle.ProjectWorldPointToAnchorPlane(anchor, anchor.TransformPoint(new Vector3(.1f, -.03f, 3f)));
    Assert.That(local, Is.EqualTo(new Vector2(.1f, -.03f)).Using(Vector2ComparerWithEqualsOperator.Instance));
}
```

Add separate tests for a top-right corner preserving bottom-left and a min-width clamp avoiding flips.

- [ ] **Step 2: Run the tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationRectangleTests`

Expected: compile failure because rectangle model and handle enum do not exist.

- [ ] **Step 3: Implement pure rectangle operations**

```csharp
public void ResizeFromHandle(CalibrationHandleKind handle, Vector2 point)
{
    // Read the fixed opposite coordinate, clamp the moving coordinate,
    // then recompute center and size from the two coordinates.
}
```

Implement body movement, four edge modes, four corner modes, and clamped dimensions. Keep world/Transform code limited to the static projection helper.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationRectangleTests`

Expected: all rectangle tests pass.

### Task 3: Runtime Frame, Handles, Buttons, and Confirmed Session Data

**Files:**
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrator.cs`
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationHandle.cs`
- Create: `Assets/Editor/SensoryBagCalibratorTests.cs`

**Interfaces:**
- Consumes: Task 1 data and Task 2 rectangle operations.
- Produces: `Begin(Transform, SensoryBagCalibrationData, SensoryBagCalibrationSettings)`, `PointerStarted`, `PointerMoved`, `PointerEnded`, `ResetCalibration`, `ConfirmCalibration`, and `CalibrationConfirmed`.
- Consumed by: Task 4.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Confirm_EmitsAnchorLocalDataAndHidesFrame()
{
    var calibrator = CreateCalibrator(anchor);
    calibrator.Begin(anchor.transform, DefaultData(), DefaultSettings());
    calibrator.MoveFrameToAnchorPlane(new Vector2(.08f, -.04f));
    SensoryBagCalibrationData received = default;
    calibrator.CalibrationConfirmed += data => received = data;
    calibrator.ConfirmCalibration();
    Assert.That(received.CenterOffset, Is.EqualTo(new Vector2(.08f, -.04f)));
    Assert.That(calibrator.FrameVisible, Is.False);
}
```

Add a test that moving the anchor changes frame world pose but not local width, height, or offset.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibratorTests`

Expected: compile failure because the calibrator does not exist.

- [ ] **Step 3: Implement runtime visual frame**

Create border bars and eight handle objects with BoxColliders as children of `CalibrationRoot`. Give every handle a large, configurable physical hit extent. Build Confirm/Reset using the project’s `MoodiumSpatialButton`; do not add XRI. In `LateUpdate`, update root pose from the current Image Anchor and apply only anchor-local data.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibratorTests`

Expected: all calibrator tests pass.

### Task 4: Existing Image Tracking Integration and Pointer Routing

**Files:**
- Modify: `Assets/Scripts/ImageTrackingCapsuleSpawner.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumSpatialInteractionController.cs`
- Create: `Assets/Scripts/Moodium/Calibration/SensoryBagCalibrationCoordinator.cs`
- Create: `Assets/Editor/SensoryBagCalibrationCoordinatorTests.cs`
- Modify: `Assets/Editor/ImageTrackingCapsuleSpawnerTests.cs`

**Interfaces:**
- Consumes: Task 3 `SensoryBagCalibrator` lifecycle and `CalibrationConfirmed` event.
- Produces: one calibration session per recognized tracked image and retained `TryGetConfirmedCalibration` data.

- [ ] **Step 1: Write failing integration tests**

```csharp
[Test]
public void RecognizedCandyImage_StartsCalibrationWithoutCreatingSoftPrefab()
{
    var harness = CreateCoordinatorHarness();
    harness.Report("CandyWorldPhoneTracking", TrackingState.Tracking);
    Assert.That(harness.Coordinator.ActiveSessionCount, Is.EqualTo(1));
    Assert.That(harness.LegacySpawnerInstanceCount, Is.EqualTo(0));
}

[Test]
public void TemporaryTrackingLoss_HidesButDoesNotResetActiveCalibration()
{
    var harness = CreateCoordinatorHarness();
    harness.Report("NatureWorldPhoneTracking", TrackingState.Tracking);
    harness.Calibrator.MoveFrameToAnchorPlane(new Vector2(.06f, .02f));
    harness.Report("NatureWorldPhoneTracking", TrackingState.None);
    Assert.That(harness.Calibrator.CurrentData.CenterOffset, Is.EqualTo(new Vector2(.06f, .02f)));
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationCoordinatorTests`

Expected: compile failure because coordinator and calibration-specific image events do not exist.

- [ ] **Step 3: Implement narrow integration changes**

Add an Image Tracking event that passes `TrackableId`, reference-image name, anchor Transform, and `TrackingState`. Let coordinator consume only Candy/Nature names and prevent the existing immediate Soft Prefab path only while calibration is enabled. Add calibration handle routing before generic selections in `MoodiumSpatialInteractionController`.

- [ ] **Step 4: Run targeted integration tests and existing image-spawner tests**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter "FullyQualifiedName~SensoryBagCalibrationCoordinatorTests|FullyQualifiedName~ImageTrackingCapsuleSpawnerTests"`

Expected: all listed tests pass.

### Task 5: Phase 1 Compile, Regression Verification, and Inspector Setup

**Files:**
- Modify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity` only if a serialized coordinator reference is required.
- Modify: `Assets/Editor/SensoryBagCalibrationCoordinatorTests.cs` if scene assertions need the final serialized reference.

**Interfaces:**
- Consumes: all preceding runtime components.
- Produces: a Phase 1-ready scene with no Candy/Nature Soft Prefab creation during calibration.

- [ ] **Step 1: Write a failing scene configuration test**

```csharp
[Test]
public void MeshingScene_HasCalibrationCoordinatorAndImageSpawnerReference()
{
    using var scene = OpenMeshingSceneForTest();
    Assert.That(Object.FindFirstObjectByType<SensoryBagCalibrationCoordinator>(FindObjectsInactive.Include), Is.Not.Null);
}
```

- [ ] **Step 2: Run test and verify RED**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SensoryBagCalibrationCoordinatorTests.MeshingScene`

Expected: failure because scene is not yet configured.

- [ ] **Step 3: Configure only required scene references**

Attach/configure coordinator on the existing Moodium host object, reference its existing `ImageTrackingCapsuleSpawner`, and set defaults: width `.25`, height `.17`, depth `.05`. Do not modify `CandyWorldManager`.

- [ ] **Step 4: Run Phase 1 suite and compile verification**

Run: `dotnet build Assembly-CSharp-Editor.csproj --nologo && dotnet test Assembly-CSharp-Editor.csproj --no-build --filter "FullyQualifiedName~SensoryBagCalibration|FullyQualifiedName~ImageTrackingCapsuleSpawnerTests" && git diff --check`

Expected: build has zero errors, all listed tests pass, and diff check is clean.
