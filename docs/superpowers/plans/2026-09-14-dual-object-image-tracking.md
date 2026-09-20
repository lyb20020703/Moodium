# Dual Object and Image Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run Object Tracking and Image Tracking simultaneously so every detected source owns a following, independently touchable Chocolate Capsule that contributes to one shared Candy Energy progress.

**Architecture:** Keep ARFoundation-specific lifecycle code in separate object and image spawners, but configure both through one shared Capsule factory. The Reality flow registers every live Capsule with a multi-target palm collision controller, while the existing static Capsule event continues to feed one CandyWorldManager.

**Tech Stack:** Unity 6.0.30f1, AR Foundation 6.0.3, Apple visionOS XR Plugin 2.4.3, XR Hands 1.4.1, PolySpatial, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-09-14-dual-object-image-tracking-design.md`

## Global Constraints

- Object Tracking and Image Tracking are simultaneously active only in Reality Enhancement Mode.
- `Assets/Moodium/TestImageTracking.png` is a fixed `0.10 m × 0.10 m` reference image.
- Every trackable owns one `Assets/prefab/Chocolate_Capsule.prefab` instance.
- Touch begins on Palm/Capsule Collider overlap; no velocity, force, or dwell threshold is added.
- All Capsules share the existing Candy Energy and progress controllers.
- Creative Space behavior is unchanged.

---

### Task 1: Shared tracked Capsule construction

**Files:**
- Create: `Assets/Scripts/Moodium/Reality/TrackedCapsuleRuntimeFactory.cs`
- Modify: `Assets/Scripts/TissueObjectTrackingSpawner.cs`
- Test: `Assets/Editor/TrackedCapsuleFactoryTests.cs`

**Interfaces:**
- Produces: `TrackedCapsuleRuntimeFactory.Create(GameObject prefab, Transform anchor, Vector3 localPosition, Quaternion localRotation, string instanceName) -> GameObject`.
- Preserves: Object Capsules have movement/scale disabled, explicit world-pose following, enabled `ChocolateCapsuleInteraction`, and disabled Spatial Pointer.

- [x] **Step 1: Write the failing test**

```csharp
[Test]
public void FactoryCreatesInteractiveNonScalableFollowingCapsule()
{
    var capsule = TrackedCapsuleRuntimeFactory.Create(prefab, anchor, Vector3.zero, Quaternion.identity, "Tracked Capsule");
    Assert.That(capsule.GetComponent<TrackedObjectPoseFollower>().Anchor, Is.EqualTo(anchor));
    Assert.That(capsule.GetComponent<ChocolateCapsuleInteraction>().InteractionEnabled, Is.True);
    Assert.That(capsule.GetComponent<ChocolateCapsuleInteraction>().SpatialPointerEnabled, Is.False);
    Assert.That(capsule.GetComponent<MoodiumManipulable>().CanManipulate, Is.False);
}
```

- [x] **Step 2: Run the targeted EditMode test**

Run `TrackedCapsuleFactoryTests`; expect failure because `TrackedCapsuleRuntimeFactory` does not exist.

- [x] **Step 3: Implement the factory and delegate Object spawning to it**

```csharp
public static GameObject Create(GameObject prefab, Transform anchor, Vector3 localPosition,
    Quaternion localRotation, string instanceName)
{
    var instance = Object.Instantiate(prefab);
    instance.name = instanceName;
    var manipulable = MoodiumRuntimeObjectSetup.Configure(instance, false, false, true);
    manipulable?.SetInteractionState(MoodiumInteractionState.InteractionMode);
    var follower = instance.GetComponent<TrackedObjectPoseFollower>() ?? instance.AddComponent<TrackedObjectPoseFollower>();
    follower.Configure(anchor, localPosition, localRotation);
    var interaction = instance.GetComponent<ChocolateCapsuleInteraction>() ?? instance.AddComponent<ChocolateCapsuleInteraction>();
    interaction.SetInteractionEnabled(true);
    interaction.SetSpatialPointerEnabled(false);
    return instance;
}
```

- [x] **Step 4: Run factory and existing tracked-Capsule tests**

Expected: all targeted tests pass and Object Tracking still produces a non-scalable Capsule.

### Task 2: Image Tracking lifecycle and reference library

**Files:**
- Create: `Assets/Scripts/ImageTrackingCapsuleSpawner.cs`
- Create: `Assets/Moodium/Tracking/MoodiumImageReferenceLibrary.asset`
- Modify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity`
- Test: `Assets/Editor/ImageTrackingCapsuleSpawnerTests.cs`

**Interfaces:**
- Produces: `CapsuleAvailable(ChocolateCapsuleInteraction)` and `CapsuleUnavailable(ChocolateCapsuleInteraction)` events.
- Produces: `Configure(ARTrackedImageManager manager, GameObject capsulePrefab)` and `ClearRuntimeInstances()`.

- [x] **Step 1: Write failing lifecycle tests**

```csharp
[Test]
public void ImageSpawnerCreatesOneCapsulePerTrackableAndReportsAvailability()
{
    var first = harness.Report(imageId, anchor, TrackingState.Tracking);
    var second = harness.Report(imageId, anchor, TrackingState.Tracking);
    Assert.That(second, Is.SameAs(first));
    Assert.That(harness.AvailableCount, Is.EqualTo(1));
}

[Test]
public void ImageSpawnerHidesAndUnregistersCapsuleWhenTrackingIsNone()
{
    var capsule = harness.Report(imageId, anchor, TrackingState.Tracking);
    harness.Report(imageId, anchor, TrackingState.None);
    Assert.That(capsule.activeSelf, Is.False);
    Assert.That(harness.UnavailableCount, Is.EqualTo(1));
}
```

- [x] **Step 2: Run the targeted tests**

Expected: failure because `ImageTrackingCapsuleSpawner` and its lifecycle API do not exist.

- [x] **Step 3: Implement ImageTrackingCapsuleSpawner**

Subscribe to `ARTrackedImageManager.trackablesChanged`; key instances by `TrackableId`; call the shared factory for added/updated tracked images; emit availability only on state transitions; hide on `TrackingState.None`; unregister and destroy on removal.

- [x] **Step 4: Create and configure the image library and scene components**

Create `MoodiumImageReferenceLibrary`, add `TestImageTracking.png`, enable specified size, assign `(0.10f, 0.10f)`, add `ARTrackedImageManager` to XR Origin, and wire the new spawner to the existing Chocolate Capsule prefab.

- [x] **Step 5: Verify asset and scene wiring**

Assert through `AssetDatabase`/serialized properties that the library has one named 10 cm image and the scene manager references that library.

### Task 3: Multi-target palm collision

**Files:**
- Modify: `Assets/Scripts/Interaction/PalmPressGestureController.cs`
- Test: `Assets/Editor/MultiTargetPalmPressTests.cs`

**Interfaces:**
- Produces: `RegisterTarget(ChocolateCapsuleInteraction target)` and `UnregisterTarget(ChocolateCapsuleInteraction target)`.
- Consumes: live Capsules reported by both spawners.

- [x] **Step 1: Write failing multi-target tests**

```csharp
[Test]
public void OverlapReturnsTheSpecificRegisteredCapsule()
{
    controller.RegisterTarget(capsuleA);
    controller.RegisterTarget(capsuleB);
    Assert.That(InvokeOverlapAt(capsuleB.transform.position), Is.SameAs(capsuleB));
}

[Test]
public void UnregisteringOneCapsuleLeavesTheOtherInteractive()
{
    controller.RegisterTarget(capsuleA);
    controller.RegisterTarget(capsuleB);
    controller.UnregisterTarget(capsuleA);
    Assert.That(InvokeOverlapAt(capsuleB.transform.position), Is.SameAs(capsuleB));
}
```

- [x] **Step 2: Run the targeted tests**

Expected: failure because the controller stores only one `m_Target`.

- [x] **Step 3: Implement target registry and per-hand contact ownership**

Use a `HashSet<ChocolateCapsuleInteraction>` for valid targets. Resolve overlap candidates to their parent Capsule, ignore unregistered Capsules, and track the current Capsule independently for each hand. End a Capsule contact only after neither hand touches it.

- [x] **Step 4: Run palm state-machine and Capsule feedback tests**

Expected: existing one-shot/rearm behavior and new multi-target behavior pass.

### Task 4: Reality session coordination and shared progress

**Files:**
- Modify: `Assets/Scripts/Moodium/Reality/RealityEnhancementFlowController.cs`
- Modify: `Assets/Scripts/Moodium/MoodiumAppFlowController.cs`
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Modify: `Assets/Scripts/TissueObjectTrackingSpawner.cs`
- Test: `Assets/Editor/DualTrackingRealityFlowTests.cs`

**Interfaces:**
- Reality flow consumes availability/unavailability events from both spawners.
- CandyWorldManager accepts a follower anchor containing either `ARTrackedObject` or `ARTrackedImage`.

- [x] **Step 1: Write failing integration tests**

```csharp
[Test]
public void ObjectAndImageCapsulesCanBeRegisteredAtTheSameTime()
{
    flow.RegisterAvailableCapsule(objectCapsule);
    flow.RegisterAvailableCapsule(imageCapsule);
    Assert.That(flow.InteractiveCapsuleCount, Is.EqualTo(2));
}

[Test]
public void ImageTrackedCapsuleIncreasesSharedCandyEnergy()
{
    imageCapsule.PalmContactStarted();
    Assert.That(candyWorld.Energy, Is.GreaterThan(0f));
}
```

- [x] **Step 2: Run the targeted tests**

Expected: failure because Reality flow has a single target and CandyWorldManager accepts only `ARTrackedObject`.

- [x] **Step 3: Update Reality flow and app mode switching**

Subscribe to both spawners for the session, independently animate/register every Capsule, unregister unavailable instances, enable both AR managers in Reality mode, and clear both sources on exit.

- [x] **Step 4: Generalize tracked-source validation**

Resolve the follower anchor and accept either `anchor.GetComponentInParent<ARTrackedObject>()` or `anchor.GetComponentInParent<ARTrackedImage>()` before registering energy and burst feedback.

- [x] **Step 5: Run the complete EditMode suite**

Expected: zero compilation errors, all tests pass, `Meshing 1` remains saved and clean.

- [ ] **Step 6: Perform Vision Pro acceptance test**

Show the 10 cm test image and the tracked tissue simultaneously; confirm two Capsules appear, follow independently, trigger independently, and increase one progress bar without duplicate instances after temporary tracking loss.
