# SDD ledger — plan: docs/superpowers/plans/2026-10-26-calibration-height.md

Ruling: Continue Native implementation in the existing live Unity checkout — established user workflow and pending changes must be retained — scoped diffs replace isolated branch protection.

Pre-flight: plane offset lives in SensoryBagCalibrationData, consumed by frame and presentation; height setter must preserve width/height and center. Existing frame fallback treated explicit zero as missing; remove it. Current visual pivot is not aligned to mesh minimum, so moving only the UI would not solve physical fit.

Ruling: Keep existing proportional mesh-thickness scaling rather than force 5cm thickness — user approved moving the base plane, not distorting models — actual modeled top may still differ from physical bag thickness.

Ruling: Limit normal offset to -25cm..+10cm — covers typical bags and modest correction while preventing accidental large drags — unusual bags outside this range would require adjusting limits.

Tasks 1–3 complete; no commit or push requested.

Task 1: implemented. All six original new feature cases initially failed as expected (size, default offset, missing height API/control, height drag, actual geometry bottom); size/height/fine controls/reset/confirm/loss cases now pass. Updated the prior anchor-movement test's expected base from zero to -5cm, as explicitly requested by the new design.

Task 2: implemented. Renderer-local bounds replace inverse-world-AABB calculation, preserving tilted anchor proportions and positioning scaled geometry's bottom on the base; collider occupies the configured depth above it. Hand guide uses calibrated footprint center instead of the now-corrected off-center model pivot.

Ruling: Calibrated SqueezeTableView must stop subtracting bag depth — its origin now already is the table, so the old subtraction would sink all mode buttons under it — put calibrated table UI 2mm above base while preserving legacy top-sticker behavior. New regression observed -5cm instead of +2mm before the fix.

Task 3: complete. Actual CandySoft/NatureSoft asset fit cases pass. Calibration subset 45/48 passed: all 10 new cases pass; three old failures are the two proportional-Y-scale assertions and legacy XY-versus-XZ projection assertion. Fresh independent reviewer found no important/blocking issue.

Final: minor (deferred): Exactly top-down Editor mouse height drag cannot move along the sticker normal with a camera-facing drag plane; use the ±0.5cm buttons. Native 3D drag is unaffected.

Visual verification: rendered Chinese and English CalibrationHeight previews, inspected both. Enlarged action labels from 7 to 18 and updated the old explicit-size test after observing its failure. No overlapping labels in preview.

Final verification: Unity full EditMode run 2026-10-26 06:50:13Z–06:51:27Z: 466 total, 440 passed, 26 failed, matching the prior 26-failure baseline. All new calibration tests passed. dotnet build Assembly-CSharp-Editor.csproj: 35 warnings, 0 errors. git diff --check clean after restoring only test-generated MoodCheck prefabs and dynamic font assets. Retained scene's intentional .40/.25 defaults and all preceding WorldGuide/walk edits. Device fit/pinch usability still requires Vision Pro testing; no physical thickness warping introduced.
