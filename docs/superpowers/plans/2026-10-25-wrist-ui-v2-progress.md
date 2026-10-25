# SDD ledger — plan: docs/superpowers/plans/2026-10-25-wrist-ui-v2.md

User approved the archive design plus raised-wrist/head-direction adaptation. Execute inline in the current project; user just backed it up at b309406. Only existing unrelated dirty file is UniversalRenderPipelineGlobalSettings.asset plus test XML outputs; preserve these.

Pre-flight: manager API signatures remain unchanged. RewardSequence currently owns tabletop-only stage feedback, not legacy orbs. New sockets are extension points only. Native spatial buttons use the existing global interaction controller.

Ruling: Adapt supplied component rather than blindly replacing current runtime integrations — package targets older main and explicitly was never compiled — preserve release/exit events and prepare socket APIs without new reward flows. Cost if wrong: old orb animations are not part of this visual change.

Ruling: Stay in user's current checkout to update their open Unity project, preserving unrelated edits — requested current-project work and backed-up baseline — cost is requiring careful scoped diffs rather than isolated branch integration.

Task 1: implemented. Imported 17 PNGs and SVG sources plus scoped postprocessor; preserved existing script GUID. Supplied helper tests first failed compilation against absent APIs (logged /tmp/mood-band-red.log), then runtime/editor compilation passed. Adapted missing Flow namespace.

Task 2: implemented. Raised wrist/head cone/dorsal gate, input shutdown on lost pose, double-press exit, idempotent availability, stage text and progress resets covered. First Unity wrist run 20/20 passed including formerly failing Chinese localization tests. Added release layout regression, observed 20/21 with label max x86 overlapping knob min x20. Fix reserves icon/text/knob lanes and adds English/Chinese × On/Off coverage, pending final run.

Ruling: The approved raised-wrist behavior needs broader head-cone defaults than the original package's gaze approximation — 45°/55° plus wrist height and dorsal gates avoid precision head aiming — device comfort may require adjusting inspector defaults.

Ruling: Current UI reports real energy stages until an actual external gem collection is supplied — do not show “gifts all collected” solely for reaching energy thresholds — cost: subtitle differs from the reference's legacy reward assumption, while GetGemSocket/SetCollectedGems/ShowToast remain available.

Final review: fresh reviewer identified one important release-label/knob overlap; covered by failing test then fixed in one pass. No other important lifecycle issues reported.
Final: minor (deferred): supplied mesh builder uses temporary lists on progress-triggered rebuilds; already gated by .002 progress change. Profile on Vision Pro before adding buffer complexity.

Task 3: complete for editor verification. Wrist suite 25/25 passed; full suite 399/425 passed, 26 non-wrist failures listed in wrist-ui-v2-verification.md. Rendered 10 isolated Chinese/English state previews, inspected and fixed English title wrapping (observed 2 lines RED → 1 line GREEN). All important reviewer findings covered; no device run claimed.

Test-generated MoodCheck prefab and ProjectSettings side effects were restored because these files were clean at task start. Preserved unrelated pre-existing URP edit. Retained font glyph additions for new wrist text. Scoped diff --check passed. No commit or GitHub push performed.

Remaining work is device validation only: dorsal pose, fit, comfort thresholds, native hover/transparent sorting and profiling. Do not characterize the full project suite as green.
