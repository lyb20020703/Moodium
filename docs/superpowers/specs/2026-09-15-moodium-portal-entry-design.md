# Moodium Portal Entry Design

**Date:** 2026-09-15  
**Status:** Approved in conversation; pending document review

## Goal

Replace the current Creative Space / Reality Enhancement mode selection screen with a cinematic portal entrance. After the existing opening animation, the portal opens automatically. The user touches a 3D object inside the portal to continue to the existing Reality Enhancement world-selection carousel.

Creative Space is removed from the runtime flow but its scripts and assets remain in the project for possible rollback or later cleanup.

## User Experience

The runtime flow becomes:

1. Existing candy opening animation.
2. A Moodium portal appears in front of the user and opens automatically.
3. A placeholder 3D object is revealed inside the portal.
4. After the opening animation finishes, the object begins a subtle idle animation and the prompt “触碰，进入 Moodium 的世界” appears.
5. The user touches the object with either hand.
6. The object responds with a scale pulse, particles, and an entry sound while the portal expands or fades away.
7. The existing Reality Enhancement Candy/Nature world-selection interface appears.
8. Selecting Candy World continues into the existing Object Tracking and Image Tracking experience. Nature World remains display-only and unavailable, consistent with the current behavior.

The portal entrance is a one-shot interaction. Multiple joints or both hands touching during the transition must not trigger it repeatedly.

## Architecture

The portal is integrated into the existing `Meshing 1.unity` scene rather than loaded as a separate Unity scene. This preserves the current XR Origin, AR Session, PolySpatial state, camera, permissions, and Object/Image Tracking managers across the transition.

The implementation uses a dedicated `MoodiumPortalEntrance.prefab` and controller. It borrows the visual technique from `Assets/Samples/PolySpatial/Scenes/Portal.unity`, but owns copies of any materials and animations that Moodium needs to customize. The PolySpatial sample scene and its source assets are not modified.

The portal controller is responsible only for the entrance lifecycle:

- placement relative to the user;
- portal opening and exit animation;
- enabling the entry object after opening;
- direct XR Hands contact detection;
- one-shot transition notification;
- entry prompt and optional visual/audio feedback.

`MoodiumAppFlowController` remains responsible for application flow and Reality Enhancement world selection. It receives the portal-completed callback and opens the existing Reality world carousel.

## Portal Prefab Structure

The proposed prefab hierarchy is:

```text
MoodiumPortalEntrance
├── PortalVisualRoot
│   ├── PortalPlaneTop
│   ├── PortalPlaneBottom
│   ├── PortalPlaneLeft
│   └── PortalPlaneRight
├── PortalContentRoot
│   ├── Environment
│   └── EntryModelRoot
├── EntryPrompt
├── EntryParticles
└── EntryAudio
```

`EntryModelRoot` is backed by a serialized `GameObject` prefab reference. Initially, it uses a sphere from the Portal sample as a placeholder. A future Moodium model can replace it through the Inspector without changing interaction code.

The contact volume is derived from the instantiated model’s combined renderer bounds by default. A serialized manual bounds override is available for models whose visible shape requires authored contact dimensions.

## Placement and Scale

The sample Portal is authored at a room-scale size and must not be copied with its original dimensions. Moodium’s portal is placed approximately 1.2–1.5 metres in front of the user, vertically aligned with the user’s view and kept upright by projecting camera forward onto the horizontal plane.

Placement occurs after head tracking has settled, following the pattern already used by `OpeningManager`. Dimensions, distance, vertical offset, and animation duration remain serialized tuning values.

The target opening duration is 1.5–2 seconds. The exact portal dimensions and entry-model scale are tuned in-device for a comfortable reach and clear visibility.

## Interaction State Machine

The entrance uses explicit states:

- `Hidden`: portal is inactive and cannot receive input.
- `Opening`: portal is visible and animating; contact is disabled.
- `Ready`: animation has completed; entry object contact is enabled.
- `Entering`: the first valid touch is accepted; further touches are ignored and exit feedback plays.
- `Completed`: the portal is hidden and the Reality Enhancement world selector owns the flow.

State transitions are one-way for a single entrance presentation. Re-entering the portal later, if desired, requires an explicit reset method rather than implicit object reactivation.

## Hand Contact Detection

The portal does not depend on a rendered hand mesh, a palm controller, or Unity collision callbacks from a hand GameObject. It reads tracked XR Hands joints directly and tests their world-space positions against the entry model’s contact bounds.

The joint set should include reliable contact points available on visionOS, such as fingertips, wrist, and metacarpals. It must not rely solely on `XRHandJointID.Palm`, because the current project has already established that the visionOS provider does not supply that joint reliably.

Contact is evaluated only in the `Ready` state. Loss of hand tracking leaves the portal visible and ready; interaction resumes automatically when tracking returns. A short per-frame or spatial hysteresis margin may be used to prevent missed contacts at the visual surface.

## Transition Feedback

On first valid contact:

- interaction locks immediately;
- the entry object performs a short scale pulse;
- particles burst from the contact or model centre;
- an entry sound plays without disrupting unrelated audio channels;
- the portal expands outward or fades over approximately 0.8 seconds;
- the portal root is hidden;
- the application flow displays the Reality Enhancement world selector;
- the carousel previews the currently selected world’s BGM using its existing natural transition behavior.

Missing optional particles, audio, or prompt references must not block the transition. Missing mandatory references, such as the portal visual root or application-flow callback, should produce a clear Unity error.

## Application Flow Changes

`MoodiumAppFlowController` no longer builds or shows the Creative Space / Reality Enhancement mode selection panel. After `OpeningManager` finishes, it presents the portal entrance instead.

After the portal completes, the controller opens the existing Reality Enhancement world-selection panel. Reality mode’s Back button returns to that world selector rather than the removed mode selector.

The runtime flow no longer initializes or presents:

- the Creative Space world-selection panel;
- the Creative Space object library;
- Creative Space edit and interaction panels;
- Creative Space spatial-physics spawning controls.

The implementation retains the existing Creative Space classes, prefabs, serialized scene references, `MoodiumMode.NoObject`, and related assets. `MeshSwiftUIDriver` remains disabled during the new flow. This limits risk and makes rollback possible without carrying unused Creative Space behavior into runtime initialization.

## Expected File Changes

- Modify `Assets/Scripts/Moodium/MoodiumAppFlowController.cs` to replace mode selection with portal entry and route Back to Reality world selection.
- Modify `Assets/Scripts/Moodium/Opening/OpeningManager.cs` to hand off to the portal entrance.
- Add `Assets/Scripts/Moodium/Opening/MoodiumPortalEntranceController.cs` and its meta file.
- Add `Assets/prefab/MoodiumOpening/MoodiumPortalEntrance.prefab` and its meta file.
- Add Moodium-owned copies of the required portal material and animation under an appropriate Moodium asset folder.
- Modify `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity` to connect the entrance prefab and flow references.
- Add focused Edit Mode tests for the entrance state machine and flow routing.

The source sample `Assets/Samples/PolySpatial/Scenes/Portal.unity` and its assets remain unchanged.

## Verification

The change is accepted when all of the following are true:

1. The existing opening animation still completes normally.
2. The old mode-selection page never appears during the new runtime path.
3. The portal automatically opens in a comfortable position in front of the user.
4. Touches during `Opening` do not trigger entry.
5. Either hand can trigger entry after the portal reaches `Ready`.
6. A multi-joint or two-hand touch produces exactly one transition.
7. Temporary hand-tracking loss does not break or dismiss the entrance.
8. The existing Reality Enhancement Candy/Nature carousel appears after entry.
9. Reality mode’s Back button returns to the Reality world selector.
10. Candy World still starts both Object Tracking and Image Tracking.
11. Nature World remains visible but cannot be entered.
12. Creative Space UI and systems are not initialized or enabled in the runtime flow.
13. The original PolySpatial Portal sample files remain unchanged.
14. Unity compiles without errors and the new Edit Mode tests pass.

## Deferred Work

- Creating the final Moodium entrance model.
- Permanently deleting Creative Space scripts and assets.
- Supporting multiple portal destinations.
- Adding a body-position or walk-through portal trigger.
