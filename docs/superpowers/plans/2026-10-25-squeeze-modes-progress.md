# SDD ledger — plan: docs/superpowers/plans/2026-10-25-squeeze-modes.md

User approved the plan on 2026-10-25. Execute inline in the user's existing Unity checkout, preserving all unrelated changes; no commits/pushes/resets/stashes/cleans.

Pre-flight: scoring produces per-contact deltas consumed by the shared energy controller; views only read snapshots and request mode changes. Tracker is the sole guided-mode contact aggregator. Free mode retains accepted AnyCapsulePinched cadence.

Ruling: Work in the existing live project, not an isolated stale copy — user requested modifying the current Unity project, whose uncommitted assets and open editor are needed — verify diffs carefully and preserve unrelated changes.

Ruling: Native property-replicated particles do not reproduce manual Emit/GetParticles changes reliably in this project — new tabletop feedback uses bounded native sprite pools and lightweight meshes — this trades some draw calls for deterministic native behavior.

Ruling: Preserve current world-prop/release ownership instead of reviving the archive's old orb reward path — the archive predates the current house/tree/Honey Jar and manual release flows — stages 20/50/80 now add tabletop-only feedback; existing prop thresholds and post-100 gameplay remain owned by their current controllers. Cost if wrong: stage spectacle is subtler than the old archive proposal, without duplicated props.

Ruling: The tabletop +Z axis points toward the viewer, so screen-right is local -X — reverse button X ordering and rotate sprite/text faces accordingly — cost if wrong is reversed ordering on an unusual anchor convention; preview verified free/rhythm/breath left-to-right.

Task 1: implemented pure rhythm/breath/free scoring, tuning asset, optional closure, elapsed assist; initial tests failed on missing implementation, then passed in Unity.
Task 2: implemented explicit held/released/lost contact signals and debounced multi-source tracker; Unity lifecycle tests passed.
Task 3: implemented shared fractional energy, deferred held switching, per-session mood defaults/context, immediate completion and render throttling; mode tests passed.
Task 4: imported 20 visual sprites + 58 composite text sprites with original GUIDs, native sorting, tabletop buttons/text, transparent uncompressed single-sprite imports; resource and mesh tests passed. Setup menu creates editable SqueezeTuning asset.
Task 5: music sample clock, BGM extension correction preserving GUIDs, lightweight pooled rhythm meshes/motes, adaptive feedback and nature tick implemented. BeatOffset remains 0 pending listening/recording calibration.
Task 6: breath tide, mirrored heads, full flash, capped ripples and particles, supplied words/count dots/hints implemented. Four Unity camera-render previews reviewed, including corrected preview teardown between captures.
Task 7: stage feedback is tabletop-only, logs are local JSONL and disabled in EditMode, visit statistics added; original rewards kept.
Task 8: fresh independent reviewer found four issues. Fixed rhythm start-to-start timing, XR-origin fingertip conversion, paused-BGM sample clock, and no new guided contact after completion. Regression tests were observed RED then GREEN. Unity targeted run: /tmp/Moodium-Squeeze-Green.xml = 33/33 pass. Subsequent extra mode-switch/teardown test pending final run.

Full-suite evidence: /tmp/Moodium-Squeeze-Full.xml = 373 passed / 403 total, 30 failures. Failures are outside new Squeeze tests (calibration assumptions, older wrist localization hooks, null-world-ID fixtures, removed post-visit panel expectations, old opening/carousel/prefab assertions and EditMode Destroy/video expectations). Do not claim the whole project test suite passes; those areas were not reworked in this task.

Final: fixed rhythm cadence — RhythmUsesStartToStartIntervalNotTimeSpentExhaling RED→GREEN.
Final: fixed fingertip coordinates — FingertipContactConvertsTrackingCoordinatesToWorld RED (missing helper)→GREEN.
Final: fixed paused audio clock — PausedMusicRetainsSampleClockInsteadOfFallingBack RED→GREEN.
Final: fixed post-completion guided restart — CompletedSessionDoesNotRestartOnHeldHeartbeat RED→GREEN.

Deferred device verification: native fingertip comfort, tabletop visibility against a real table, BGM first-beat offset/latency alignment, sustained performance and actual nine-cycle breath timing. No Vision Pro run claimed. Ambient/tick cues currently reuse a quiet existing step cue plus generated soft air; bespoke sound design can replace these later.

Final targeted verification: Unity 6000.0.30f1 batchmode EditMode, filter ^Squeeze, /tmp/Moodium-Squeeze-Final.xml: **34/34 passed**, zero failures. Runtime + Editor scripts compiled by Unity during that run. Fresh scoped git diff --check passed. Original pre-existing MoodCheck prefab whitespace remains untouched. All new runtime/Editor scripts now have Unity-generated metas.

Finishing gate: full project suite is not green, so no merge/push/branch cleanup was attempted. This task's code and source assets remain in the current checkout for device testing. Extra test now covers real shared-energy preservation across Rhythm/Breath switch and view teardown.
