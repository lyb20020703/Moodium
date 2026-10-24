# Moodi Breathing Float Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable, drift-free idle breathing float to the `Moodi meshy` instances in both world UI prefabs.

**Architecture:** A focused `MoodiBreathingFloat` MonoBehaviour captures authored local position and scale on enable, derives both animated values from one sine wave, and restores the authored transform on disable. The two existing nested prefab instances receive the component as an added-component override so the source model prefab remains unchanged.

**Tech Stack:** Unity 6, C#, Unity Test Framework, serialized prefab YAML.

**Spec:** `docs/superpowers/specs/2026-10-23-moodi-breathing-float-design.md`

## Global Constraints

- Default vertical amplitude is exactly `0.01m`.
- Default period is exactly `2.8s`.
- Default scale amplitude is exactly `0.01` (1%).
- Motion uses `Time.unscaledTime` and never accumulates from the previous frame.
- Disabling the component restores the authored local position and scale.
- Do not modify the Moodi mesh, materials, Animator, collisions, or world UI layout.
- Preserve unrelated changes in the existing dirty worktree.

## Review Focus

- Repeated enable/disable cycles must not shift the captured baseline; test two cycles and assert the same restored transform.
- A zero or invalid period from serialized data must not produce NaN; clamp the serialized value and test finite output.
- Non-uniform authored scale must remain proportional on all axes; test multiplication from the baseline vector.
- Prefab variant added-component overrides must target the `Moodi meshy` root rather than a child mesh; load both prefabs and assert component placement by object name.
- The two prefabs use different authored scales and positions; assert both retain their serialized baseline values after the component is added.

---

### Task 1: Reusable Breathing Float Component

**Files:**
- Create: `Assets/Scripts/Moodium/MoodiBreathingFloat.cs`
- Create: `Assets/Scripts/Moodium/MoodiBreathingFloat.cs.meta`
- Create: `Assets/Editor/MoodiBreathingFloatTests.cs`
- Create: `Assets/Editor/MoodiBreathingFloatTests.cs.meta`

**Interfaces:**
- Consumes: Unity `Transform`, `Time.unscaledTime`, and serialized float parameters.
- Produces: `Moodium.Flow.MoodiBreathingFloat`, plus pure helpers `EvaluateOffset(float elapsedSeconds, float amplitude, float period)` and `EvaluateScale(Vector3 baseline, float normalizedWave, float scaleAmplitude)` for deterministic tests.

- [ ] **Step 1: Write the failing edit-mode tests**

Add tests named `EvaluateOffset_UsesRequestedAmplitudeAndPeriod`, `EvaluateScale_PreservesNonUniformBaseline`, `EvaluateOffset_InvalidPeriod_RemainsFinite`, and `Disable_RestoresBaselineAcrossRepeatedEnableCycles`. Assert the exact defaults and baseline restoration behavior from the spec.

- [ ] **Step 2: Run the focused tests and verify failure**

Run Unity Test Framework for `MoodiBreathingFloatTests`.

Expected: FAIL because `MoodiBreathingFloat` does not exist.

- [ ] **Step 3: Implement `MoodiBreathingFloat`**

Create a sealed, disallow-multiple component with serialized fields `m_VerticalAmplitude = .01f`, `m_PeriodSeconds = 2.8f`, and `m_ScaleAmplitude = .01f`. Capture baselines in `OnEnable`, update from one sine wave in `Update`, and restore both values in `OnDisable`; guard period with a positive minimum.

- [ ] **Step 4: Run the focused tests and verify pass**

Run Unity Test Framework for `MoodiBreathingFloatTests`.

Expected: all focused tests PASS with no compiler errors.

- [ ] **Step 5: Commit the isolated component change**

Stage only the four Task 1 files and commit with `feat: add reusable Moodi breathing float` if commits are authorized for this workspace.

### Task 2: Attach the Component to Both World UI Prefabs

**Files:**
- Modify: `Assets/prefab/CandyWorldUI.prefab`
- Modify: `Assets/prefab/NatureWorldUI.prefab`
- Modify: `Assets/Editor/MoodiBreathingFloatTests.cs`

**Interfaces:**
- Consumes: `Moodium.Flow.MoodiBreathingFloat` from Task 1.
- Produces: Both world UI prefabs with the component attached directly to the nested instance root named `Moodi meshy`.

- [ ] **Step 1: Add failing prefab integration tests**

Add `CandyWorldUI_MoodiMeshy_HasBreathingFloat` and `NatureWorldUI_MoodiMeshy_HasBreathingFloat`. Load each prefab, find the transform named exactly `Moodi meshy`, assert one component is present on that object, and assert the authored local position and scale remain unchanged (`Candy`: `(0,.023,-.11)`, `.05`; `Nature`: `(.064,.018,-.087)`, `.03`).

- [ ] **Step 2: Run focused tests and verify integration failure**

Run Unity Test Framework for `MoodiBreathingFloatTests`.

Expected: component unit tests PASS; both prefab integration tests FAIL because the component is not attached.

- [ ] **Step 3: Add nested-prefab component overrides**

Attach `MoodiBreathingFloat` to the `Moodi meshy` root in `CandyWorldUI.prefab` and `NatureWorldUI.prefab`, retaining the default parameter values and all existing prefab overrides.

- [ ] **Step 4: Run focused tests and project checks**

Run Unity Test Framework for `MoodiBreathingFloatTests`, refresh scripts in the open Unity editor, inspect the editor log for C# errors, and run `git diff --check`.

Expected: focused tests PASS; no C# compiler errors; no whitespace errors.

- [ ] **Step 5: Commit the prefab integration**

Stage only the two prefabs and the test update, then commit with `feat: animate Moodi in world UI prefabs` if commits are authorized for this workspace.
