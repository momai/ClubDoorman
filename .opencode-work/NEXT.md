# Next Decision

## Question

Which demonstrated production testability risk should be planned after test-trust recovery?

## Current Answer

Choose one production seam before implementation: pipeline failure observability or captcha scheduler ownership. Do not combine them and do not add a shared test harness.

## Why

- The authoritative 2026-07-18 worktree baseline is `493 passed / 0 skipped / 0 failed / 493 total` in `9 s`.
- Command: `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal`; no `.runsettings` was supplied.
- Branch: `test/audit-follow-ups`; base HEAD: `b5b3164`; the measured Slice 1-6 changes are uncommitted.
- False-green BDD was removed, local classifier checks no longer require secrets, and `UserCleanupServiceTests` storage is isolated.
- Pipeline failure and captcha expiry remain separate production behavior changes requiring focused characterization and approval.
- A shared harness still risks adding another construction path rather than fixing an owning seam.

## Candidate Next Slice

Title: Select and characterize one production testability seam.

Mode: HITL before production behavior changes.

Goal: produce a bounded plan for either observable pipeline failure or single-owner captcha expiry, not both.

Allowed:

- Inspect the owning production seam and nearest focused tests.
- Characterize current returned results and side effects before proposing changes.
- Keep one behavior group per implementation slice.
- Preserve current persisted formats and external bot behavior unless explicitly approved otherwise.

Not allowed:

- Combine pipeline and captcha work.
- Add shared test infrastructure.
- Begin broad moderation architecture changes.
- Treat remaining AI, storage, cache, timer, or package risks as resolved without corresponding code changes.

Suggested verification:

- Exact focused seam test selected by the approved plan.
- Full suite after any production behavior change, per repository rules.

## Alternative Later Slice

Continue isolating direct `ApprovedUsersStorage` constructions, starting with `Integration/AiAnalysisTests.cs`, as a separate test-only plan.
