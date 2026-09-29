# Nature World Vine Growth — Phase B and C Design

## Scope and constraints

This change repairs the existing Nature World vine path and adds one completion
effect. It reuses the project’s existing `IvyGeneratorTREANT`, `ARMeshManager`,
Spatial Mesh colliders, Nature progress, and existing pinch input. It does not
change Candy World, Candy Wrapper cracks, calibration, image tracking, or the
XR Hands input system.

The working tree is the source of truth. No reset, revert, checkout, clean,
stash, unrelated refactor, or automatic Git commit is part of this work.

## Diagnosis captured before implementation

The current local Nature pinch route is:

`ChocolateCapsuleInteraction.AnyCapsulePinched`
→ `CandyWorldManager.OnCapsulePinched`
→ `NatureIvySpatialGrowthController.TryGrowNearInteraction(visualCenter)`
→ camera-to-NatureSoft-centre raycast
→ `IvyGeneratorTREANT.generateIvyA(hit)`.

The likely failure is the camera-to-centre ray. When NatureSoft is on a table,
that ray normally reaches the soft object, not the table’s upward-facing
Spatial Mesh; no valid hit is supplied to Ivy Studio. Current code also treats
`generateIvyA` as successful without checking whether it created renderable
geometry.

The existing post-completion environment route is separate:

`SpatialPointerPhase.Began`
→ `MoodiumSpatialInteractionController`
→ `CandyWorldManager.TryActivateEnvironmentFromSpatialPointer`
→ `WorldEnvironmentGazeInteractor`
→ `NatureIvySpatialGrowthController.TryGrowAtSpatialHit(hit)`.

It is enabled only at progress 100%. The current Nature prompt instead becomes
eligible after the first bag interaction, so it can advertise an interaction
before it is usable.

`IvyGeneratorTREANT.generateIvyA(RaycastHit)` is the correct existing spawn
entry point. The Ivy Studio generator exposes a public `directionality`
vector. With it unset, its branches choose radial directions around the hit
normal with randomness. With it set, it uses that vector as the initial
direction (with a small random variation). Therefore Phase C can control
approximate radial directions without replacing the plugin.

## Shared surface resolution

Add one small Nature-only surface resolver used by both local pinch growth and
completion growth. It will accept a NatureSoft visual centre/bottom query
origin and return a real AR Spatial Mesh `RaycastHit`.

1. Cast downward from the NatureSoft visual centre/bottom vicinity.
2. Accept only colliders owned by the configured `ARMeshManager` Spatial Mesh
   set, never NatureSoft, calibration, or other virtual colliders.
3. Prefer normals whose dot product with world-up meets an Inspector threshold
   (default `0.65`).
4. If the primary cast misses, sample a small finite ring around the origin and
   choose the closest valid upward-facing surface within an Inspector maximum
   search distance.
5. Return the original `RaycastHit`, retaining its hit point, normal, collider,
   and mesh transform.

The existing pointer-based environment interaction remains pointer based. It
already has a surface hit and must not be converted to a new hand-input path.

## Phase B — existing NatureSoft pinch

`TryGrowNearInteraction` will stop casting from the camera to the NatureSoft
centre. Instead it will call the shared resolver from NatureSoft’s actual
visual centre/bottom and, on success, pass that returned hit to the existing
`TryGrowAtSpatialHit` / `IvyGeneratorTREANT.generateIvyA(hit)` path.

The existing input event, the manual-vine stroke budget, the generator prefab,
and Ivy Studio growth material remain in place. A failure to find a valid table
will be observable through debug output rather than silently treated as a
successful pinch.

## Force Test Vine Growth

Add an opt-in debug command to the Nature ivy controller, exposed through a
Context Menu for Editor use and a one-shot Inspector boolean routed from the
existing world setup for device builds. It will not require a pinch or progress
100%.

It will:

1. Resolve the upward Spatial Mesh under the current NatureSoft origin.
2. Request one conspicuous Ivy Studio vine of roughly 0.5–1.0 m.
3. Log a structured `[Nature Vine]` trace containing: surface-found result,
   hit point, normal, Spatial Mesh name, `generateIvyA` call, generated object
   discovery, MeshRenderer count, renderer enabled state, material and shader
   names, renderer bounds, world position, and world scale.

After `generateIvyA`, renderer inspection will occur after the generator has
had a frame to create its branch objects. If no generated object, mesh, or
renderer exists, the trace will explicitly say so. This distinguishes a failed
surface search from an Ivy Studio renderer/material/PolySpatial issue.

## Phase C — completion vine burst

The first Nature progress transition from below 1.0 to at least 1.0 triggers a
one-shot completion coroutine. It is reset only on a new Nature World session
or world reset.

The coroutine first resolves the primary upward Spatial Mesh beneath
NatureSoft. It then creates a configurable number of independent start hits:

- Default branch count: 6 (allowed range 4–8).
- Each start point is offset around the primary hit within `Start Point Scatter
  Radius`, then projected down onto a valid Spatial Mesh.
- Desired directions are evenly distributed around the surface normal with a
  configurable random angular deviation (default ±15 degrees).
- Each request sets Ivy Studio to one branch and uses its supported
  `directionality` vector for that approximate initial direction.
- Requests are staggered over the configurable growth duration rather than
  submitted in one frame. Ivy Studio’s existing grow shader/controller remains
  responsible for the visible tip-growth animation.

This uses several distinct surface hits and directions, rather than calling
`generateIvyA` six times at one hit point. If a scattered sample cannot be
projected onto the same valid spatial surface, that branch is skipped and its
debug stage identifies the failure.

The generator’s existing `maxBranchPositions * branchPartLength` controls
physical branch length. Completion settings will map the target radius (default
1.0 m) to those existing values, while preserving normal manual pinch limits.
The existing generator `surfaceOffset` is exposed and set to a small default
offset to avoid both table penetration and visibly floating vines.

## Prompt timing

Change only the Nature condition in the existing environment prompt policy:
show the Nature environment guidance when Nature environment interaction is
actually enabled (Nature progress >= 100%), not immediately after the first
NatureSoft pinch. The existing text/UI presentation is retained.

## Debug contract

An Inspector `Nature Vine Debug` toggle will emit concise, stage-labelled
messages:

`Pinch Detected` → `Nature Interaction Accepted` → `Surface Search Started`
→ `Surface Found` / `Surface Failed` → `Ivy Request`
→ `generateIvyA Called` → `Generated Object Found` / `Not Found`
→ `Renderer Found` / `Not Found` → `Growth Started`.

Completion entries additionally include `Completion Burst Triggered` and a
branch index. The controller will retain readable last-state fields where
Unity’s Inspector can show the most recent result.

The trace is diagnostic only; it must not change game behaviour when debug is
disabled.

## Planned file scope

Modify incrementally from the current working-tree versions:

- `Assets/Scripts/Moodium/NatureIvySpatialGrowthController.cs` — resolver use,
  generator diagnostics, force-test entry, and completion burst.
- `Assets/Scripts/Moodium/CandyWorldManager.cs` — track NatureSoft origin,
  one-shot progress crossing, controller calls, and reset state.
- `Assets/Scripts/Moodium/Reality/WorldEnvironmentPromptPolicy.cs` — align
  Nature prompt timing with progress-100 availability.
- `Assets/Scripts/Moodium/MoodiumAppFlowController.cs` only if required to
  expose/pass the Nature debug settings.
- `Assets/Editor/NatureIvySpatialGrowthControllerTests.cs` — extend tests for
  resolver/budget/one-shot behaviour as practical.

Add only Nature-focused files if needed:

- `Assets/Scripts/Moodium/NatureIvySurfaceResolver.cs` — real Spatial Mesh
  surface selection.
- `Assets/Scripts/Moodium/NatureIvyGrowthSettings.cs` — serializable Inspector
  settings, if keeping the controller fields alone would be less clear.
- Editor tests for resolver selection if it can be isolated safely.

No Ivy Studio asset, shader, prefab, Candy system, calibration system, Image
Tracking system, or XR Hands main-system file is to be changed.

## Initial Inspector defaults

- Minimum upward normal dot: `0.65`
- Maximum surface search distance: `0.80 m`
- Surface offset: `0.003–0.006 m`
- Completion branch count: `6`
- Completion radius: `1.0 m`
- Start point scatter radius: `0.08–0.15 m`
- Direction randomness: `15 degrees`
- Completion stagger/growth duration: `1.2–1.8 s`
- Force test target length: `0.75 m`

These are starting points for device tuning, not hardcoded limits.

## Verification

1. Compile Unity C# assemblies after each scoped change.
2. In Editor, use Force Test with a known MeshCollider table: confirm an
   upward hit and a rendered generated branch.
3. On Vision Pro, use Force Test first. Its trace determines whether a failure
   is in surface selection, Ivy generation, generated renderer/material, or
   PolySpatial shader rendering.
4. Test an ordinary NatureSoft pinch while it sits above a table: one local
   vine should grow without changing the pinch input.
5. Reach 100% once: exactly one completion burst should spread from distinct
   nearby table points; later progress callbacks must not duplicate it.
6. After reaching 100%, test the existing environment gaze+pinch route; it
   remains independent of the completion burst.

If Force Test logs a generated enabled renderer and valid bounds but nothing
is visible on device, the next diagnosis target is the Ivy Studio grow material
or shader’s PolySpatial compatibility—not a replacement vine implementation.
