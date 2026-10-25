# SDD ledger — plan: docs/superpowers/plans/2026-10-25-moodi-animated.md

User confirmed full archive integration. Work inline in the current backed-up project. Existing wrist UI, font glyphs, URP settings and its docs/assets remain user-owned unrelated changes.

Pre-flight: supplied runtime must not own the walk guide's root pose. New skeleton clips replace scale-breathing only. Legacy AttachTo currently creates a motion wrapper and PlayableGraph immediately, before new component Start; prevent attachment at source rather than relying solely on later disable/Rebind.

Ruling: Use current live checkout, preserving all dirty wrist edits — requested in-place prefab upgrade and open Unity project — careful scoped diffs are required instead of isolated branch integration.

Ruling: Prevent legacy selection attachment before it creates a wrapper/graph — avoids transient hijacking of the new Animator and lost first-frame reactions — old non-companion models continue through the original path.

Tasks 1–2 complete. Supplied rig imported as Generic with five named clips, original materials, no root motion, preserved prefab GUID/static backup, and scale breathing disabled while vertical floating remains. Selection recommendation requests Happy; walking invite/garden request Wave/Happy. Existing guide eye-height-minus-.45m rule unchanged.

Task 3: compiled successfully; six isolated renders checked (original static plus all five actions), matching appearance, front direction and scale. Targeted suite 31/33 passed; all 12 new animation cases pass. Two old breathing tests demand outdated world-entry local positions; current authored positions are deliberately preserved.

Independent final review found competing reaction triggers. Added failing pending-Wave assertion, then cleared competing triggers in Touched/Wave; regression now passes. No other actionable findings.

Additional regression: controller rebuild originally temporarily removed parameters, and reused state sub-assets were not all marked dirty. Set parameters atomically and explicitly persist states/transitions/state machine. Controller-identity test now rebuilds a uniquely named disposable copy, never the production controller. Evaluated Touched now returns to Idle; prefab/controller identities remain stable.

Full project regression and cleanup recorded in the verification document. No commit/push. Vision Pro performance, hand-touch comfort and actual native-rendered animation transitions remain device checks.
