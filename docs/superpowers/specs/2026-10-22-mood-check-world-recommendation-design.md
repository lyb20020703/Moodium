# Mood Check and World Recommendation Design

## Goal

Add a lightweight, optional mood check to Moodium before and after a world visit. Before entry, the user moves Moodi on a two-dimensional valence/arousal plate or selects one of five quick moods. Moodium recommends Candy or Nature without locking the user's choice. After a visit, the user may check in again and see a neutral comparison of the two samples.

The feature supports the healing-oriented product premise while preserving the existing tutorial, sticker tracking, calibration, world energy, feedback, and XR input systems. Every confirmed sample is stored locally for later user testing. No data is uploaded.

## Product Rationale

The plate uses the valence × arousal model associated with Russell's circumplex model and Mood Meter-style interfaces. The recommendation aims to move the user toward a calmer, more pleasant state:

- High negative arousal (`Tense`) is routed toward Nature to lower activation.
- Low negative arousal (`Low`) is routed toward Candy for gentle activation.
- Positive high arousal (`Happy`) is routed toward Candy.
- Positive low arousal (`Calm`) is routed toward Nature.
- `Neutral` defaults to Nature while explicitly stating that either world is valid.

Candy is not recommended for `Tense` because its striking and cracking interactions could reinforce high-arousal anger instead of lowering it. This follows the finding in Bushman (2002) that venting through aggressive action can increase rather than reduce anger.

Recommendations remain suggestions. Both available sticker entries stay usable at all times.

## Scope

This implementation includes:

1. A pre-check during the first-use tutorial.
2. A reusable mood-check entry and one-time automatic prompt during sticker selection.
3. The spatial mood plate, quick mood chips, confirmation, recommendation, and skip flows.
4. A recommendation card and a recommendation tag on matching sticker entries.
5. A post-check and comparison after leaving a world when a pre-check exists.
6. Local JSON Lines logging, visit duration, and Candy capsule pinch counting.
7. An optional, default-enabled mood overlay for `Low` and `Tense` world visits.
8. Chinese and English copy selected from one centralized table.
9. Imported UI assets and a prebuilt world-space UI prefab matching the supplied design references.

Out of scope:

- Network upload or analytics transport.
- New AR, ARKit, input, or hand-collision systems.
- Changes to image tracking, calibration mathematics, Candy energy thresholds, or existing feedback chains.
- Mood overlays for `Happy`, `Calm`, or `Neutral`.
- A diagnostic or therapeutic interpretation of mood data.

## Existing Systems Retained

- `OpeningManager` keeps ownership of the current tutorial `VideoPlayer`, display, and timeline.
- `StickerWorldSelectionController` remains the owner of image-tracked world-entry presentation.
- `MoodiumSpatialInteractionController` remains the sole spatial-pointer router for Editor mouse, gaze + pinch, and direct touch.
- `SensoryBagCalibrationCoordinator` and `SensoryBagCalibrator` retain their current calibration data and flow.
- `MoodiumAppFlowController` remains the top-level owner of tutorial, sticker selection, calibration, world entry, and world exit.
- `CandyWorldManager` retains its energy controller and all existing Candy/Nature feedback.
- The existing `Moodium_Glass_UI`, `Moodium_Glass_UI_Edge`, TextMeshPro fonts, `MoodiumSpatialButton`, and `VisionOSHoverEffect` are reused.

## Architecture

New runtime code lives in `Assets/Scripts/Moodium/MoodCheck/` under the `Moodium.MoodCheck` namespace.

### `MoodClassifier`

A static, deterministic policy class. It accepts normalized `valence` and `arousal`, clamps each to `[-1, 1]`, and returns:

- `intensity = clamp01(sqrt(valence² + arousal²))`
- mood classification
- recommended world ID
- quick-chip preset positions
- comparison result relative to a configurable target point
- copy keys for mood, recommendation, and comparison text

Classification:

| Rule | Mood | Recommendation |
|---|---|---|
| `r < 0.22` | Neutral | Nature |
| `v >= 0 && a >= 0` | Happy | Candy |
| `v >= 0 && a < 0` | Calm | Nature |
| `v < 0 && a < 0` | Low | Candy |
| `v < 0 && a >= 0` | Tense | Nature |

Availability fallback occurs after the preferred world is selected. If it is unavailable and the other supported world is available, the other world is returned. If neither world is available, the recommendation is absent but the mood sample remains valid and can still be logged.

Comparison uses a configurable default target `(0.5, -0.1)`. A decrease in distance of at least `0.15` is `Better`; an increase of at least `0.15` is `Worse`; otherwise it is `Same`.

### `MoodCheckSample`

A serializable value object containing:

- `valence`, `arousal`, `mood`, `intensity`, and `inputMethod`
- `recommendedWorldId` and optional `enteredWorldId`
- `phase`, `sessionId`, `visitId`, `timestampUtc`, and `language`
- post-only `durationInWorldSeconds` and `pinchCount`

Enums serialize to the required lowercase strings through an explicit log DTO rather than relying on Unity enum serialization behavior.

### `MoodCheckContext`

A session-scoped component owned by app flow. It generates one application-session GUID and manages one active visit at a time:

- optional confirmed pre-sample
- entered world and generated visit ID
- realtime visit start
- capsule pinch count
- whether automatic sticker-selection prompting has already occurred

Entering a world binds the chosen world to the pre-sample and starts visit measurement. Completing or skipping the post-check closes the visit. Starting a later visit creates a new visit ID without changing the application session ID.

### `MoodCheckLog`

Appends one compact JSON object per line to `Application.persistentDataPath/moodium_mood_log.json`. Each successful or failed write also produces one structured line beginning with `[Mood Check]`. File errors produce `Debug.LogWarning` and never block or alter the UI flow.

No record is written for a skipped check. The pre-sample is written when confirmed with its recommendation; `enteredWorldId` is updated in the in-memory context and included in the post record. Because JSON Lines are append-only, the original pre line is not rewritten.

### `MoodCheckPlate` and `MoodCheckPlateHandle`

`MoodCheckPlate` owns position normalization, mood visuals, token motion, and drag feedback. `MoodCheckPlateHandle` is attached to the plate root, with both plate and token colliders below it, so `GetComponentInParent<MoodCheckPlateHandle>()` resolves either target.

For each pointer update:

1. Convert `pointer.interactionPosition` with `plate.InverseTransformPoint`.
2. Drop the local Z coordinate.
3. Divide by the effective plate radius to produce valence/arousal.
4. Clamp outside points to the unit circle.
5. Update the Moodi token directly without positional smoothing.

The handle captures one pointer ID from `Began` until `Ended` or cancellation. Other pointers cannot steal the drag. Releasing enters confirmation. Choosing “adjust” returns to dragging at the same coordinate. Quick chips set their prescribed coordinates and go directly to confirmation.

### `MoodCheckController`

The UI state machine is:

```text
Hidden
  -> Dragging
  -> Confirming
  -> Recommending -> Done        (pre-check)
  -> Comparing -> Done           (post-check)

Dragging/Confirming/Recommending/Comparing
  -> Hidden + Skipped
```

It exposes completion and skip events, plus explicit methods to begin a pre- or post-check. It receives fonts, language, available worlds, initial coordinates, the optional pre-sample, and visit metrics through configuration rather than finding app-flow objects globally.

The controller owns the visible panel instance but not application navigation. App flow decides what happens after `Completed` or `Skipped`.

### `WorldMoodOverlayController`

This component receives an optional pre-sample when the shared Candy/Nature world manager starts an experience. For `Low` or `Tense`, and only when the feature flag is enabled, it creates a PolySpatial-compatible light rain particle layer around the user. Initial emission intensity is proportional to sample intensity. `CandyWorldManager.OnEnergyChanged` forwards normalized progress, and the overlay scales toward zero using `1 - normalizedProgress`.

At completion it stops emission and plays one brief clearing/light effect, preferring an existing lightweight completion effect. It does not alter energy, thresholds, capsule behavior, or reward release. Other moods create no overlay.

## User Flows

### First-Use Tutorial

After `OpeningVideoHardwareChoice` has a settled answer and before the selected Yes/No branch starts:

1. Keep the existing player looping `timeline.QuestionLoop` with the existing seek coroutine.
2. Show the entry mood-check panel at the world-space placement described below.
3. Wait for completion, skip, or the tutorial's existing `skipRequested()`.
4. On completion, show the recommendation card.
5. Continue into the original branch video. The recommendation collapses to the compact footer/card form and remains visible beneath the video while the branch explains sticker choice.
6. At branch completion, hand the session to sticker selection as before.

If `skipRequested()` becomes true, the panel and compact recommendation are dismissed immediately and the existing tutorial skip path continues without waiting for animation completion.

### Sticker Selection

When sticker selection begins:

- A secondary mood button appears below the existing instruction.
- Before a sample exists it shows `prompt.entry`.
- After a sample exists it shows the localized equivalent of “Now: Tense · Nature recommended.”
- Selecting it opens a fresh pre-check and replaces the current session's pre-sample only after confirmation.
- If `ActiveEntryCount` remains zero for 12 continuous seconds, no pre-sample exists, and auto-prompt has not yet occurred, the controller opens the check once. The one-shot flag persists across returns to sticker selection during the same application session.
- Any interval with an active entry resets the no-entry timer. If entries later disappear, a new continuous 12-second zero-entry interval may trigger the prompt, provided the session's one-shot flag is still clear.

The recommended entry gets a small breathing tag above its Enter button. Both recommended and non-recommended entries stay enabled.

### Entering a World

Clicking a sticker entry continues through existing calibration. After calibration confirmation and immediately before world start, app flow:

1. Generates a visit ID.
2. Binds the selected world to the current pre-sample.
3. Starts duration and pinch tracking.
4. Passes the current context into `CandyWorldManager.StartExperience`/the overlay.

The recommendation never bypasses calibration and never changes the world selected by the user.

### Leaving and Post-Check

When the world exit action is invoked:

1. App flow stops the world through the existing stop path.
2. If no pre-sample exists, it closes the visit and returns directly to sticker selection.
3. If a pre-sample exists, it opens a post-check initialized at the pre-sample coordinate.
4. Confirmation writes the post sample and shows the comparison card with before/after moods, duration, and pinch count.
5. Skip closes the visit without a post record and returns to sticker selection.
6. Continue from the comparison card closes the visit and returns to sticker selection.

The comparison copy is descriptive and non-judgmental for Better, Same, and Worse.

## Spatial Input Integration

`MoodiumSpatialInteractionController` gains one narrow routing branch, modeled after `SensoryBagCalibrationHandle`:

- `Began`: resolve `MoodCheckPlateHandle`, capture the pointer ID, and call `PointerStarted`.
- `Moved`: forward only to the captured handle for that pointer ID.
- `Ended`/cancel: forward final position, release capture, and enter confirmation.

The route uses the existing spatial pointer event stream and works for Editor mouse, gaze + pinch, and direct touch. It creates no new input actions or hand colliders.

## UI Construction and Assets

The supplied `png/`, `svg/`, and `design-reference/` directories are copied into `Assets/UI/Moodium/MoodCheck/`. PNG importer metadata is configured for Sprite (2D and UI), alpha transparency, sRGB, Clamp wrapping, mip maps, and original maximum dimensions.

An Editor setup script builds reusable rounded meshes and a `MoodCheckPanel` prefab. Runtime code never calls functions inside an Editor assembly. The generated UI uses world-space `SpriteRenderer`, `MeshRenderer`, TextMeshPro, colliders, `MoodiumSpatialButton`, and `VisionOSHoverEffect` consistent with existing project patterns.

The fallback layered tint specified in the source brief is the primary implementation:

- `disc_mask` provides the low-alpha mood-colored base.
- A clipped `glow_radial` sprite follows Moodi and provides the stronger center glow.
- Material instances or renderer colors are used instead of depending on unverified PolySpatial `MaterialPropertyBlock` behavior.

This avoids introducing an unverified Shader Graph into the critical path while preserving the specified appearance and PolySpatial compatibility.

### Dimensions and Placement

- Panel: `0.462 × 0.566 m`, corner radius `0.034 m`.
- Plate diameter: `0.28 m`; effective coordinate radius: `0.112 m`.
- Neutral ring: `0.123 m` diameter.
- Moodi token: `0.073 m`.
- Quick and action buttons: `0.036 m` visual height with at least `0.045 m` collider height.
- Recommendation card: `0.392 m` wide.
- Comparison card: `0.56 × 0.49 m`.

At appearance, the panel center is placed `0.75 m` in front of the camera and `0.12 m` below its gaze height. Camera forward is projected onto the horizontal plane, and only yaw is used. The panel stays at that world pose rather than following the user's head. It is parented under a scene-level root, never under the non-uniformly scaled video display. Any unavoidable scaled parent uses `StickerWorldSelectionController.GetCompensatedLocalScale`.

### Visual Hierarchy and Sorting

The hierarchy follows the supplied design:

```text
MoodCheckPanel
├─ PanelGlass
├─ Header
├─ Readout
├─ Plate
│  ├─ PlateWell
│  ├─ PlateTintBase
│  ├─ PlateGlow
│  ├─ Axes
│  ├─ NeutralZone
│  ├─ QuadrantIcons
│  ├─ Trail
│  ├─ PlateCollider
│  ├─ TokenShadow
│  └─ Token
├─ AxisLabels
├─ QuickChips
└─ Footer
```

Glass begins at sorting order 0, plate tint and axes occupy 11–15, shadow/token occupy 18–22, and text uses 30. Transparent layers receive small forward Z offsets as specified in the brief. The existing glass panel and edge materials provide a dark translucent surface that allows the real environment to remain visible.

### Visual Feedback and Motion

- Mood texture, readout text, active quadrant icon, and mood tint update during drag.
- Mood tint and icon opacity interpolate over `0.3–0.35 s`; material writes occur only when the mood changes or intensity changes by more than `0.02`.
- Active quadrant icon opacity is `0.95`; others are `0.26`.
- The trail is hidden below intensity `0.08`.
- Drag lift scales Moodi from 1 to 1.12 and moves it `0.008 m` toward the user over `0.18 s`; release reverses it.
- Mood texture changes include a `0.12 s` scale recovery from 0.94 to 1.
- Panel entry/exit uses `0.35 s`/`0.25 s` scale and alpha animation.
- Recommendation tags breathe between 1 and 1.05 on a two-second sine cycle.
- No flashing animation is used.

All Moodi sprites remain untinted. White icon sprites may be tinted by mood color and alpha. World-space text uses the configured Alibaba PuHuiTi and Inter TMP assets and receives non-uniform scale compensation.

## Copy and Localization

All user-visible strings live in a single key-based Chinese/English table under the MoodCheck namespace. The controller resolves language from the active `OpeningVideoSession.English` value when available, otherwise from `PlayerPrefs["Moodium.Language"]`.

No UI-building method contains duplicated language literals. Dynamic strings use localized format templates for mood name, recommendation, visit duration, and pinch count.

## Data and Privacy

Log path:

```text
Application.persistentDataPath/moodium_mood_log.json
```

Each line includes:

- `sessionId`, `visitId`, and `phase`
- `valence`, `arousal`, `mood`, `intensity`, and `inputMethod`
- `recommendedWorldId` and `enteredWorldId`
- `timestampUtc` and `language`
- post-only `durationInWorldSeconds` and `pinchCount`

The comparison card states that records stay on the device. Missing optional artwork or a failed file write generates a warning but does not block completion.

## Failure Handling

- Missing Moodi/plate artwork: log a warning and keep functional colliders, text, and buttons; the flow remains skippable.
- Missing sticker thumbnail: omit the thumbnail while retaining recommendation copy and CTA.
- No available recommended world: show the mood result without a world-specific CTA; sticker selection remains available.
- Missing fonts: use the current TMP fallback and log one warning.
- Destroyed player or tutorial skip: dismiss mood UI synchronously and return control to the existing skip path.
- Interrupted drag: release pointer capture and remain in a valid draggable state.
- Log I/O error: warn and complete the flow normally.
- Overlay resource failure: disable the overlay for that visit without affecting Candy world startup.

## Integration Changes

### `OpeningManager`

Insert the pre-check between settled hardware choice and `SeekToFrameAndPlay(player, branch.StartFrame)`. Preserve the question loop and skip semantics. Keep the compact recommendation visible through branch playback.

### `StickerWorldSelectionController`

Add the secondary mood entry, 12-second one-shot policy, current mood/recommendation label, and recommendation tag creation. Expose events rather than owning the mood-check controller.

### `MoodiumSpatialInteractionController`

Add pointer capture and routing for `MoodCheckPlateHandle` only.

### `MoodiumAppFlowController`

Own or configure `MoodCheckContext` and `MoodCheckController`; orchestrate sticker entry, pre-check replacement, visit start, post-check, comparison completion, and return to sticker selection. Existing uncommitted wrist-exit changes remain intact, with their exit callback becoming the post-check trigger.

### `CandyWorldManager`

Accept the current mood context, count accepted capsule pinch events for the active visit, initialize/stop the overlay, and forward normalized energy progress. Do not modify `CandyEnergyController` or existing feedback thresholds.

### Editor Setup

Add an idempotent setup tool that imports/configures sprites, creates rounded mesh assets, builds the MoodCheck prefab, and wires references without modifying unrelated scene or prefab data.

## Testing Strategy

### Pure EditMode Tests

- Classification at the neutral radius boundary and in all four quadrants.
- Intensity clamping for edge and out-of-range inputs.
- All quick-chip preset coordinates.
- Preferred recommendation and unavailable-world fallback.
- Better/Same/Worse thresholds around `0.15`.
- Localized copy lookup completeness.
- Log DTO field names, lowercase values, and omission/default behavior for post-only fields.

### Controller and Policy Tests

- Drag coordinates are transformed, normalized, and clamped correctly.
- Only the captured pointer controls a drag.
- Quick chip enters confirmation with the correct input method.
- Adjust preserves coordinates; confirm and skip fire exactly once.
- Sticker auto-prompt occurs once after 12 seconds only when no entry and no pre-sample exist.
- Recommendation tags attach only to matching entries and never disable other entries.
- Tutorial skip interrupts an active check without blocking.
- World exit bypasses post-check when no pre-sample exists.
- Post-check begins at the pre-sample coordinate and comparison uses captured visit metrics.
- Low/Tense overlays scale by intensity and `1 - progress`; other moods do nothing.

### Verification

1. Run the relevant EditMode test assembly through Unity batch mode.
2. Run existing opening, sticker selection, app flow, spatial interaction, Candy energy, and wrist exit tests.
3. Open the generated prefab/scene in Unity and inspect missing scripts, materials, sprites, colliders, scale, and sorting orders.
4. Exercise Editor mouse interaction through pre-check, recommendation, world selection, simulated exit, post-check, and comparison.
5. Perform a visionOS device pass for gaze + pinch, direct touch, glass transparency, sprite mip behavior, text proportions, and PolySpatial particle compatibility.

## Acceptance Criteria

- First-use users see an optional pre-check at the specified tutorial point; tutorial skip is never blocked.
- Returning/skipped-tutorial users can open the check from sticker selection, and the no-entry auto-prompt fires at most once per application session.
- Plate drag and quick selection work through existing Editor, gaze + pinch, and direct-touch input paths.
- Classification, recommendation, fallback, and comparison exactly match the specified rules.
- Recommendation never locks a world choice.
- Exiting a visit with a pre-sample offers an optional post-check and displays duration and pinch count after confirmation.
- Confirmed samples append locally in the required JSON Lines format; skipped checks produce no record.
- Low/Tense world visits receive a default-enabled, progress-fading visual overlay without changing energy behavior.
- The generated UI matches supplied dimensions, assets, typography, glass style, sorting, and non-flashing motion.
- Existing unrelated working-tree changes remain preserved, and no commit, reset, revert, clean, or stash is performed.
