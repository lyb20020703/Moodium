# Candy Jelly Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a PolySpatial-compatible, visual-only jelly coating for Candy World's spatial mesh, gated by a physical Vision Pro vertex-displacement result.

**Architecture:** A temporary smoke scene proves whether RealityKit accepts Shader Graph vertex displacement. If it does, the supplied controller drives two Shader Graph material layers over unmodified AR meshes; events bridge bag squeezes and mesh touches to material properties, while crack decals use the layer thickness. If it does not, preserve the fragment-only material path and stop before claiming physical jelly deformation.

**Tech Stack:** Unity 6000.0.30f1, URP Shader Graph 17.0.3, PolySpatial visionOS 2.4.3, AR Foundation 6.0.3, XR Hands 1.5.0, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-10-23-candy-jelly-layer-design.md`

## Global Constraints

- Use the supplied handoff sources as the baseline; do not recreate the controller or reference math independently.
- RealityKit device rendering uses Shader Graph only; retain `CandyJelly.shader` as Editor/reference-only.
- Do not alter image tracking, calibration, energy calculation, bag-pinch recognition, or XR Hands systems.
- Do not delete `CandyTransformationShader.shader` or `CandyTransformationMaterial.mat`.
- Never CPU-deform or per-frame synchronize spatial mesh vertices.
- Add to the dirty worktree without overwriting, reverting, cleaning, stashing, committing, or changing unrelated work.
- Require a physical Vision Pro result, screenshot/recording, and device log before asserting vertex displacement works.

## Review Focus

- A concave `MeshCollider` must not register an impression simply because the fingertip is within its volume; raycast contact must be real.
- Meshes without normals must use a readable copied mesh with recalculated normals, while unreadable meshes log the documented warning and remain safe.
- A removed or replaced AR mesh must destroy its overlay and runtime normal copy without retaining stale colliders.
- Ripple and pinch event subscriptions must be removed on destruction, avoiding duplicate triggers after reconfiguration.
- A failed device vertex test must not lead to CPU deformation or a claim of stereo thickness.

---

### Task 1: Vision Pro Vertex-Displacement Smoke Gate

**Files:**
- Create: `Assets/Materials/Shaders/CandyJellyVertexSmoke.shadergraph`
- Create: `Assets/Materials/CandyJellyVertexSmoke.mat`
- Create: `Assets/Editor/CandyJellyVertexSmokeSetup.cs`
- Create: `Assets/Samples/PolySpatial/Scenes/CandyJellyVertexSmoke.unity`

**Interfaces:**
- Produces: a standalone scene containing a cube whose material exposes `_Offset` and displaces its vertex Position by `Normal(Object) * (_Offset + Sine(Time) * 0.02)`.
- Produces: a physical-device pass/fail artefact, device log, and short recording/screenshot outside source control.

- [ ] **Step 1: Create the smoke Shader Graph and cube scene**

Set the graph to URP Lit and add `_Offset` (Float). In the Vertex Position block, use object-space position plus object-space normal times `_Offset + Sine(Time) * 0.02`; keep the fragment graph deliberately plain so visual movement is unambiguous.

- [ ] **Step 2: Build and deploy only the smoke scene using Unity 6000.0.30f1**

Run from the host that has a paired Vision Pro; capture a short recording showing the cube and collect the device/Xcode log.

- [ ] **Step 3: Record the gate result before continuing**

Expected pass: the cube visibly expands and contracts along its normals. Expected fail: MaterialX preserves the fragment shading but ignores vertex offset. On fail, report evidence and stop the full deformation implementation; do not substitute CPU mesh edits.

### Task 2: Controller Algorithm and Regression Tests

**Files:**
- Create: `Assets/Editor/CandyJellyLayerTests.cs`
- Modify: `Assets/Editor/CandySpatialMeshSoftShellTests.cs` only if its public compatibility assertions need an additional coverage case
- Modify: `Assets/Scripts/Moodium/Reality/CandySpatialMeshTransformationController.cs`
- Create: `Assets/Materials/Shaders/CandyJelly.shader`

**Interfaces:**
- Consumes: the supplied `CandySpatialMeshTransformationController.cs` and `CandyJellyLayerTests.cs`.
- Produces: `CurrentThickness`, `PulseWobble(float)`, `PulseWobbleForSqueeze()`, `TriggerRipple(Vector3)`, `ComputeTargetFill`, `ComputeSpreadRadius`, and `StepJellySpring`.
- Preserves: `Configure`, `StartTransformation`, `StopTransformation`, `SetOrigin`, `SetProgress`, `IsInteractionReady`, `IsOverlayActive`, `Progress`, `Origin`, and `ComputeSoftPressDisplacement`.

- [ ] **Step 1: Add the supplied `CandyJellyLayerTests` unchanged and run it**

Run with Unity 6000.0.30f1:

```bash
/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/yibei/Desktop/appcontest2026/Moodium -runTests -testPlatform EditMode -testFilter CandyJellyLayerTests -testResults /tmp/candy-jelly-red.xml -logFile /tmp/candy-jelly-red.log -quit
```

Expected: compile/test failure because the existing controller lacks the new public helpers.

- [ ] **Step 2: Replace the controller with the supplied implementation, retaining only proven project-compatibility fixes**

Keep the two runtime material layers, normal-copy fallback, short-ray contact search, damped press springs, spread/fill helpers, wobble, ripple and awakening values. Any change to the supplied source must be documented with its compile error or compatibility rationale.

- [ ] **Step 3: Add the supplied HLSL reference shader and import assets**

Place it at `Assets/Materials/Shaders/CandyJelly.shader`; it is for Editor comparison only and must not become the Vision Pro material.

- [ ] **Step 4: Run focused old and new tests**

Run the same command with `CandyJellyLayerTests|CandySpatialMeshSoftShellTests` where Unity test filtering supports it, otherwise run two focused commands. Expected: both fixtures pass, including fill/spread boundaries, spring overshoot/settling, and legacy press falloff.

### Task 3: PolySpatial-Compatible Shader Graph and Material

**Files:**
- Create: `Assets/Materials/Shaders/JellySpread.shadersubgraph`
- Create: `Assets/Materials/Shaders/JellyPress.shadersubgraph`
- Create: `Assets/Materials/Shaders/CandyJelly.shadergraph`
- Create: `Assets/Materials/CandyJellyMaterial.mat`
- Create: `.meta` files through Unity asset import
- Modify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity`

**Interfaces:**
- Consumes: controller property references `_Fill`, `_Thickness`, `_LayerDepth`, `_LayerOpacity`, `_CandyOrigin`, `_SpreadRadius`, `_SpreadSoftness`, `_WobbleImpulse`, `_PressA`, `_PressB`, `_PressRadius`, `_RipplePosition`, `_RippleAge`, `_RippleStrength`, and `_AwakeningFlash`.
- Produces: material `CandyJellyMaterial` using the graph and assigning `Assets/Moodium/Textures/CandySparkleMask.png` to `_SparkleMask`.

- [ ] **Step 1: Build the two Sub Graphs exactly from the supplied guide**

`JellySpread` returns the soft radius mask. `JellyPress` returns dent/rim offset and press falloff, including the guarded radius division. Confirm their exposed input/output references match the guide.

- [ ] **Step 2: Build `CandyJelly.shadergraph` from the supplied guide**

Use the documented transparent Lit settings, property reference names, vertex displacement, fragment iridescence, manual three-plane sparkle sampling, and no Custom Function, Scene Color, Scene Depth, or Triplanar nodes. Use the manual Fresnel expression if PolySpatial rejects `Fresnel Effect`.

- [ ] **Step 3: Verify PolySpatial compatibility and asset binding**

Inspect the imported graph for unsupported nodes and confirm the Scene's `MoodiumAppFlowController.m_CandyTransformationMaterial` points only to the new material. Preserve the old material and preview references.

- [ ] **Step 4: Run Editor import/compile verification**

Open/import the project in Unity 6000.0.30f1 and inspect Editor.log for Shader Graph or PolySpatial conversion errors. Expected: no C# compile errors and no unsupported-node warnings for the graph.

### Task 4: Runtime Event and Crack Integration

**Files:**
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Modify: `Assets/Scripts/Moodium/Reality/SpatialMeshTouchRipple.cs`
- Modify: `Assets/Scripts/Moodium/Reality/CandyWrapperCrackDetector.cs`
- Test: `Assets/Editor/CandyWrapperCrackMathTests.cs`

**Interfaces:**
- Consumes: `CandySpatialMeshTransformationController.PulseWobbleForSqueeze()`, `TriggerRipple(Vector3)`, and `CurrentThickness`.
- Produces: `SpatialMeshTouchRipple.RippleTriggered` of type `System.Action<Vector3>`.

- [ ] **Step 1: Add the ripple event testable contract**

Declare `public event System.Action<Vector3> RippleTriggered;` in `SpatialMeshTouchRipple`, and invoke it in the exact branch that currently calls `CandyEnvironmentShaderController.TriggerRipple(touchPoint)`.

- [ ] **Step 2: Add minimal Candy World subscriptions**

In the existing `CandyWorldManager.Configure` wiring, unsubscribe then subscribe `m_SpatialTouchFeedback.RippleTriggered` to `m_SpatialTransformation.TriggerRipple`; unsubscribe in `OnDestroy`. After the existing accepted `m_Energy.RegisterPinch()`, call `m_SpatialTransformation?.PulseWobbleForSqueeze()` without changing pinch qualification.

- [ ] **Step 3: Offset cracks by jelly thickness**

At both `CandyWrapperCrackVisual.Spawn` call sites, add `m_Wrapper.CurrentThickness` to the existing normal surface offset while keeping the configured `m_SurfaceOffset` additive.

- [ ] **Step 4: Run compile and focused regression tests**

Run `CandyWrapperCrackMathTests`, `CandyJellyLayerTests`, and `CandySpatialMeshSoftShellTests`. Expected: all pass and Unity reports no delegate, method-group, or material-property compile errors.

### Task 5: Integrated Verification and Device Acceptance

**Files:**
- Verify: `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity`
- Verify: device recording/log from Task 1 and Candy World device session

**Interfaces:**
- Consumes: Tasks 1–4 and the physical-device smoke result.
- Produces: a delivery report listing changed assets, tests, device outcome, any no-normal warnings, and inspector parameter ranges.

- [ ] **Step 1: Run the complete EditMode suite from Unity 6000.0.30f1**

```bash
/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/yibei/Desktop/appcontest2026/Moodium -runTests -testPlatform EditMode -testResults /tmp/candy-jelly-all.xml -logFile /tmp/candy-jelly-all.log -quit
```

Expected: report the exact pass/fail count; distinguish pre-existing failures from jelly-related failures.

- [ ] **Step 2: Perform the Vision Pro Candy World acceptance pass**

Verify the 20%, 60%, and 100% visual stages; press/release bulge; squeeze quiver; touch ripple; and overlay performance with `m_InnerLayerOpacity` at its default and at zero. Save one recording/screenshot and scan logs for the documented unreadable-normal warning.

- [ ] **Step 3: Prepare the evidence-backed handoff**

List changed files, test command outcomes, the vertex-smoke result, device recording location, warnings, and recommended values for thickness, curve, damping/frequency, inner opacity/depth, and visual controls. Do not claim the device deformation works unless Task 1 and this task both have recorded evidence.

## Self-Review

- Spec coverage: Tasks 1–5 cover the device gate, supplied controller/tests/reference shader, Shader Graph/material/scene assignment, squeeze/ripple/crack wiring, mesh normals, and final evidence. No requirement is intentionally omitted.
- Step scan: each action has one deliverable or verification result; the graph task retains the supplied guide as the exact node-level source instead of duplicating graph implementation prose.
- Type consistency: event uses `System.Action<Vector3>`; controller API names match the supplied source; crack depth reads `CurrentThickness`.
- Review focus: short-ray contact is owned by Task 2; normal-copy/stale overlay cases by Task 2; event cleanup by Task 4; device fallback by Tasks 1 and 5.
- Proportion: the plan specifies decisions, interfaces, tests, and safety gates without reprinting the supplied source or graph.
