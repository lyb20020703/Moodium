# Video Sticker World Selection Design

## Goal

Replace the fixed, card/carousel-based Reality Enhancement world picker with a video-backed, image-tracked world-entry state. After every tutorial branch, the opening video remains visible and loops the existing hardware-question visual segment while the user looks at a physical Moodium sticker. Each tracked Candy or Nature sticker creates its matching 3D world-entry prefab at that sticker's image anchor. Selecting an entry begins calibration for that world; leaving a world returns to this same sticker-entry state.

## Retained Systems

- Unity 6000.0.30f1, PolySpatial 2.4.3, visionOS XR 2.4.3, AR Foundation 6.0.3, XR Hands 1.5.0.
- `ARTrackedImageManager` remains the sole image tracking owner. No ARKit session or replacement tracking pipeline is created.
- The existing `OpeningManager` runtime `VideoPlayer`, RenderTexture and spatial video display continue to render the combined opening clip.
- Existing `MoodiumSpatialInteractionController` remains responsible for Editor mouse, gaze + pinch and direct spatial-pointer input.
- Existing `SensoryBagCalibrationCoordinator` and `SensoryBagCalibrator` retain their anchor-local data model and visual calibration controls.
- Existing `MoodiumAppFlowController` continues to own entering and stopping Candy/Nature experiences and existing feedback chains.

## State Model

```text
Opening tutorial branch
  -> StickerWorldSelection
  -> WorldCalibration
  -> WorldInteraction
  -> StickerWorldSelection
```

### StickerWorldSelection

The combined video is kept alive on its existing display and loops `00:41:10` through `00:47:00`. The displayed copy is language-specific:

- Chinese: `请对准贴纸`
- English: `Look at the sticker`

`ARTrackedImageManager` is enabled. For every currently tracked reference image:

- `CandyWorldPhoneTracking` creates one `CandyWorldUI` instance.
- `NatureWorldPhoneTracking` creates one `NatureWorldUI` instance.

Each instance follows its own `ARTrackedImage` anchor and has an independent, spatially clickable entry affordance above it:

- Chinese: `进入世界`
- English: `Enter`

Multiple image anchors may be visible concurrently. They produce multiple independent entry instances. Temporary tracking loss hides an instance; recovery restores/reuses it. A removed anchor destroys only its own entry instance.

`CandySoft`, `NatureSoft`, calibration frames, and all world feedback remain absent in this state.

### WorldCalibration

Selecting an entry freezes selection semantics:

- hide the video display and instruction text;
- hide/destroy all world-entry instances;
- retain the selected `ARTrackedImage` anchor and reference-image identity;
- enable one `SensoryBagCalibrator` for the selected Candy or Nature world.

The calibration frame uses the selected anchor as its plane, preserves its current anchor-local dimensions and offset, and follows the existing Phase 1 confirmation/reset behavior. Other image anchors must not create calibration frames during this state.

### WorldInteraction

Confirming calibration passes the selected world and its `SensoryBagCalibrationData` to a narrow app-flow entry method. This begins the existing Candy/Nature world experience only after calibration has succeeded.

The currently shipped Soft-prefab interaction remains visually and behaviorally unchanged by this feature. Phase 2 will later use the saved calibration data to size the virtual bag and interaction volume. Until then the data is retained and exposed, but does not falsely alter visual fitting or press/impact geometry.

## Exit Behavior

Exiting Candy or Nature must:

1. stop current world-specific systems through the existing stop path, including Nature runtime trees, VFX, BGM and tracked Soft instances;
2. clear any active calibration UI/session for that entry attempt;
3. reactivate `StickerWorldSelection` rather than `ShowObjectWorldSelection()`;
4. restore the looping video, language-appropriate instruction, image tracking and entry prefabs.

The old card/carousel picker remains in project files but is no longer reached from opening completion or World back actions.

## Video Ownership and Timeline

`OpeningManager` must not destroy the runtime VideoPlayer, display, material or RenderTexture immediately after the tutorial branch. It hands an explicit lightweight video-session reference to the sticker selection controller.

The controller owns the selection loop:

```text
start = OpeningVideoTimeline.Chinese.QuestionLoop.StartFrame  // 00:41:10
end   = OpeningVideoTimeline.Chinese.QuestionLoop.EndFrame    // 00:47:00
```

These frames are intentionally shared by both languages as the requested visual waiting segment. The controller seeks back to `start` on end-of-loop or an unreliable transient `isPlaying` state, using the existing visionOS-safe seek/play pattern. It must not call a tutorial failure path or bypass into the World picker.

## Entry Prefab Placement

Each entry root is created under the tracked image transform (or uses the existing pose follower) and uses a configurable anchor-local position, orientation and scale. The UI prefab itself remains untouched. The entry button is generated as a child of the entry root with configurable local height and forward offset, so it moves with the sticker and remains separately clickable.

The selected entry's current anchor remains the calibration anchor. The system must not infer that a sticker lies at the bag center.

## Component Boundaries

### `StickerWorldSelectionController` (new)

Owns the selection-state lifecycle, the tracked-image listener, runtime entry dictionary, looped-video session, localized instruction, and entry button activation. It exposes:

```csharp
public void Begin(OpeningVideoSession session, bool english);
public void End();
public event Action<StickerWorldEntry> WorldEntrySelected;
```

### `StickerWorldEntry` (new)

Small data object/component containing `TrackableId`, reference image name, `MoodiumWorldDefinition`, image-anchor Transform and instantiated entry root. It exposes the world choice without knowing calibration or feedback logic.

### `OpeningVideoSession` (new)

Owns only references to the existing runtime `VideoPlayer`, video display root, instruction root and disposal callback. It makes ownership explicit between `OpeningManager` and selection controller, preventing premature destruction or a second player.

### `SensoryBagCalibrationCoordinator` (modify)

Adds explicit active-world session control rather than automatically starting calibration for every recognized Candy/Nature anchor. It exposes a selected-anchor start method and a completion event carrying the entry context plus calibration data. When inactive or in sticker-selection mode it does not consume tracking updates.

### `MoodiumAppFlowController` (modify)

Adds a single owner for the new top-level states. It receives opening video completion, starts sticker selection, begins calibration after entry click, enters the existing world flow after Confirm, and routes World back actions to sticker selection.

### `OpeningManager` (modify)

Finishes tutorial branches by handing off the still-running video session to app flow instead of stopping/destroying it and immediately calling the old picker.

## Input and Interaction

Entry buttons use the existing spatial UI path used by other Moodium buttons. They require a Collider and `MoodiumSpatialButton`/existing pointer routing so the same entry works with Editor mouse, gaze + pinch and direct touch. They should not use a bespoke hand-collision-only input route.

## Tests

Editor tests must cover:

1. Candy and Nature anchors map to their respective entry prefabs/world definitions.
2. Multiple simultaneous anchors create independent entries; loss/removal affects only the affected entry.
3. A sticker selection suppresses calibration until its entry is clicked.
4. Clicking an entry starts calibration only for that entry's reference-image/anchor context.
5. Confirming calibration invokes app flow only once with the selected world and saved data.
6. World exit returns to sticker selection rather than the old carousel.
7. Selection-loop seek boundaries use exactly `00:41:10–00:47:00` / the corresponding 30 FPS frames.
8. Chinese/English instruction and entry labels use the chosen language.

## Constraints and Non-Goals

- Do not create another ARKit/AR Foundation tracking session.
- Do not change Candy/Nature feedback, gesture detection or runtime prefab art in this feature.
- Do not change Phase 1 calibration resize math, anchor-local data or calibration UI interaction.
- Do not make calibration data control Soft visual scale, bag volume or Impact/Press until Phase 2.
- Do not automatically commit, reset, revert, clean, stash, or overwrite unrelated working-tree changes.
