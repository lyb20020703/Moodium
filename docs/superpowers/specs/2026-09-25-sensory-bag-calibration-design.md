# Sensory Bag Calibration — Phase 1 Design

## Goal

Turn an existing tracked Candy or Nature reference image into a stable, manually adjustable two-dimensional calibration frame for the real sensory bag. Confirming the frame stores bag geometry in Image Anchor local space. This phase deliberately does not create the final Soft Prefab or perform hand-impact, press, or feedback behavior.

## Existing Systems Retained

- Unity 6000.0.30f1, PolySpatial 2.4.3, visionOS XR 2.4.3, AR Foundation 6.0.3, XR Hands 1.5.0.
- `ARTrackedImageManager` remains the only image-tracking source and continues to own `ARTrackedImage` poses.
- `ImageTrackingCapsuleSpawner` continues subscribing to `trackablesChanged`; a calibration-specific anchor event is added rather than creating another tracking session.
- `MoodiumSpatialInteractionController` remains the route for `EnhancedSpatialPointerSupport` input, so gaze+pinch, direct spatial touch, and Editor pointer routing use the existing project mechanism.
- Candy/Nature feedback, `CandyWorldManager`, legacy hand-collider scripts, and tracked-object behavior remain unchanged.

## Phase 1 Runtime Flow

```text
ARTrackedImageManager / ARTrackedImage
  -> ImageTrackingCapsuleSpawner calibration anchor event
  -> SensoryBagCalibrationCoordinator
  -> SensoryBagCalibrator + CalibrationRoot
  -> user adjusts projected 2D frame and confirms
  -> SensoryBagCalibrationData retained by coordinator
```

`CandyWorldPhoneTracking` and `NatureWorldPhoneTracking` share the same calibrator. The coordinator retains the world identity and the later Soft Prefab selection, but does not instantiate that prefab during this phase.

## Coordinate Model

All mutable values are Image Anchor local-space values:

- `width`, `height`: rectangle extent along the anchor local X/Y axes.
- `centerOffset`: rectangle center X/Y relative to Image Anchor origin.
- `depth`: Inspector-configured default only.
- The frame origin is continuously updated from the Image Anchor Transform. The frame position equals `anchor.TransformPoint(new Vector3(centerOffset.x, centerOffset.y, planeOffset))` and its orientation equals the anchor rotation.

The pointer’s world point is converted with `anchor.InverseTransformPoint`. Its Z coordinate is discarded. Thus moving, resizing, and handles can never translate the rectangle away from the tracked-image plane.

## Calibration Editing

The frame provides four corners, four edge handles, a body target, and Confirm/Reset buttons.

- Body drag moves `centerOffset` only.
- Left/right edge drags hold the opposite edge fixed and alter width.
- Top/bottom edge drags hold the opposite edge fixed and alter height.
- A corner holds its diagonal opposite corner fixed and alters both dimensions.
- Width/height are clamped to configurable min/max values, so axis flipping is impossible.
- Reset restores configured default width, height, and center offset without restarting or modifying tracking.

## Tracking-Loss Policy

On `TrackingState.None`, the coordinator hides the frame but retains the active in-memory calibration values and prior anchor-relative state. It does not reset or destroy them. On a subsequent tracked update with the same `TrackableId`, it restores the frame. Removal destroys only that runtime calibration session; a later phase may add persistence if needed.

## Data Contract

`SensoryBagCalibrationData` is a serializable value containing:

```csharp
string referenceImageName;
SensoryBagWorldKind worldKind;
float width;
float height;
float depth;
Vector2 centerOffset;
Vector3 planeLocalOffset;
Quaternion localOrientation;
```

The data intentionally stores no frozen world pose. Later runtime geometry uses the current Image Anchor pose plus these local values.

## New Components

- `SensoryBagCalibrationData`: persistent Phase 1 geometry contract.
- `SensoryBagCalibrator`: owns the local rectangle model, visual frame, bounds clamping, and Confirm/Reset events.
- `SensoryBagCalibrationHandle`: identifies a body/corner/edge handle and forwards pointer lifecycle calls to the calibrator.
- `SensoryBagCalibrationCoordinator`: receives existing image-anchor events, chooses Candy/Nature metadata, starts/resumes/ends calibrator sessions, and retains confirmed data.

## Input and Visuals

`MoodiumSpatialInteractionController` gets only a narrow routing addition for `SensoryBagCalibrationHandle`. It passes the existing `SpatialPointerState.interactionPosition` into `PointerStarted`, `PointerMoved`, and `PointerEnded`.

The first visual is generated at runtime: four bright border bars, eight enlarged BoxCollider-backed handles, and existing spatial buttons labelled Confirm and Reset. It is intentionally high-contrast and debug-oriented; no new art asset is required.

## Out of Scope

- Final `BagAnchorRoot`, visual fitting, and `SensoryBagInteractionVolume`.
- XR Hands multi-joint sampling, velocity, impact, press, and debug views.
- Candy/Nature feedback or progress wiring.
- Persistent calibration across app launches.

## Acceptance Criteria

1. A tracked image starts calibration rather than spawning `CandySoft` or `NatureSoft`.
2. Frame position and orientation follow the existing Image Anchor while width, height, and offset remain Anchor-local.
3. Body, four corners, and four edges edit exactly their intended axes and respect min/max bounds.
4. Confirm exposes correct data and hides calibration UI; Reset restores Inspector defaults.
5. Temporary tracking loss preserves unsaved frame dimensions/offset and does not reset the session.
6. Editor and Vision Pro use the existing spatial pointer route; project scripts compile without errors.
