# Nature Vine Growth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make existing NatureSoft pinch reliably grow a local Ivy Studio vine on the real table surface, and add one gradual, one-shot completion burst at 100% Nature progress.

**Architecture:** A Nature-only resolver selects upward-facing AR Spatial Mesh hits below NatureSoft and is reused by local pinch, force test, and completion growth. `NatureIvySpatialGrowthController` remains the single adapter to `IvyGeneratorTREANT.generateIvyA`; it gains diagnostics and controlled burst scheduling, while `CandyWorldManager` only supplies the origin and detects the one-shot progress crossing.

**Tech Stack:** Unity C#, AR Foundation `ARMeshManager`, PolySpatial Spatial Mesh colliders, Ivy Studio `IvyGeneratorTREANT`, Unity Editor tests.

**Spec:** `docs/superpowers/specs/2026-09-28-nature-vine-growth-design.md`

## Global Constraints

- Working Tree current content is the source of truth; no reset, revert, checkout, clean, stash, unrelated refactor, or automatic Git commit.
- Do not modify Candy World effects, Candy Wrapper Crack, Sensory Bag Calibration, Image Tracking, or XR Hands main input.
- Reuse `IvyGeneratorTREANT.generateIvyA(RaycastHit)`; do not build a replacement vine system.
- Preserve existing NatureSoft pinch input and post-100% environment gaze+pinch input.
- Keep debug diagnostic-only and disabled by default.

## Review Focus

- A virtual collider nearer than the table must never be selected as a Nature growth surface; Task 1 tests spatial membership filtering.
- A wall or ceiling below/near the origin must lose to an upward-facing tabletop; Task 1 tests normal filtering.
- A pinch that has no valid table must not consume the manual vine budget; Task 2 tests failure leaves count unchanged.
- Repeated progress callbacks at or above 100% must not start multiple completion bursts; Task 3 tests the one-shot guard.
- A successful Ivy API call that creates no renderer must be reported as renderer failure rather than logged as visual success; Task 2 tests inspection status.

---

### Task 1: Spatial Mesh tabletop resolver

**Files:**
- Create: `Assets/Scripts/Moodium/NatureIvySurfaceResolver.cs`
- Create: `Assets/Editor/NatureIvySurfaceResolverTests.cs`

**Interfaces:**
- Produces: `NatureIvySurfaceResolver.TryFindSurfaceBelow(Vector3 origin, out RaycastHit hit) -> bool`.
- Consumes: configured `ARMeshManager` and its managed Spatial Mesh colliders.
- Used by Task 2 and Task 3.

- [ ] **Step 1: Write failing resolver tests**

Create tests proving an upward-facing Spatial Mesh collider beneath an origin is returned, a non-spatial virtual collider is rejected, and a wall does not qualify when its normal is below the configured upward-dot threshold.

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: FAIL because `NatureIvySurfaceResolver` does not exist.

- [ ] **Step 3: Implement `NatureIvySurfaceResolver`**

Implement `TryFindSurfaceBelow(Vector3 origin, out RaycastHit hit)` using a primary downward ray then finite radial downward samples. Require known `ARMeshManager` Spatial Mesh membership, `Vector3.Dot(hit.normal, Vector3.up) >= minUpNormalDot`, and configured maximum distance. Return the closest valid hit and no hit otherwise.

- [ ] **Step 4: Run resolver tests and compile**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: PASS with zero errors.

### Task 2: Pinch surface repair, Ivy diagnostics, and Force Test

**Files:**
- Modify: `Assets/Scripts/Moodium/NatureIvySpatialGrowthController.cs`
- Modify: `Assets/Editor/NatureIvySpatialGrowthControllerTests.cs`

**Interfaces:**
- Consumes: `NatureIvySurfaceResolver.TryFindSurfaceBelow(Vector3, out RaycastHit)` from Task 1.
- Produces: `TryGrowNearInteraction(Vector3 interactionPosition) -> bool`, `TryForceTestGrowth(Vector3 natureSoftOrigin) -> bool`, and structured debug state/logging for `generateIvyA` requests.
- Used by Task 3 through the existing `TryGrowAtSpatialHit` and new completion API.

- [ ] **Step 1: Write failing controller tests**

Add tests asserting a failed near-interaction surface lookup does not increment manual stroke count, while a valid returned hit calls the existing generator path and increments once. Add a test for the generator inspection result reporting `Renderer Not Found` when a request creates no renderers.

- [ ] **Step 2: Run controller tests to verify they fail**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: FAIL because the new resolver-dependent behavior and diagnostics are absent.

- [ ] **Step 3: Implement minimal controller changes**

Replace camera-to-centre casting in `TryGrowNearInteraction` with resolver lookup below NatureSoft. Keep `TryGrowAtSpatialHit` as the Ivy Studio entry and only consume manual budget after a valid request. Add debug-labelled logs and delayed renderer inspection of generated Ivy children. Add a Context Menu and one-shot serialized Force Test command that resolves below a supplied NatureSoft origin and requests a 0.5–1.0 m vine.

- [ ] **Step 4: Run controller tests and compile**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: PASS with zero errors.

### Task 3: Progress-100 completion burst and truthful prompt timing

**Files:**
- Modify: `Assets/Scripts/Moodium/NatureIvySpatialGrowthController.cs`
- Modify: `Assets/Scripts/Moodium/CandyWorldManager.cs`
- Modify: `Assets/Scripts/Moodium/Reality/WorldEnvironmentPromptPolicy.cs`
- Modify: `Assets/Editor/NatureIvySpatialGrowthControllerTests.cs`

**Interfaces:**
- Consumes: Task 1 resolver and Task 2 diagnostics/generator request entry.
- Produces: `TryStartCompletionBurst(Vector3 natureSoftOrigin) -> bool`, guarded one-shot by `CandyWorldManager` at transition `< 1f` to `>= 1f`.

- [ ] **Step 1: Write failing completion and prompt tests**

Add tests showing the completion API schedules no duplicate burst once started and that Nature environment guidance is false before progress 1.0 and true at progress 1.0. Add a test showing scattered candidate starts are distinct rather than all equal to the primary hit.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: FAIL because completion one-shot/scattered start behavior and progress-aligned prompt policy are absent.

- [ ] **Step 3: Implement completion burst and state integration**

Store the last accepted NatureSoft visual origin in `CandyWorldManager` before energy progress is registered. On the first Nature progress crossing from `< 1f` to `>= 1f`, call `TryStartCompletionBurst`; reset the guard when the Nature world session stops. In the controller, resolve the tabletop, derive 4–8 scattered projected starts, set Ivy Studio `branchCount = 1` and `directionality` to approximately radial directions with configurable angular randomness, stagger requests over configured duration, and restore generator settings afterward. Change only Nature prompt policy to require progress >= 1f.

- [ ] **Step 4: Run completion tests and compile**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: PASS with zero errors.

### Task 4: Whole scoped verification

**Files:**
- Modify only if a compilation or covered-test failure demonstrates a scoped defect.

**Interfaces:**
- Consumes: Tasks 1–3.
- Produces: compilation evidence and a device-test checklist, without committing.

- [ ] **Step 1: Run all relevant editor tests and compile**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Expected: PASS with zero errors.

- [ ] **Step 2: Inspect scoped diff for forbidden changes**

Run: `git diff -- Assets/Scripts/Moodium/NatureIvySpatialGrowthController.cs Assets/Scripts/Moodium/NatureIvySurfaceResolver.cs Assets/Scripts/Moodium/CandyWorldManager.cs Assets/Scripts/Moodium/Reality/WorldEnvironmentPromptPolicy.cs Assets/Editor/NatureIvySpatialGrowthControllerTests.cs Assets/Editor/NatureIvySurfaceResolverTests.cs`

Expected: only planned Nature-vine changes; no Candy, calibration, tracking, or XR Hands main-system edits.

