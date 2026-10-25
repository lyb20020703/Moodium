# Squeeze Modes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking. User's package forbids commits, resets, stashes, reverts and cleans.

**Goal:** Add Free, Rhythm and Breath squeezing modes to both worlds, preserving shared progress, existing physical feedback, calibration and post-completion gameplay.

**Architecture:** Pure scoring/state classes consume a single aggregated contact stream. A session-owned mode controller adds energy through the existing controller, while tabletop views consume read-only mode state. The visual root follows the calibrated bag without inheriting its nonuniform visual scale. Existing progress-prop controllers retain ownership of their prefabs.

**Tech Stack:** Unity 6000.0.30f1, C#, AR Foundation/XR Hands, PolySpatial-native meshes and sprites, NUnit EditMode tests.

**Spec:** ../specs/2026-10-24-squeeze-modes-design.md and ../specs/squeeze-modes-layout.json. Source archive: SqueezeEnergySpec.zip supplied by user on 2026-10-25.

## Global Constraints

- Free mode remains +5 per accepted legacy pinch; RegisterPinch and AnyCapsulePinched remain compatible.
- Shared energy does not reset on mode changes; a requested change while held commits after release.
- No failure labels, energy deductions or countdowns.
- Release debounce .15 seconds; tracking-loss grace .3 seconds, settled at loss timestamp.
- New guide graphics stay on the tabletop outside the bag footprint; particles rise at most .03/.04 metres.
- Do not change tracking/calibration or 100% rewards, falling effects, release button, or walking gameplay.
- Preserve unrelated working-tree modifications. Do not commit or push.
- 89 BPM, beat offset 0 and input latency .05 seconds are provisional, not device-verified calibration.

## Review Focus

- Simultaneous hands/bags must not award two starts or terminate while another contact remains.
- Same-frame release/disable/world exit must settle at most once and leave no guide, audio or subscription behind.
- Music crossfades, looping, pause/resume and missing audio must not produce discontinuous scoring time.
- A calibrated nonuniformly scaled bag must not distort buttons, tabletop geometry or sorting order.
- New 20/50/80 feedback must not duplicate houses, trees, Honey Jar or automatically start 100% rain.

## Current-code findings and implementation decisions

1. The archive is based on 70fda52; current code additionally contains the new walking companion, mood-plate visuals, and tracking-modal fixes. Preserve those changes.
2. Existing CandyCompletionHouseController and NatureForestGrowthController already spawn progress props; Nature thresholds are .35/.55/.75/.9/1 and Honey Jar is .92. Do not replace these with the archive's conceptual 20/50/80 list.
3. Old RewardSequenceController spawns an orb above the bag, duplicates physical props, and may auto-run reward rain. Add an opt-in tabletop-feedback-only policy for this feature, handling 20/50/80 only, with default legacy behavior preserved for existing consumers/tests. Reuse its once-only stage delivery bookkeeping, not its floating-orb/automatic-rain path.
4. +Z for guide UI means bag-to-viewer, calculated from the viewer and bag positions projected onto the table plane. The archive's parenthetical 'camera forward' points the opposite way; use the stated spatial intent, not that sign error. Lock this heading once when the guide binds.
5. Both BGM .wav files are actually MP3 (verified with file). Rename to .mp3 with the existing .meta GUIDs and update any string paths, preserving scene/asset GUID references.
6. Support finger touch and supported native spatial pointer selection. Do not advertise actual gaze tracking or synthesize head-gaze input as a substitute for native system input. Missing fingertip data must not block palm-based bag play.
7. Closure is optional; return unavailable/zero bonus when hand joints are occluded, never infer a pressure measurement.

### Task 1: Pure scoring and tuning

**Create:** Assets/Scripts/Moodium/Squeeze/{SqueezeSample,ISqueezeScorer,FreeScorer,RhythmScorer,BreathScorer,EnergyAssist,SqueezeTuning}.cs and matching metas; Assets/Editor/SqueezeScoringTests.cs.

**Interfaces:** Namespace Moodium.Squeeze. SqueezeMode enum Free/Rhythm/Breath. SqueezeSample carries double time, holdSeconds, gapSeconds and nullable closure. ISqueezeScorer exposes OnStarted(sample), OnHeld(sample,deltaSeconds), OnReleased(sample), each returning nonnegative energy; RhythmScorer exposes accuracy, beat error, combo and adaptive interval; BreathScorer exposes quality, count and release state. SqueezeTuning creates per-session parameter copies so user-selected modes stop using mood overrides without mutating shared assets.

- [ ] Write tests for perfect/window-boundary rhythm accuracy, combo 8/rush bonus, .4/.7-second spam multipliers, four misses activating adaptation and stable adaptive intervals.
- [ ] Write breath integral tests at 4, 6 and >6 seconds, q at 1.2/3/5.5/8, exhale quality at 2/5, floor spam multiplier, assist at 75/150, Free +5.
- [ ] Implement spec formulas. Held scoring uses an analytic integral difference, not frame-rate-sensitive endpoint sampling. Reject nonfinite/backward times and clamp negative delta.
- [ ] Run scoring tests and verify large vs small time steps produce equal energy.

### Task 2: Contact lifecycle

**Modify:** Assets/Scripts/Interaction/ChocolateCapsuleInteraction.cs; modify PalmPressGestureController only if needed to expose tracking availability, without changing contact thresholds or calibration.
**Create:** SqueezeTracker.cs, HandClosureSampler.cs; Assets/Editor/SqueezeTrackerTests.cs.

**Interfaces:** Preserve AnyCapsulePinched; add AnyCapsuleHeld(capsule,Vector3) and AnyCapsuleReleased(capsule). Tracker exposes Started/Held/Released and IsHeld, Reset, Tick(double now). Use source/contact identity aggregation, and explicit tracking availability rather than treating missing Held heartbeats as a physical release.

- [ ] Test .1-second separation merges, .15-second separation releases, recovery within .3 continues hold, long loss settles at original loss time, two hands/bags overlap into one interval, disable clears once.
- [ ] Emit new events from existing contact methods and all reset/disable paths, preserving old feedback and pinch events.
- [ ] Subscribe/unsubscribe only for the active world session; verify no ghost scoring after exit.

### Task 3: Shared energy and mode integration

**Modify:** CandyEnergyController.cs, CandyWorldManager.cs, MoodiumAppFlowController.cs, MoodCheckContext.cs.
**Create:** SqueezeModeController.cs; Assets/Editor/SqueezeModeControllerTests.cs.

**Interfaces:** CandyEnergyController.AddEnergy(float amount,string reason) performs clamping and once-only threshold crossing. RegisterPinch delegates +5 unchanged. ModeController.BeginSession(energy,tuning,preSample,world,visitId), RequestMode(mode), StopSession; exposes current/pending/recommended mode and scorer snapshots. Session receives existing MoodCheckContext through app flow explicitly, not a global scene search.

- [ ] Test energy unchanged on switch, held switch delayed to release, reset of combo/breath state, neutral/no-sample Free, Low Rhythm .28, Tense Breath exhale 6, manual override clearing mood preset.
- [ ] Preserve existing bag-source validation; only accepted tracked/calibrated sensory bags feed scoring, not generated reward props.
- [ ] Free mode keeps the legacy accepted-start cadence; guided modes aggregate debounced holds. Ensure only one path adds energy.
- [ ] Throttle only rendered energy/progress updates to 10 Hz; thresholds and mode completion remain immediate and final 100% is flushed synchronously.

### Task 4: Asset import, tabletop frame and mode buttons

**Create:** Assets/UI/Moodium/SqueezeModes/ from the archive including original metas; ModeButtonBar.cs, TableTextSlot.cs, GuideHintView.cs and view-layout tests.

- [ ] Import all 20 sprites and 58 composite text textures. Give composite text a stable Resources location for runtime loading, retaining GUIDs and avoiding duplicate resource names.
- [ ] Bind using calibrated width/height/depth and an unscaled scene-rooted tabletop frame; follow anchor pose without editing tracking or calibration.
- [ ] Implement layout.json dimensions, fixed viewer-facing table heading, +Z toward viewer, 30-degree text tilt, fingertip-only buttons and native pointer activation.
- [ ] Implement 1-second delay/.4 fade, lift/depress, ripple, recommended dot and queued-selection appearance. At 100%, disable button input immediately and fade guides out.
- [ ] Use explicit per-layer table heights and VisionOSSortingGroup; verify resource loading, material alpha/depth state and cleanup.

### Task 5: Audio clock and rhythm guide

**Modify:** MoodiumAudioManager.cs; rename BGM media and metas preserving GUIDs.
**Create:** RoundedRectBandMesh.cs, RhythmGuideView.cs and mesh/audio-clock tests.

- [ ] Expose TryGetBackgroundMusicTime(out double seconds) from the source playing the current BGM, including crossfade transition and loop unwrapping; Nature uses a session clock and low-volume local tick.
- [ ] Use BPM 89 / two beats, offset 0, latency .05 as editable defaults. Flag first-beat alignment for later listening/recording verification.
- [ ] Adapt the supplied band mesh with fixed arrays, valid winding/UVs, no per-frame managed allocations and destroyed runtime meshes/materials on teardown.
- [ ] Draw target, 2.2-to-1 ring, judgment brightness, hit splash, 8 beads, rush feedback and tabletop-only particles. Trigger once per scored contact, never every frame held.
- [ ] Validate first-three-points, rapid-six-press and adaptive-once hint limits, and loop/pause-safe scoring clock.

### Task 6: Breath guide

**Create:** BreathGuideView.cs; extend tabletop view tests.

- [ ] Draw tide .012-.065, mirrored contour heads, .15-second full flash and exhale ripples every 1.2 seconds (max 3, lifetime 2.4).
- [ ] Pool capped native-renderable motes; inhale 15/s at .002-.012 height, exhale 9/s with max .04 height. Never enter the above-bag volume.
- [ ] Render supplied inhale/exhale words and nine count dots; enforce ready/first-two/long-hold-once/three-short-holds-once hints.
- [ ] Keep transforms/material changes visual only; never rescale bag colliders, calibration roots or hand targets. Account for existing deformation instead of competing writes to its Transform.

### Task 7: Stage feedback and visit log

**Modify:** RewardSequenceController.cs, MoodCheckContext.cs; integrate from CandyWorldManager.
**Create:** SqueezeLog.cs and tests.

- [ ] Add tabletop-only, 20/50/80 policy with no duplicate physical prefabs, floating orbs or automatic rain; keep old defaults and existing stage-growth thresholds.
- [ ] Write local JSONL per visit: per-squeeze metrics, from/to switches, energy deltas/assist, and final summary including modes, time-in-mode, accuracy, breaths, adaptive use and hints.
- [ ] Ensure scoring logs are disabled under tests, tolerate IO failures, do not write raw joint poses and never transmit logs.

### Task 8: Verification and handoff

- [ ] Compile runtime and Editor assemblies; regenerate any needed assets inside the existing Unity session without closing the user's editor or overwriting unsaved work.
- [ ] Run new scoring/tracker/mode/mesh/view tests and all EditMode tests; report unrelated failures separately (known old post-visit mood-check expectation must not re-enable that removed flow).
- [ ] Render Candy/Nature Free/Rhythm/Breath tabletop previews and check source storyboards, text positions and alpha sorting.
- [ ] Verify from code and tests that reaching 100% still uses the existing reward rain, leaves/frost and walking companion paths without resetting energy.
- [ ] Handoff explicit real-device checklist: fingertip/native pointer, long-hold debouncing, two-hand aggregation, BGM beat offset, seated text visibility, transparent sorting and 9-breath timing. Do not claim these have passed without device evidence.
