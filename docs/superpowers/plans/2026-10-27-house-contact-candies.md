# House contact candy bursts

User approved the short design in chat and requested execution. Continue Native in the live Unity checkout, preserving pending voice/walking/material edits; no commit/push.

Goal: CandyHouse, CandyLittleHouse and Honey Jar keep their deformation/audio, but emit the existing random CandySoft candy prefabs from actual hand surface contacts.

Architecture: CompletionHouseSoftInteraction raises a contact-origin event while it has real contact, rate-limited to .8 seconds per prop. CandyCompletionHouseController connects only its three staged props to CandyBurstController. A contact-specific burst request uses the Candy pool and existing CandyProjectile pipeline, with a32 combined live/reserved admission cap. CandySoft's existing Burst method and Nature behavior stay unchanged.

Tasks:
- [ ] RED contact tests: actual raycast surface, rotated/scaled prop, holding/releasing/no-contact, real three asset bindings.
- [ ] Wire contact event and bounded contact burst admission; preserve existing source collision ignore/video avoidance/projectile lifetime.
- [ ] Run focused and full Unity tests, build, independent final review; restore only generated test side effects.

Review focus: source/receiver disabled or destroyed; simultaneous fingers/props and coroutine reservations; no candy on near miss; normals under transformed props; shared world stop cleanup.
