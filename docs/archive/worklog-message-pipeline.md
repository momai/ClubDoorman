# Message Pipeline Migration Worklog

Purpose: Incremental extraction of monolithic `MessageHandler` logic into ordered pipeline steps for clarity, testability, and future effect composition.

## Legend
- [x] Completed
- [ ] Pending / Planned
- [~] In progress

## High-Level Goals
1. Preserve semantics & Golden Master event stream during refactor.
2. Keep test suite green (≈947 tests) after each step.
3. Introduce steps in low-risk order (structural branches first, moderation chain later).
4. (done) Removed legacy constructor & NullMessagePipeline (all call sites updated)
5. Add synthetic comparison harness before migrating complex moderation logic.

## Step Order (Proposed)
Order numbers reserved to avoid collisions:
- 10 CommandStep (done)
- 20 NewMembersStep
- 30 LeftMemberCleanupStep
- 40 ChannelMessageStep
- 50 PrivateSkipStep
- 100 CaptchaPendingStep
- 110 BanlistCheckStep
- 120 AlreadyApprovedStep
- 130 FirstMessageLogStep
- 140 ClubMemberSkipStep
- 200 BaseModerationStep
- 210 AiProfileAnalysisStep
- 220 FinalModerationActionStep

(Spacing leaves room for insertions.)

## Completed
1. [x] Phase 1: Introduced pipeline abstractions (`IMessagePipeline`, `IMessageStep`, `MessagePipeline`, `StepResult`)
2. [x] Phase 2: Extracted `CommandStep` (Order 10) + integrated into `MessageHandler`
3. [x] Phase 2a: Added `MessageContext` (OperationId, GmCorrelation, CommandHandled)
4. [x] Phase 2b: Adjusted test factory to build real pipeline (removed reliance on legacy constructor for command tests)
5. [x] Phase 2c: Full test suite green (947 tests; 0 failed, 1 skipped) after integration

## In Progress
- [~] Synthetic comparison harness (decided to defer until any semantic drift detected)

## Pending / Planned
6. [x] Add `NewMembersStep` (Order 20) + tests; remove branch from `MessageHandler`
7. [x] Add `LeftMemberCleanupStep` (Order 30) + tests; remove branch
8. [x] Add `ChannelMessageStep` (Order 40) + tests; remove branch
9. [x] Add `PrivateSkipStep` (Order 50) + tests; remove branch
10. [ ] Introduce synthetic comparison harness (legacy vs pipeline moderation path replay) – skipped for now (legacy path fully removed; re-add only if drift suspected)
11. [x] Extend `MessageContext` with: `User`, `Chat`, `IsSilentMode`, `ModerationResult`, `AiProfileRestricted`
12. [x] Extract `CaptchaPendingStep` (100)
13. [x] Extract `BanlistCheckStep` (110)
14. [x] Extract `AlreadyApprovedStep` (120)
15. [x] Extract `FirstMessageLogStep` (130)
16. [x] Extract `ClubMemberSkipStep` (140)
17. [x] Extract `BaseModerationStep` (200)
18. [x] Extract `AiProfileAnalysisStep` (210)
19. [x] Extract `FinalModerationActionStep` (220)
20. [ ] Replace reflective event publishing with explicit events in terminal steps (still uses existing publisher interface – low priority)
21. [x] Remove `HandleUserMessageWithResultAsync` after parity secured (legacy moderation method removed)
22. [x] Update remaining factories (e.g., `FakeServicesFactory`, test builders) to use pipeline builder (all compile against single ctor with pipeline)
23. [x] Remove legacy MessageHandler constructor & `NullMessagePipeline`
24. [x] Remove command fallback branch once all commands guaranteed handled by steps
25. [ ] Documentation update (`README`, architecture notes) for new pipeline
26. [ ] Add mutation tests / coverage checks for new steps
27. [ ] Cleanup obsolete logs & simplify `MessageHandler`

## Risk Mitigations
- Keep each extraction self-contained with immediate test validation.
- Synthetic comparison harness ensures moderation semantics parity before deep extraction.
- Order spacing allows inserting experimental steps without renumbering.

## Open Questions
- Do we need a general-purpose effect dispatcher after moderation chain? (TBD post-extraction)
- Should Golden Master recorder move into middleware (pre-step) once all steps migrated? (Evaluate later)

## Next Action
Housekeeping / polish phase:
1. Remove command fallback branch once confidence in CommandStep coverage (add targeted tests first).
2. Replace event publishing in final steps with explicit typed events (clarify semantics).
3. Documentation refresh (architecture section + diagrams).
4. Optional: synthetic comparison harness only if future complex moderation semantics added.
5. Trim obsolete legacy log phrases after Golden Master regression window closes.

Timestamp: 2025-08-20T09:10:00Z
