# Sensory bag tabletop height implementation plan

> Execute inline with superpowers:executing-plans, preserving the user's live checkout and all pending WorldGuide/three-second walk edits. User approved the short design in chat. No commit/push.

**Goal:** Default calibration 40×25cm, tabletop initially 5cm below the sticker, with independent normal-axis height drag and ±0.5cm buttons; model bottom and interaction volume share that plane.
**Architecture:** Calibration data remains anchor-local X/Z rectangle plus Y-normal plane offset. Existing edge/corner and frame drags keep their planar semantics. An additional Height handle changes only normal offset. Presentation recenters the model's footprint and bottoms its actual local renderer bounds onto the calibrated plane without changing tracking rotation or existing proportional thickness scaling.
**Spec:** User-approved preceding short design: independent height control, default -5cm, .5cm steps, full sticker-frame alignment; both CandySoft and NatureSoft.
**Tech:** Unity 6000.0.30f1, PolySpatial native colliders and existing calibration pointer routing, TMP Chinese/English labels.

## Constraints / decisions
- Source defaults and serialized scene settings both become Width .40, Height .25m. Existing limits .08–.60 and .06–.45m remain.
- Default plane offset is -DefaultDepth (-.05m); physical normal is anchor-local Y, not world Z. Offset zero is a valid explicit choice, never an Inspector fallback.
- Height drag only changes PlaneLocalOffset.y, clamp [-.25,.10]m; fine controls ±.005m. Reset restores initial default plane, confirmation and temporary tracking loss end active drags.
- Preserve prefab root scale and current geometric-mean thickness fit; do not silently reshape the mesh thickness to 5cm. Bottom the actual scaled bounds and center the X/Z footprint. Interaction volume spans its configured Depth above the base plane.
- Video/hand tracked orientation, world guide and existing squeeze rules remain intact. Add editor mouse support for calibration controls as well as native pointer input.

## Review focus
- Non-centered pivots, .5 authored root scale, tilted/yawed image anchor: correct bottom position, footprint size and unchanged heading.
- Height changes must not resize/recenter in-plane; edge resize/frame translation must preserve height; clamp and nonfinite input cannot poison transforms.
- Zero offset, reset, confirm, tracking loss/reacquisition and stale pointers cannot resurrect old drags.
- Collider from base→above-plane, not centered partly below desk; normal direction and video center remain consistent.
- UI control/label is distinguishable from size handles, Chinese/English font and mouse/native routes share data changes.

### Task 1: Anchor-local height / controls
- [ ] Add RED tests for new 40×25 source/scene defaults and -5cm data, height-only drag, zero/clamp, fine-step actions and reset/confirm/loss safety.
- [ ] Update defaults/data/rectangle/calibrator/handle enums and serialized scene defaults; add normal-axis handle, ±.5cm controls and height readout; preserve existing size controls.
- [ ] Add editor mouse calibration routing (screen drag plane for height, sticker plane for size), verify native routing is unchanged.

### Task 2: Physical presentation alignment
- [ ] Add RED renderer-bottom and volume-center tests for off-center geometry, tilted anchors and authored .5 scale; use actual CandySoft/NatureSoft assets too.
- [ ] Calculate renderer bounds in mesh-local space (not inverted world AABB), center footprint and bottom geometry in the tracked frame, offset collider center above base.
- [ ] Run height/orientation/old calibration tests and inspect preview; document prior stale assertions separately.

### Task 3: Verification
- [ ] Fresh independent final review and important-finding regression pass.
- [ ] Build plus full EditMode suite, restore test-only prefab/font side effects; retain user changes.
- [ ] Update ledger with exact results, device limitations, decisions and no push.
