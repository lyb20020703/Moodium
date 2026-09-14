# Reality Palm Press Interaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Reality Enhancement thumb-bend input with left/right palm contact against the tracked Chocolate Capsule while preserving all existing capsule, particle, audio, and progress feedback.

**Architecture:** A small pure state machine owns one-trigger-per-contact and cooldown behavior. A Reality-mode controller follows the XR Hands palm joints with invisible kinematic trigger boxes, filters contacts to the current tracked capsule, and forwards valid presses through the existing `ChocolateCapsuleInteraction` entry point.

**Tech Stack:** Unity 6, C#, XR Hands, Unity Physics, NUnit EditMode tests

**Spec:** User-approved conversation design from 2026-09-14: the physical palm presses the tissue while the visible side is the back of the hand; either hand may activate once per contact and must leave before activating again.

## Global Constraints

- Change Reality Enhancement mode only.
- Preserve `ChocolateCapsuleInteraction.AnyCapsulePinched` so progress and environment feedback continue to work.
- A stationary palm must not repeatedly add progress.
- Finger-only contact and unrelated scene colliders must not activate the capsule.
- Palm proxy dimensions and cooldown remain serialized for Vision Pro tuning.

---

### Task 1: Palm contact state machine

**Files:**
- Create: `Assets/Scripts/Moodium/Gestures/PalmPressStateMachine.cs`
- Create: `Assets/Tests/EditMode/PalmPressStateMachineTests.cs`

**Interfaces:**
- Produces: `PalmPressStateMachine(float cooldownSeconds)`, `bool Update(bool touching, float currentTime)`, and `void Reset()`.

- [x] **Step 1: Write failing tests** proving initial contact triggers, held contact does not repeat, leaving rearms, cooldown blocks a rapid re-entry, and reset clears contact state.
- [x] **Step 2: Run EditMode tests** and confirm compilation fails because `PalmPressStateMachine` does not exist.
- [x] **Step 3: Implement the minimal state machine** with contact-edge detection and cooldown.
- [x] **Step 4: Run EditMode tests** and confirm the new tests pass.

### Task 2: XR palm collision controller and Reality flow integration

**Files:**
- Create: `Assets/Scripts/Interaction/PalmPressGestureController.cs`
- Modify: `Assets/Scripts/Interaction/ChocolateCapsuleInteraction.cs`
- Modify: `Assets/Scripts/Moodium/Reality/RealityEnhancementFlowController.cs`

**Interfaces:**
- Consumes: `PalmPressStateMachine.Update(bool touching, float currentTime)`.
- Produces: `PalmPressGestureController.SetTarget(ChocolateCapsuleInteraction)` and `SetInteractionEnabled(bool)`.
- Produces: a neutral one-shot capsule activation/release entry used by palm contact while retaining old gesture methods for compatibility.

- [x] **Step 1: Add an integration-oriented failing test** for public palm activation causing one capsule feedback event and release restoring its ready state.
- [x] **Step 2: Run EditMode tests** and confirm the test fails because the palm activation API does not exist.
- [x] **Step 3: Implement invisible left/right palm proxies** using the XR Palm joint pose, overlap boxes, target filtering, contact state, and tracking-loss cleanup.
- [x] **Step 4: Replace `BimanualThumbBendGestureController` setup in `RealityEnhancementFlowController`** with the palm controller and bind it only after the tracked capsule appears.
- [x] **Step 5: Run all EditMode tests and Unity compilation checks** and fix only regressions caused by this change.
