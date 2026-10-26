# SDD ledger — plan: docs/superpowers/plans/2026-10-26-world-guide-integration.md

Spec read in full from original README/CONVENTIONS/tokens and patch CHANGES. Source archives copied to a dedicated temporary location to survive WeChat eviction.

Ruling: Continue in the user's live checkout using their established Native workflow — Unity is open here and requested changes must be immediately testable — unrelated edits remain untouched.

Ruling: Keep walking invitation at 3s; explore card at 2.5s is not allowed to silently delay walking — explicit user timing wins — visual overlap remains possible and will be reported.

Pre-flight: Manager consumes ShowTutorial/ShowExplore/Hide; interaction controller consumes swipe handle. Original source lacked cancellation semantics, allowed tutorial to be replaced mid-read, and destroyed shared texture assets; adapt these lifecycle boundaries before integration.

Task 1: complete. Orientation tests reproduced three failures (flat sticker yaw90, tilted sticker, hand world-axis mismatch); video preserves anchor-local forward and normal, hand guide root follows the tracked frame while preserving authored child pose. Regression passed; video mute tests remain green.

Task 2: complete. Missing Guide namespace RED observed before runtime import. All 34 original PNGs installed; importer generated correct sprite metadata; patched design/tool files preserved. Manager now shows entry tutorial, schedules once-per-session explore at 100%, and immediately hides/cancels on exit. Native and Editor mouse swipe routes implemented with cancellation semantics. No gameplay pause and no walking timer changes beyond the existing user change.

Ruling: If energy reaches 100% before the user finishes reading, wait for tutorial dismissal rather than replace it — avoids silently dropping tutorial pages — exploration may appear later than 2.5s in this case.

Task 3 complete. Rendered all 20 world/language/page combinations and inspected Candy Chinese tutorial, Nature English final page, and both exploration layouts; original rounded colored button PNGs remain unchanged. Targeted cases pass in the final full run; runtime/editor compilation 0 errors. Final full suite 430/456 passed with the same 26 baseline failures. Details in 2026-10-26-world-guide-verification.md.

Final review found one P2 asset-lifetime issue. Shared sprite leases now count owners, detach renderers and unload imported sprite/texture assets only at the final release. Two-instance preservation and final-lease cleanup tests pass. Original native-texture-null assertion failed even after UnloadAsset in Editor (its importer retains native objects); contract test checks ownership cleanup instead. Actual native memory reclamation is a device profiling check, not claimed by this test.

Coverage limitations: manager full entry→energy→exit path and real native spatial input are code-reviewed but not end-to-end device-tested. No other actionable review findings, no deferred minors.

Final cleanup restored generated MoodCheck prefabs, test-added dynamic Inter font atlas entries and a serializer-only scene field reorder. Existing three-second walking edits and source archive patch preserved. No commits or pushes.
