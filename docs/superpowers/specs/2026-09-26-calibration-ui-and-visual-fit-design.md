# Calibration UI and Visual-Fit Phase 2 Design

## Intent

Improve the Vision Pro sticker-to-world flow so that people can clearly read
the bilingual prompt and enter-world affordance, visibly calibrate the physical
sensory bag, and see the selected Soft prefab fit the calibrated bag bounds.
The existing ARFoundation image tracking and PolySpatial spatial-pointer input
remain the only tracking and input pipelines.

## Confirmed scope

1. Correct non-uniformly scaled runtime text in the sticker-selection prompt
   and entry buttons.
2. Rework the calibration-frame presentation and action controls while keeping
   the existing 2D anchor-plane editing behavior.
3. Offset world-entry prefabs above the sticker and farther from the viewer.
4. Implement Phase 2: apply confirmed calibration data to a visual root and a
   separate interaction-volume root that both follow the existing image anchor.

Out of scope: changing ARKit/ARFoundation sessions, replacing image tracking,
hand/impact/press recognition, CandyWorldManager feedback, or world-specific
VFX/audio behavior.

## Existing architecture retained

`StickerWorldSelectionController` listens to the scene's existing
`ARTrackedImageManager` during the sticker-selection video loop and creates an
entry prefab per supported recognised image. Its UUID-suffixed visionOS image
names are normalised through `MoodiumImageReferenceName`.

After Enter World, `SensoryBagCalibrationCoordinator` and
`SensoryBagCalibrator` own the same image anchor. The calibrator already stores
width, height, depth, centre offset, plane offset and local orientation in
`SensoryBagCalibrationData`. `ImageTrackingCapsuleSpawner` defers to this
coordinator instead of creating a legacy tracked Soft object while calibration
is active.

## Selection UI design

### Text scale correction

The video display is non-uniformly scaled. Any world-space TextMeshPro child of
that display must compensate each parent scale axis, following
`OpeningVideoSubtitlePresenter.SafeScaleDivide`. This prevents Chinese and
English glyphs from being vertically squashed.

Each Enter World control gets a logical unscaled root with its interaction
collider, a separately scaled visual background, and a sibling text object.
The background can remain a flattened button while text remains uniform.

### Entry pose

The default entry pose is exposed in `StickerWorldSelectionController` and
starts at an image-anchor local offset of approximately `(0, 0.12, 0.10)` m:
12 cm above the sticker and 10 cm on the anchor normal away from the viewer.
The authored inspector values remain adjustable after device validation. The
exact local normal sign is documented and verified in device testing; if ARKit
reports the opposite image-normal direction, only that inspector default is
reversed, not the tracking system.

## Calibration UI design

The calibration frame remains a rectangle constrained to the image anchor
plane. It is deliberately not converted into a free 3D box: the calibrated
data represents the two-dimensional bag face.

Visuals:

- bright cool-white continuous rectangle border;
- four oversized corner handles and four edge handles, presented as high-
  contrast pill/capsule controls;
- generous colliders so gaze + pinch has a larger target than the visible
  handle;
- two independently readable buttons below the frame: `确认 / Confirm` and
  `重置 / Reset`;
- label transforms independent of flattened button/background transforms;
- no opaque panel covering the real bag.

The interaction implementation is unchanged: a frame-body drag moves its
centre, edges keep their opposite edge fixed, corners keep their opposite
corner fixed, and all spatial-pointer positions are projected into anchor-local
plane coordinates. Size clamps remain enforced.

## Phase 2 calibrated runtime hierarchy

Upon confirmation, a single new calibrated presentation is created per
selected image anchor:

```text
CalibratedBagRoot          follows existing ARTrackedImage transform
├── VisualRoot             position/orientation from centre offset + plane pose
│   └── CandySoft/NatureSoft, fitted to calibrated width and height
└── InteractionRoot        same calibrated centre and plane pose
    └── SensoryBagInteractionVolume (BoxCollider / dimensions)
```

`VisualRoot` and `InteractionRoot` are intentionally separate. The visual fit
uses the prefab's local renderer bounds as its native width/height reference,
then computes the required x/y fit scale. The interaction volume always takes
the exact calibrated width, height and configured depth. Therefore later
Impact/Press code will use geometry rather than visual mesh bounds.

The first Phase 2 visual fitting uses non-uniform scale when necessary. It is
contained under `VisualRoot`, clearly labelled as a temporary fitting strategy,
and does not change source prefab assets. A later deformation or authored
variant can replace only this fitting strategy without changing calibration or
interaction geometry.

## Lifecycle and tracking loss

The calibrated root keeps the existing anchor as its parent/follower. Anchor
pose movement updates the runtime presentation while calibration data stays in
anchor-local coordinates. Brief `TrackingState.None` events hide the visual
presentation but do not discard calibration data. Removal of the AR trackable
destroys only the matching runtime root.

## Validation

Editor validation:

- automated tests for text scale compensation, entry pose, visual-fit bounds,
  interaction-volume dimensions, and removal lifecycle;
- existing calibration rectangle tests continue to verify plane-only movement,
  resize anchors and clamps.

Vision Pro validation:

- present either supported sticker during the selection loop;
- verify prompt and Enter World text maintain normal glyph proportions;
- verify entry prefab is above/behind the sticker rather than intersecting it;
- drag each edge/corner and the frame body; find Confirm and Reset without
  searching;
- after Confirm, confirm Soft visual bounds approximately cover the physical
  bag and continue following a moving sticker;
- confirm a short tracking loss does not reset the saved dimensions.

## Known limitations

- This phase does not make visual deformation preserve the Soft mesh's original
  proportions; it uses a contained fit scale pending authored bag-specific
  meshes/deformation.
- No Impact, Press, multi-joint sensing or world feedback is added.
- Entry normal direction needs one Vision Pro check because the effective image
  anchor normal depends on the current visionOS ARFoundation pose convention.
