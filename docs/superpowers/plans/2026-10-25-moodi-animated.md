# Animated Moodi Implementation Plan

> **For agentic workers:** Execute inline with superpowers:executing-plans. Preserve all existing wrist/UI edits. Do not commit or push without request.

**Goal:** Upgrade Assets/prefab/Moodi meshy.prefab in place to the user's five-clip rigged companion, retaining references, appearance and walking hover height.
**Architecture:** Import the supplied FBX and Generic Animator controller, preserve the original inactive static child and prefab GUID. One companion component owns visual animation/turning/touch only; existing controllers retain world position and UI placement. Selection recommendation and walking invite/garden flows request reactions.
**Tech Stack:** Unity 6000.0.30f1, Generic skeletal animation, XR Hands, PolySpatial native rendering.
**Spec:** Moodi-Animated.zip README and source under /tmp/moodi-animated.I8zvic/Moodi-Animated; approved full integration in conversation.

## Global Constraints
- Idle 3 s and Walk .8 s loop; Touched .9 s, Happy 1.4 s, Wave 1.6 s return to Idle.
- Speed >.08 m/s enters Walk, <.04 exits; touch cooldown 1 s.
- Preserve static child (inactive), original materials, prefab GUID and root authored transforms.
- Keep vertical floating; disable duplicate scaling breath. No MoodiEmergence or world-height changes.
- Prevent old mascot PlayableGraph from attaching to the new Animator. No root-motion ownership outside current walk controller.
- High-poly mesh retained per spec; performance and native skinning require device validation.

## Review Focus
- Cold spawn/re-enable/recommendation before first Update must not lose reactions or infer a teleport as walking.
- One-shot triggers must not stack accidentally; touch with occluded hands and XR origin transforms must fail safely.
- Legacy AttachTo must not wrap or hijack new Animator, while old avatars remain supported.
- Re-running setup must preserve generated controller/prefab identities and not produce duplicate active models.
- Rig alignment/bounds/materials and active world-entry instances must remain visually consistent; isolated preview must not dirty unrelated assets.

### Task 1: Import, rig and generated assets
- [x] Import supplied tests first, observe missing-API RED; import FBX/runtime/setup with existing script metas preserved.
- [x] Adapt setup for idempotent controller identity, test 5 named clips/loops, face/up/size and static-child preservation; verify generated assets in Unity.

### Task 2: Runtime and flow integration
- [x] Test yaw, horizontal-only velocity, touch cooldown, pending reaction before first update/re-enable, legacy selection skip and root pose ownership.
- [x] Adapt companion runtime and legacy AttachTo; add recommendation Happy and walking Wave/Happy hooks. Keep GuideHoverPoint unchanged.
- [x] Verify native-compatible materials/renderer, controller transitions and inherited nested prefab references.

### Task 3: Verification and handoff
- [x] Run animated/walking/breathing/selection tests, compile runtime/Editor and render sampled five-clip previews before/after alignment.
- [x] Obtain one independent review, fix important findings with regression tests, record pre-existing failures separately.
- [x] Preserve prior wrist work, no pushes. Device checklist: skinning/frame rate, touch comfort, facing, transitions, no ground drop.
