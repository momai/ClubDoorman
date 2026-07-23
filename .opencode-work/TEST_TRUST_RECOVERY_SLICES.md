# Slices

## Summary

This slice set makes the test count more honest before any production architecture change. It removes executable BDD that does not prove its named behavior, preserves the meaningful `CheckCommand` BDD, moves local classifier checks out of secret-gated E2E setup, isolates one confirmed mutating file-storage fixture, and records an authoritative post-cleanup baseline. The set was checked against branch `test/audit-follow-ups`, HEAD `b5b3164`, a clean worktree, the current test files, and a full run of `514 passed / 3 skipped / 0 failed` on 2026-07-16.

## Assumptions

- False-green feature text has no required documentation consumer outside the executable test project. Deletion requires HITL confirmation before execution.
- `CheckCommand.feature` remains enabled because it executes concrete command code and observes formatted output.
- The current two spam/ham classifier examples are worth retaining as local model checks; the mimicry example is duplicate coverage and does not need migration.
- This slice set may reduce test count. Preserving count is not a goal.

## Slice 1: Lock BDD Disposition

Mode: HITL
Depends on: none
Risk: low

### Goal

Record explicit keep/delete decisions before removing executable scenarios.

### Change

- Add BDD decisions to `.opencode-work/TEST_QUARANTINE_DECISIONS.md`.
- Record `DELETE` for `ModerationFlow`, `StatisticsAndCommands`, `AiChecksPhotoLoggingTest`, `SpamHamCommands`, `AiAnalysis`, `CaptchaSystem`, and `PermissionsAndQuietMode`.
- Record `KEEP` for `CheckCommand` with the evidence that it invokes concrete `CheckCommandHandler`/`CommandRouter` behavior and observes output.
- State that `CaptchaSystem` deletion does not claim captcha timeout coverage; scheduler testability remains a later production slice.

### Files / areas

- `.opencode-work/TEST_QUARANTINE_DECISIONS.md`
- `.opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md`

### Acceptance criteria

- Every current `.feature` file has an explicit disposition and evidence.
- The decision record distinguishes missing coverage from false coverage.
- No source or test behavior changes in this slice.

### Verification

- `git diff --check -- .opencode-work/TEST_QUARANTINE_DECISIONS.md .opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md` — no whitespace errors.
- `git diff -- .opencode-work/TEST_QUARANTINE_DECISIONS.md .opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md` — decisions match the inspected feature/binding evidence.

### Non-goals

- Do not delete tests in this slice.
- Do not design replacement BDD infrastructure.
- Do not claim that all behavior named by deleted features is covered elsewhere.

### Risks

- Deletion is a destructive follow-up. Stop after the decision record unless the user approves Slices 2 and 3.

---

## Slice 2: Remove Obvious False-Green BDD

Mode: HITL
Depends on: Slice 1
Risk: medium

### Goal

Remove four executable feature groups whose bindings are empty, tautological, or only assert harness state.

### Change

- Delete `ModerationFlow.feature` and its generated fixture.
- Delete `StatisticsAndCommands.feature` and its generated fixture.
- Delete `AiChecksPhotoLoggingTest.feature` and its generated fixture.
- Delete `SpamHamCommands.feature` and its generated fixture.
- Delete dedicated binding files that have no remaining feature users: `ModerationFlowSteps.cs`, `StatisticsAndCommandsSteps.cs`, `StatisticsSteps.cs`, `AiChecksPhotoLoggingSteps.cs`, and `SpamHamCommandSteps.cs`.
- Keep `CheckCommand.feature`, `CheckCommandSteps.cs`, and shared command bindings used by it.

### Files / areas

- `ClubDoorman.Test/Features/ModerationFlow.feature`
- `ClubDoorman.Test/Features/ModerationFlow.feature.cs`
- `ClubDoorman.Test/Features/StatisticsAndCommands.feature`
- `ClubDoorman.Test/Features/StatisticsAndCommands.feature.cs`
- `ClubDoorman.Test/Features/AiChecksPhotoLoggingTest.feature`
- `ClubDoorman.Test/Features/AiChecksPhotoLoggingTest.feature.cs`
- `ClubDoorman.Test/Features/SpamHamCommands.feature`
- `ClubDoorman.Test/Features/SpamHamCommands.feature.cs`
- `ClubDoorman.Test/StepDefinitions/Common/ModerationFlowSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/StatisticsAndCommandsSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/StatisticsSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/AiChecksPhotoLoggingSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/SpamHamCommandSteps.cs`

### Acceptance criteria

- The four feature groups are no longer discovered as tests.
- The project compiles without missing SpecFlow bindings or generated fixture references.
- Existing AI notification, statistics, command router, and retained `CheckCommand` tests still pass.
- No production code changes.

### Verification

- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~AiNotificationDeliveryTests|FullyQualifiedName~MessageHandlerStatsCommandTests|FullyQualifiedName~GlobalStatsManagerTests|FullyQualifiedName~CommandRouterTests|FullyQualifiedName~CheckCommandFeature"` — all retained/replacement tests pass.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal` — full project passes after the deletion batch.
- `git diff --check -- ClubDoorman.Test/Features ClubDoorman.Test/StepDefinitions/Common` — no whitespace errors.

### Non-goals

- Do not add command-handler tests merely to preserve removed test count.
- Do not edit `TestInfrastructure` or `TestKit`.
- Do not change `CheckCommand` setup in this slice.

### Risks

- Shared English step phrases can hide a remaining feature dependency. Mitigation: build and run retained `CheckCommandFeature` plus the full project before completing the slice.

---

## Slice 3: Remove Simulated AI Captcha Permissions BDD

Mode: HITL
Depends on: Slice 2
Risk: medium

### Goal

Remove the remaining three false-green feature groups that synthesize AI, captcha, and permission outcomes instead of executing the owning seams.

### Change

- Delete `AiAnalysis.feature`, `CaptchaSystem.feature`, and `PermissionsAndQuietMode.feature` with generated fixtures.
- Delete `AiAnalysisSteps.cs`, `CaptchaSteps.cs`, `PermissionsSteps.cs`, and `UserManagementSteps.cs` after their feature users are removed.
- Keep the committed focused AI/pipeline/captcha tests.
- Keep `CheckCommand.feature` as the only enabled BDD feature in this plan.

### Files / areas

- `ClubDoorman.Test/Features/AiAnalysis.feature`
- `ClubDoorman.Test/Features/AiAnalysis.feature.cs`
- `ClubDoorman.Test/Features/CaptchaSystem.feature`
- `ClubDoorman.Test/Features/CaptchaSystem.feature.cs`
- `ClubDoorman.Test/Features/PermissionsAndQuietMode.feature`
- `ClubDoorman.Test/Features/PermissionsAndQuietMode.feature.cs`
- `ClubDoorman.Test/StepDefinitions/Common/AiAnalysisSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/CaptchaSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/PermissionsSteps.cs`
- `ClubDoorman.Test/StepDefinitions/Common/UserManagementSteps.cs`

### Acceptance criteria

- Simulated BDD no longer contributes green tests for AI profile actions, captcha timeout/completion, or permissions.
- `AiProfileAnalysisStepTests`, `AiNotificationDeliveryTests`, `CaptchaPendingStepTests`, and captcha service tests pass.
- `CheckCommandFeature` still passes without unrelated `BeforeScenario` setup from deleted bindings.
- The test run no longer logs `CaptchaSteps` setup for `CheckCommand` scenarios.
- No production behavior changes.

### Verification

- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~AiProfileAnalysisStepTests|FullyQualifiedName~AiNotificationDeliveryTests"` — focused AI seams pass.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~CaptchaPendingStepTests|FullyQualifiedName~CaptchaServiceFakeTests|FullyQualifiedName~CaptchaServiceExtendedTests"` — focused captcha seams pass.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~CheckCommandFeature"` — retained BDD passes.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal` — full project passes.
- `git diff --check -- ClubDoorman.Test/Features ClubDoorman.Test/StepDefinitions/Common` — no whitespace errors.

### Non-goals

- Do not implement captcha timeout scheduling tests here.
- Do not change captcha production timers or callbacks.
- Do not change AI provider construction, moderation policy, or permissions behavior.
- Do not rewrite `CheckCommand` BDD.

### Risks

- The deleted Gherkin names behavior not currently covered. This is intentional removal of false confidence, not a coverage-complete migration. Missing valuable contracts should become separate seam-test slices only after an owner is identified.

---

## Slice 4: Move Secret-Free Classifier Checks

Mode: HITL
Depends on: Slice 3
Risk: medium

### Goal

Run the two local spam/ham model checks without `.env`, Telegram credentials, OpenRouter configuration, or an infinite retraining loop.

### Change

- Add `ClubDoorman.Test/Unit/Services/SpamHamClassifierTests.cs` with the current spam and ham examples from `SimpleE2ETests`.
- Construct `SpamHamClassifier` with a local `Mock<IAppConfig>` returning `GoldenBaselineMode = true`.
- Use a local `NullLogger<SpamHamClassifier>` or focused logger mock; do not add shared factory support.
- Delete `SimpleE2ETests.cs` after migrating the two unique classifier examples.
- Do not migrate `E2E_MimicryClassifier_ShouldDetectMimicry`; `MimicryClassifierTests.AnalyzeMessages_TemplatePhrases_ReturnsHighScore` already covers the behavior directly.

### Files / areas

- `ClubDoorman.Test/Integration/SimpleE2ETests.cs`
- `ClubDoorman.Test/Unit/Services/SpamHamClassifierTests.cs`
- `ClubDoorman.Test/Unit/Services/MimicryClassifierTests.cs` — inspect only; no change expected.

### Acceptance criteria

- Spam and ham examples execute without reading `.env` or requiring `DOORMAN_OPENROUTER_API`, `DOORMAN_BOT_API`, or `DOORMAN_ADMIN_CHAT`.
- No test in this slice constructs `AiChecks` or `FakeTelegramClient`.
- The classifier's retraining loop is disabled through `GoldenBaselineMode = true`.
- The three `SimpleE2ETests` skips disappear; actual final counts are measured later rather than predicted.

### Verification

- `env -u DOORMAN_OPENROUTER_API -u DOORMAN_BOT_API -u DOORMAN_ADMIN_CHAT dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~SpamHamClassifierTests|FullyQualifiedName~MimicryClassifierTests"` — focused classifiers pass without secrets.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal` — full project passes and no `SimpleE2ETests` skip remains.
- `git diff --check -- ClubDoorman.Test/Integration/SimpleE2ETests.cs ClubDoorman.Test/Unit/Services/SpamHamClassifierTests.cs` — no whitespace errors.

### Non-goals

- Do not change `SpamHamClassifier` production lifecycle.
- Do not make model training synchronous.
- Do not add AI HTTP contract tests.
- Do not edit `AppConfigTestFactory` or shared test infrastructure.

### Risks

- Model initialization remains asynchronous and may take up to the existing prediction wait timeout. Mitigation: retain the production path in this slice and record runtime; lifecycle correction belongs to a later production plan.

---

## Slice 5: Isolate User Cleanup Storage

Mode: AFK
Depends on: Slice 3
Risk: low

### Goal

Prevent `UserCleanupServiceTests` from reading or mutating runner-relative approval files.

### Change

- Mark `UserCleanupServiceTests` non-parallel because it temporarily changes a process environment variable.
- Before constructing `ApprovedUsersStorage`, save the exact previous `DOORMAN_DATA_ROOT`, create a unique temporary directory, and set the variable to that directory.
- In teardown, restore the previous value exactly and recursively delete the temporary directory.
- Keep setup local to the test file; do not modify `UserCleanupServiceTestFactory`, `ApprovedUsersStorageTestFactory`, or other shared infrastructure.
- Document that other direct `ApprovedUsersStorage` constructors remain separate follow-ups.

### Files / areas

- `ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs`
- `.opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md`

### Acceptance criteria

- Every test in the fixture starts with empty approval storage.
- Test writes occur only under a unique temporary directory.
- `DOORMAN_DATA_ROOT` is restored even when a test fails.
- The focused fixture passes on two consecutive runs.
- Shared test factories are unchanged.

### Verification

- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~UserCleanupServiceTests"` — first focused run passes.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~UserCleanupServiceTests"` — second focused run passes without stale state.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~ApprovedUsersStorage|FullyQualifiedName~UserCleanupService"` — broader storage/cleanup tests pass.
- `git diff --check -- ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs .opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md` — no whitespace errors.

### Non-goals

- Do not add a production filesystem abstraction.
- Do not change `ApprovedUsersStorage` constructor or persistence format.
- Do not edit `ClubDoorman.Test/TestInfrastructure`.
- Do not claim all storage tests are isolated after this slice.

---

## Slice 6: Record Authoritative Baseline

Mode: AFK
Depends on: Slice 4, Slice 5
Risk: low

### Goal

Record one authoritative post-cleanup baseline and the exact meaning of remaining skips.

### Change

- Run the full test project at the final slice HEAD without `.runsettings`.
- Record branch, short HEAD, exact command, pass/fail/skip/total counts, duration, and skip reasons in `.opencode-work/WORKLOG.md`.
- Update `.opencode-work/NEXT.md` so its current baseline and next decision do not point to the stale `900 / 12` state.
- Add a dated post-slice baseline note to `.opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md`; retain the original review baseline as historical evidence.
- Record remaining non-hermetic paths, especially `Integration/AiAnalysisTests.cs` and direct storage construction in legacy factories.

### Files / areas

- `.opencode-work/WORKLOG.md`
- `.opencode-work/NEXT.md`
- `.opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md`

### Acceptance criteria

- One current baseline is clearly labelled authoritative for the final HEAD.
- Historical baselines remain identifiable as historical, not current.
- Every remaining skip has an explicit source and reason.
- The record states that `.runsettings` was not supplied.
- Remaining known testability risks are not marked resolved unless the corresponding code changed.

### Verification

- `git branch --show-current && git rev-parse --short HEAD` — branch and HEAD captured in the record.
- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal` — full project passes; exact result copied without inference.
- `git diff --check -- .opencode-work/WORKLOG.md .opencode-work/NEXT.md .opencode-work/TEST_BASE_AND_TESTABILITY_REVIEW.md` — no whitespace errors.

### Non-goals

- Do not normalize or rewrite the full historical worklog.
- Do not change test filters or `.runsettings` in this slice.
- Do not begin production architecture work.

## Final check

- `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal` — zero failed tests at final HEAD.
- `git diff --check` — no whitespace errors across the completed slice set.
- `git status --short` — only intended slice files are changed before commit, or clean after explicitly requested commits.
- Inspect test output — no deleted false-green features are discovered, no `SimpleE2ETests` skips remain, and retained `CheckCommandFeature` executes.

## Not included

- Pipeline return-contract or `FinalModerationActionStep` behavior changes.
- Moderation policy decision/effect separation.
- Captcha expiry scheduler ownership or controllable time.
- `SpamHamClassifier` constructor/background-service refactor.
- Global `MemoryCache.Default` migration.
- `MessageHandlerTestFactory`, `FakeServicesFactory`, `MessageHandlerBuilder`, or AutoFixture redesign.
- Full `ApprovedUsersStorage` isolation across legacy factories and `AiAnalysisTests`.
- Package vulnerability/version remediation.
- `CheckCommand` BDD migration.

## Open questions

- Slices 2-4 delete executable test files. Each requires explicit approval immediately before execution, even though deletion is the recommended disposition.
