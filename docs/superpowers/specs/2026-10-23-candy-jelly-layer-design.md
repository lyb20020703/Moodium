# Candy Jelly Layer — short design

## Intent

Replace Candy World's flat, transparent spatial-mesh overlay with a visual-only jelly layer that spreads from the sensory bag, grows to about 2.2 cm, responds to touches, ripples on surface contact, and quivers when the bag is squeezed. AR mesh tracking, colliders, energy calculation, image tracking, calibration, and XR Hands remain untouched.

## Evidence and constraints

- The provided handoff contains the authoritative controller, HLSL reference shader, Shader Graph build guide, and EditMode tests; implementation will preserve those sources and make only compatibility corrections discovered during integration.
- The project runs PolySpatial 2.4.3 in RealityKit mode, so the device material must be a Shader Graph. `CandyJelly.shader` is an Editor-only mathematical reference.
- Shader Graph vertex-position support on a physical Vision Pro is unproven. It is a release gate, not something an Editor test can establish.
- The supplied controller avoids the existing concave-MeshCollider `ClosestPoint` false-contact problem using short raycasts, and never alters spatial mesh vertices each frame.
- The worktree is dirty, including `CandyWorldManager.cs`; integration must add only isolated lines/events, never replace or revert unrelated changes.

## Approaches considered

1. **Full Shader Graph jelly layer — selected.** Use the supplied controller and tests, build the documented Shader Graph/subgraphs and material, then connect squeeze, ripple, and crack depth. It meets the intended visual behaviour but requires the Vision Pro smoke result for confirmation of thickness/deformation.
2. **Fragment-only Shader Graph fallback.** Keep the same spread, tint, iridescence, sparkle and ripple signals but omit vertex displacement. This is the fallback only if the smoke test proves vertex position is unsupported; it cannot provide stereo thickness or fingertip dents.
3. **CPU mesh deformation.** Rejected: it would repeatedly synchronize high-density spatial meshes and violates the provided constraints.

## Implementation sequence

1. Create an isolated, temporary Shader Graph cube smoke scene/material with `_Offset` plus a time-driven normal displacement. Build/deploy it on Vision Pro and record whether it visibly breathes. Do not begin the full graph until a physical-device result exists.
2. If it works, copy the supplied controller, reference shader, and EditMode tests into their specified locations. Preserve existing `CandyTransformation*` assets.
3. Build `CandyJelly.shadergraph`, `JellySpread.shadersubgraph`, and `JellyPress.shadersubgraph` from the supplied guide, restricting nodes to PolySpatial-supported operations. Create the jelly material, assign the sparkle mask, and update only the scene material reference.
4. Add non-overlapping integration: squeeze pulse after accepted pinch registration; touch-ripple event publication/subscription and cleanup; crack normal offset by `CurrentThickness`.
5. Run the supplied and existing EditMode test fixtures, compile/import in the project Unity version, and report the device smoke outcome separately from Editor verification.

## Success criteria

- At 20% the overlay begins as a thin, rolling film; at 100% it targets 0.022 m, with two visual layers for depth.
- Released presses spring and overshoot; squeeze and touch propagate the documented material signals.
- Existing soft-shell tests and supplied jelly tests pass; no unrelated file is overwritten.
- If device vertex displacement fails, report it with evidence and retain the fragment-only material path—no CPU deformation fallback.
