# Animated Moodi verification — 2026-10-25

## Delivered

- Upgraded `Assets/prefab/Moodi meshy.prefab` in place. GUID `1c1c0c337d80f495e8342b47fb803074` and authored root/entry transforms retained. Original `moodi` static child remains inactive as backup; active `moodi_rigged` uses original materials.
- Supplied Generic FBX: Idle (3s) and Walk (.8s) loop; Touched (.9s), Happy (1.4s), Wave (1.6s) return to Idle. No root motion. Existing vertical float .01 retained, duplicate scale breathing disabled.
- First appearance waves; horizontal movement controls walking/facing; XR hand contact reacts with 1s cooldown. Reaction triggers are mutually exclusive. First-placement teleports do not trigger walking.
- Selection recommendation requests Happy. Walking invite requests Wave; garden discovery requests Happy. Companion root remains controlled by existing flow, including eye-height-minus-.45m walking hover. Legacy selection graph is rejected before it can hijack the new Animator.
- No change to MoodiEmergence, old authored world-entry positions, or unrelated wrist work. No commit/push.

## Evidence

- Runtime/Editor `dotnet build Assembly-CSharp-Editor.csproj` with temporary include targets: 0 errors, 2 existing warnings; `/tmp/moodi-animated-delivery-build.log`.
- New animated EditMode cases: **12/12 passed**. Covers identity/static backup/loop flags, legacy skip, menu signatures, GUID-preserving controller rebuild on disposable copy, cold placement, mutually exclusive reactions and evaluated return to Idle, touch cooldown, visual turning without root pose changes, yaw helpers.
- Targeted related suite: **31/33 passed**. Remaining old breathing layout assertions expect Candy (0,.02,-.11) / Nature (.06,.02,-.09), while current authored positions are (0,.01,-.02) / (.06,.02,-.06). Current layout intentionally preserved.
- Final full EditMode run: **411/437 passed, 26 failed**, 2026-10-25 09:40:31–09:42:39 UTC. All 12 new cases pass; the same 26 failures are listed in the prior wrist verification document, with no new failing test names. Raw report: `/var/folders/r0/92tsbtv569lfgkrwytf1z9rr0000gn/T/Moodium-Wrist-AllTests.xml`.
- Rendered and visually inspected original static + Idle/Walk/Touched/Happy/Wave, via `Moodium > Moodi > Preview Animated Moodi`. Images are `Moodi-Animated-<state>.png` in the system temporary directory. Appearance, front direction and scale agree; clips visibly deform the skin. These are isolated Unity renders, not device captures.
- One independent review: fixed queued competing reaction triggers. Regression observed failing pending-Wave assertion before fix, passing after fix.
- Controller rebuild findings fixed: set parameters atomically, mark reused state/transition sub-assets dirty, preserve controller GUID. Rebuild tests now use a uniquely named disposable copy, avoiding production asset mutation.
- Full-run generated MoodCheck prefab and PlayerSettings side effects restored to their clean baseline; existing wrist/font/URP edits preserved. Removed only temporary test controller copies through AssetDatabase cleanup.

## Device checks still required

1. Native visionOS skinning/frame rate with the supplied high-poly mesh (~83k vertices, ~130k triangles, 13 bones). No unapproved decimation was applied.
2. Real hand contact volume and 1s touch cooldown comfort; XR origin alignment.
3. First-appearance/recommendation priority, reaction blending, walk-facing and stopping while following the user.
4. Selection UI alignment and walking hover height in headset; original static backup stays hidden.

Editor tests and renders cannot establish native rendering/performance on Vision Pro. No additional assets are missing for this integration.
