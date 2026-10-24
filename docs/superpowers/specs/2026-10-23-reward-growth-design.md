# Reward growth — design

## Intent

Replace the hidden, fixed-offset world rewards and manual wrist `Release` switch with a visible four-stage reward loop. Each accepted energy stage produces a glass reward orb over the sensory bag, stores that exact orb in a left-wrist bracelet, and automatically grows the associated physical reward at a safe, forward-facing room position. This makes the reward legible at the moment it is earned and preserves the user's sense of progression without a separate release action.

## Goals

- Deliver exactly one reward at 20, 50, 80, and 100 percent during each world session.
- Keep every physical reward in front of the user whenever viable, inside scanned spatial mesh, clear of the bag, squeeze-progress video, room obstacles, and other rewards.
- Turn `CandyEnergyWristUI` into a collection bracelet: four glass slots, collected mini-orbs, a next-slot progress ring, gaze-based visibility/fade, and no energy percentage or release switch.
- Preserve the existing energy thresholds, bag squeeze recognition, calibration, progress video, candy hand-open burst, Nature completion panorama/ivy/footstep effects, and Candy footstep effects.
- Provide isolated EditMode coverage for stage delivery, miniature sanitization, bracelet state, reward placement policy, and grow-in final pose.

## Non-goals

- No cross-session reward persistence, AR-session changes, new input system, or requirement that optional audio/particle resources exist.
- No change to energy math, calibration, image tracking, or squeeze-video behavior.
- No attempt to alter unrelated pre-existing workspace changes.

## Existing integration points

- `CandyEnergyController.StageReached` already emits the required 20/50/80/100 stages but currently has no subscriber.
- `CandyWorldManager` owns session start/stop, latest bag pinch centre, energy progression, world prefabs, spatial mesh, existing reward effects, and wrist UI creation.
- `SqueezeProgressVideoController` binds a top-facing video disc to the calibrated bag; its world bounds must become a placement exclusion.
- `NatureForestGrowthController` and `CandyCompletionHouseController` currently own fixed/backward placement and must no longer subscribe to progress for spawning.
- `CandyEnergyWristUI` already owns left-wrist tracking and smoothing; its pose source will be retained while its old ring/release presentation is replaced.

## Approaches considered

1. Complete reward pipeline — selected. A dedicated Rewards subsystem owns reward data, sequencing, placement, visuals, bracelet state, and grow-in. It is the only approach that delivers the intended collectible feedback loop and removes the hidden manual release dependency.
2. Two-phase delivery. This would defer gem inspection and reward highlighting, but would leave the experience partly redesigned and risk duplicate old/new reward affordances.
3. Minimal automatic release. This would merely move current houses/trees forward and auto-fire the release effect, but misses the supplied orb and bracelet design.

## Data and configuration

New namespace: `Moodium.Rewards`; new code lives in `Assets/Scripts/Moodium/Rewards/`.

`WorldRewardTable : ScriptableObject` contains four `RewardEntry` records:

- stage: 20, 50, 80, 100.
- one or more source prefabs, display name, size class, target height, optional miniature-scale override.

Two resources are created: Candy and Nature. `MoodiumAppFlowController` exposes serialized table fields and passes them through a new `CandyWorldManager.ConfigureRewardTables(...)` method. The initial mappings are Candy: 20 existing macaron/star/heart cluster, 50 cookie/chocolate cluster, 80 little completion house, 100 completion house; Nature: 20 three burst prefabs, 50 shortest forest prefab resized to 0.5 m, 80 small tree, 100 large tree. Exact prefab selections are serialized resources rather than hard-coded by name.

`RewardTextTable` centralizes `orb.firstHint`, `bracelet.full`, and `gem.label` in Chinese/English; it selects language from `PlayerPrefs["Moodium.Language"]`.

## Runtime architecture

### RewardSequenceController

Attached/configured by `CandyWorldManager`. It subscribes to `StageReached` only while a world session is running, clears its awarded-stage set at session start, and clears all spawned reward state at stop. On a newly reached stage it immediately captures camera head position/flat forward direction, latest pinch visual centre, bag/video bounds, and reserves a placement from the solver.

It starts two independent tracks so missing hand tracking cannot delay growth:

1. **Collection track:** `RewardOrb` appears 0.12 m above the last visual bag centre; scale-in with a 0.3 s overshoot, hold with light floating for about one second, then fly on a raised bezier arc to the bracelet's next slot over 0.6–0.8 s. If no wrist frame is available it waits up to 20 seconds; then it records as collected with bracelet hidden. The first completed collection triggers `orb.firstHint` once per session.
2. **World track:** at the planned deadline (no later than t0 + 2.5 s), show a world-colour ground cue and spatial audio, wait 0.4 s, then spawn `RewardGrowIn` at the reserved point. This track never waits for the wrist.

At four collections, `RewardBracelet` pulses world-colour light and each gem flashes in order; the sequence asks `CandyRainRewardController` to play once and clear after about eight seconds, and announces `bracelet.full`.

### RewardOrb

Creates one transparent 0.07 m glass sphere with a white edge/highlight, pink candy or green nature interior glow, and matching thin pedestal. A cloned miniature reward is fit inside a 0.045 m interior, slowly yaw-rotated at ~20 degrees/second, and the clone has colliders, rigidbodies, particle systems, animators, and non-renderer behaviours disabled. It can receive a soft touch bounce without changing the sequence.

The very same object is re-parented and reduced to 0.015 m at a bracelet slot; it is never replaced by a flat-colour gem.

### RewardBracelet

`CandyEnergyWristUI` retains its `TryGetWristFrame` and smoothing source but delegates visual collection state to this component. It renders a 0.012 m translucent band around the left wrist, four back-of-hand slots with shallow glass cups, empty white dashed circles, collected mini-orbs, and a white progress ring around the next slot. Centre slots use scale 1.0 and outer slots 0.9, with 0.017 m arc spacing.

No tracked wrist means fully hidden: there is no old floating-front fallback. When the wrist direction is within 30 degrees of camera forward, alpha is 1 and visual scale is 1.15; otherwise alpha is .35 and scale 1.0, both eased over .3 s. Gem interactions use existing `MoodiumSpatialInteractionController`/`MoodiumSpatialButton`; selecting a gem enlarges it 1.3×, presents `gem.label` for two seconds, and asks its world reward to flash for 1.5 seconds.

### RewardPlacementSolver

Pure policy class with injected raycast/overlap/visibility interfaces; Unity adapters bind spatial `MeshCollider` queries in runtime. It accepts snapshot inputs and returns a reserved position, yaw, scale reduction, and fallback reason.

- 20: table candidate beside the calibrated bag on a same-height upward-facing surface; bag and video bounds are expanded by 0.05 m. If unavailable, use 1.0–1.4 m floor candidates.
- 50/80: 1.2–1.8 m on opposite forward-side sectors (30–45 degrees each side).
- 100: 2.0–2.8 m in the forward 20-degree sector.
- Generate angle steps of five degrees and range steps of .1 m. Require scanned upward mesh ground (or the special small-item table surface), forward limit at most 50 degrees, ≥.6 m head-floor distance, free clearance capsule, reward separation, and visibility appropriate to size class.
- Score nearest preferred angle/distance and visible Large base. Relax in this order: widen angle by 10-degree steps to ±50, lower range to .8, swap 50/80 side, shrink to 80%, then accept the best candidate that only violates visibility and log a warning.
- Face the user with yaw only; reservations count as occupied before a reward grows.

### RewardGrowIn and world reward interaction

`RewardGrowIn` replaces the staged fixed-placement use of `NatureTreeGrowIn`: a world-colour 0.3 m cue and optional particles/audio precede a 1.3 s ease-out growth. It starts 0.6× object height below the surface at 30% final scale and finishes base-aligned at 1.0 scale with a 1.04 overshoot. All generated physical rewards get a generalized `CompletionHouseSoftInteraction` configuration: it discovers readable render bounds and sizes contact discovery from bounds instead of assuming a house-specific mesh hierarchy.

Old `NatureForestGrowthController` and `CandyCompletionHouseController` remain only as inert compatibility utilities or are stopped from receiving energy progress; neither emits a staged fixed-offset reward. The wrist Release control and `CandyWorldManager.ReleaseAccumulatedEnergy` path are removed from active session wiring.

## Lifecycle and resilience

- Every optional sound, particle prefab, text/audio presenter, visual material, or missing reward prefab logs a warning and permits the remaining sequence to proceed.
- `StopExperience` unsubscribes events, cancels sequences, destroys orb/bracelet runtime visuals and all physical rewards, and releases reservations.
- Existing 100% completion panorama, ivy burst, hand-open burst, and footstep effects remain invoked through their existing manager path.

## Testing and acceptance

EditMode tests in `Assets/Editor/` cover threshold de-duplication, miniature sanitization/bounds fitting, bracelet slot/progress/visibility state, the placement scenarios and fallback order from the request, and final grow-in ground alignment/scale. Existing tests remain green and Unity compiles without errors.

On device, validate the four-stage visual sequence seated at a table plus small-room, large-room, and standing conditions. Tune physical candidate ranges and world-scale/material presentation on Vision Pro; PolySpatial transparent-glass fallback is `Moodium_Glass_UI` / `Moodium_Glass_UI_Edge` if URP Lit Transparent is unsuitable.
