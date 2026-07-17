# Test Base And Testability Review

Date: 2026-07-16

Status: review record and planning input. This is not an approved implementation plan.

## Purpose

Preserve the findings from the test-base and production-testability review so later cleanup does not lose evidence or collapse into a generic "rewrite the architecture" task.

This document complements:

- `.opencode-work/TEST_CLEANUP_STRATEGY.md` — value-based test cleanup direction.
- `.opencode-work/TEST_QUARANTINE_DECISIONS.md` — decisions already made for specific tests.
- `docs/test-infra-strategy.md` — repository rules for choosing the nearest production seam.
- `docs/architecture-seams.md` — intended production boundaries.

Some older worklog baselines describe earlier repository states with more tests. The current baseline measured during this review is recorded below.

## Review Scope

Primary scope:

- `ClubDoorman.Test` construction paths, assertions, categories, isolation and reliability.
- Production seams that force broad or fragile test setup.
- Hidden network, filesystem, process-environment, cache, timer and background-task dependencies.
- Cases where green tests do not prove the behavior named by the test.

Out of scope:

- A broad production rewrite.
- Preserving test count as a goal.
- Enabling real Telegram or AI calls in normal CI.
- Designing a new general-purpose test harness.
- Changing production or test behavior during this review.

## Method And Evidence Level

The review used:

- Static inspection of production and test code.
- Comparison with `docs/architecture-seams.md` and `docs/test-infra-strategy.md`.
- Searches for handler construction, global cache use, real storage construction, weak assertions, sleeps, environment access and disabled tests.
- A full test-project run.

Evidence labels used below:

- **Fact** — directly observed in the current code or test output.
- **Risk** — a concrete failure mode enabled by the fact.
- **Hypothesis** — plausible impact that still needs a focused reproduction.

Line references describe the worktree as inspected on 2026-07-16. Existing dirty and untracked files were not reverted or overwritten.

## Historical Review Baseline

Command:

```bash
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal
```

Result:

```text
0 failed, 514 passed, 3 skipped, 517 total, duration 9 s
```

Important observations from the same run:

- Empty `ModerationFlow` BDD steps were reported as `done` and their scenarios passed.
- Test output repeatedly said that OpenRouter API was configured. The run did not prove that a real request occurred, but normal tests can construct an AI-enabled configuration.
- `SpamHamClassifier` training and retraining loops started during the run.
- Three local classifier tests were skipped because `SimpleE2ETests` requires unrelated environment secrets.
- `ApprovedUsersStorage` used `ClubDoorman.Test/bin/Debug/net9.0/data` in this run. The exact fallback location depends on the runner working directory.

Build warnings observed:

- `NU1603`: requested `DotNetEnv 2.6.0` was unavailable; `3.0.0` was resolved.
- `NU1603`: requested `Microsoft.Extensions.Caching.Hybrid 8.0.0` was unavailable; `9.3.0` was resolved.
- `NU1902`: `SixLabors.ImageSharp 3.1.9` has known moderate-severity vulnerability `GHSA-rxmq-m78w-7wmc`.
- The test post-build `cp -n` command emitted portability warnings.

## Authoritative Post-Cleanup Baseline

Date: 2026-07-18

- Branch: `test/audit-follow-ups`.
- Base HEAD: `b5b3164`.
- Scope: current uncommitted Slice 1-6 worktree based on that HEAD; no commit was created during the slice.
- Command: `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal`.
- `.runsettings` was not supplied.
- Result: `0 failed, 493 passed, 0 skipped, 493 total, duration 9 s`.
- Skip reasons: none because no tests were skipped.
- Only `CheckCommand.feature` remains enabled and discovered by SpecFlow.
- The 2026-07-16 `514 passed / 3 skipped` result above is retained as historical review evidence, not the current baseline.

Post-cleanup meaning and remaining risks:

- False-green BDD no longer contributes passing scenarios for moderation, statistics, AI, captcha, permissions, or spam/ham commands.
- The two local spam/ham examples execute without secret-gated `SimpleE2ETests`; duplicate mimicry coverage was not migrated.
- `UserCleanupServiceTests` uses isolated temporary approval storage, but this does not isolate every direct storage constructor.
- `Integration/AiAnalysisTests.cs` still searches for `.env`, mutates process environment, and directly constructs `ApprovedUsersStorage`.
- `ApprovedUsersStorageTestFactory`, `UserCleanupServiceTestFactory`, and `MessageHandlerTestFactory` still construct real file-backed storage.
- The run still logged AI-enabled configuration and classifier training/retraining from other test construction paths.
- Global cache, timer, background-task, runner-relative filesystem, pipeline failure, captcha scheduler, and package-warning risks remain unresolved.

## Executive Conclusion

The problem is not only "bad architecture".

The current order of causes is:

1. Some tests are false-positive contracts: they pass without executing or observing the named behavior.
2. Several test harnesses reproduce production behavior in mocks and fakes.
3. The suite has competing construction paths for the same handler world.
4. Important production seams hide failures or combine decisions with side effects.
5. Time, filesystem, environment and cache are process-global or internally constructed.
6. Constructor size and duplicated wrappers amplify the preceding problems.

A broad architecture rewrite should not be the first response. The current suite is not trustworthy enough to prove that such a rewrite preserves behavior. Restore signal first, then make bounded production changes at the seams causing demonstrated test pain.

## Priority Summary

| Priority | Finding | Primary consequence |
|---|---|---|
| P0 | Executable BDD scenarios contain empty or tautological steps | Green suite overstates coverage |
| P0 | Test factories emulate notification, deletion, violation and AI behavior | Tests can prove the harness instead of production |
| P0 | Moderation effect failure is recorded as successful moderation | Production and tests cannot distinguish failure from success |
| P0 | Persistence is not consistently isolated per test | Cross-run state and unintended file mutation |
| P1 | `ModerationPolicy` mixes decisions and side effects | Focused decision tests require a broad dependency graph |
| P1 | Pipeline discards terminal `StepResult` | Callers and tests cannot observe failure reason |
| P1 | Captcha has competing timeout owners | Races, leaked tasks and real-time tests |
| P1 | Worker and classifier start untracked background work | Constructor/host tests leak work and filesystem access |
| P1 | Handler builders and E2E tests ignore configured behavior | Misleading tests and dead setup |
| P1 | Message ID fake compensation can select the wrong message | False-positive delete/report assertions |
| P1 | Hidden network/environment paths exist in ordinary tests | Non-hermetic and potentially expensive tests |
| P2 | Global cache and static settings couple unrelated tests | Order and parallelization hazards |
| P2 | Weak assertions and mislabeled layers obscure suite value | Poor failure diagnostics and maintenance tax |
| P2 | Duplicate wrappers/builders and large constructors multiply setup | Changes cascade through unrelated tests |

## Detailed Findings

### F01 — BDD scenarios pass without exercising production behavior

Severity: P0

Facts:

- `ClubDoorman.Test/Features/ModerationFlow.feature:8-35` claims order, forward deletion, spam deletion, logging and model-training behavior.
- Most owning bindings are empty TODO methods: `ClubDoorman.Test/StepDefinitions/Common/ModerationFlowSteps.cs:35-40`, `:60-79`, `:81-106`, `:109-141`.
- `ClubDoorman.Test/Features/StatisticsAndCommands.feature:19-30` claims report and authorization behavior, while owning setup/execution/assertion bindings are empty at `ClubDoorman.Test/StepDefinitions/Common/StatisticsAndCommandsSteps.cs:35-63`.
- The photo/API feature ends in `true.Should().BeTrue()` at `ClubDoorman.Test/StepDefinitions/Common/AiChecksPhotoLoggingSteps.cs:12-28`.
- AI-analysis bindings simulate outcomes or only check that no prior exception was recorded at `ClubDoorman.Test/StepDefinitions/Common/AiAnalysisSteps.cs:155-175`, `:266-319`, `:339-376`.
- Captcha timeout is simulated with a short delay rather than invoking timeout behavior at `ClubDoorman.Test/StepDefinitions/Common/CaptchaSteps.cs:84-98`.
- The full run visibly reported the empty `ModerationFlowSteps` methods as completed and passed the scenarios.

Impact:

- The green count cannot be interpreted as behavior coverage.
- A production regression in deletion, logging, ordering, training or notification can leave these scenarios green.
- Feature files currently mix executable contracts and unimplemented documentation without an explicit distinction.

Smallest safe direction:

- Classify each affected scenario as `DELETE`, `QUARANTINE`, or `REWRITE`.
- Do not fill empty steps with more simulation.
- Rewrite only behavior worth preserving, at the nearest existing seam.

Exit evidence:

- No enabled BDD step body is empty, tautological, or assertion-free.
- Every enabled scenario observes a production call/result or a fake-recorded external effect.

### F02 — `MessageHandlerTestFactory` emulates production behavior

Severity: P0

Facts:

- The factory forwards and sends suspicious notifications in mock callbacks at `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs:108-129`.
- It emulates moderation deletion and violation tracking at `:146-174`.
- Similar behavior is duplicated at `:555-605`.
- AI notification behavior is synthesized at `:1025-1045`.
- Some async operations are invoked without awaiting them at `:124-127`, `:165-169`, `:571-574`, `:602`.
- Surrounding `catch` blocks cannot observe exceptions faulting the returned tasks.
- `FakeServicesFactory` contains a separate AI-analysis/notification simulation at `ClubDoorman.Test/TestInfrastructure/FakeServicesFactory.cs:159-192`.

Impact:

- A test can pass because the helper produced the expected side effect.
- Production facade, effects or notification orchestration may never execute.
- Timing and exception behavior differs from production because helper callbacks do not await work.

Smallest safe direction:

- Freeze these helpers: no new scenarios and no new behavior emulation.
- Migrate one behavior group at a time to `IMessageStep`, `ModerationFacade`, `UserBanService`, messaging or command seams.
- Delete helper emulation only when no remaining test depends on that callback.

Non-goal:

- Do not rewrite the factory into a more sophisticated test DI container.

### F03 — `MessageHandlerBuilder` ignores configured moderation behavior

Severity: P1

Facts:

- `WithBanMocks` and `WithModerationMocks` configure `_moderationFacadeMock` at `ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs:226-286`.
- `Build()` creates a different local `IModerationFacade` mock and hardcodes `Allow` at `:309-327`.
- The built two-step pipeline therefore does not use the builder's configured facade.
- Several setup methods call configured mocks while setting up the builder and capture one returned task/value, losing argument-dependent behavior at `:92-103`, `:111-120`, `:128-136`, `:145-165`.

Impact:

- A test can request ban/delete/error behavior but execute an always-allow pipeline.
- The fluent API advertises contracts it does not honor.

Smallest safe direction:

- Stop new use immediately.
- Inventory existing call sites and determine whether any test depends on the advertised behavior.
- Prefer deleting/migrating callers over repairing the builder as a general harness.

### F04 — E2E-labelled moderation tests use a hardcoded mock

Severity: P1

Facts:

- `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs:52-69` constructs real-looking dependencies that are not wired into the tested moderation service.
- `_moderationService` is a mock returning `Allow` at `:70-75`.
- Spam and mimicry tests assert outcomes produced by that mock at `:84-118`, `:139-153`.
- The spam assertion accepts both `Allow` and `Delete` at `:113-117`.

Impact:

- These tests cannot detect moderation-order, spam-classification or mimicry regressions.
- The E2E/integration labels misrepresent both cost and coverage.

Smallest safe direction:

- Keep the fake-client contract test if it has value, but move/rename it to the correct layer.
- Delete or replace moderation cases with direct `ModerationPolicy`/`ModerationFacade` tests.

### F05 — Production moderation policy crosses its documented seam

Severity: P1

Facts:

- `docs/architecture-seams.md:79-95` says decision code must not call Telegram, AI endpoints, persistence or cache directly.
- `ModerationPolicy` has 12 dependencies including Telegram, messaging, bans, cleanup, AI and persistence at `ClubDoorman/Features/Moderation/03-Policies.cs:26-75`.
- It mutates global cache at `:108-115`.
- It performs ban/delete side effects at `:398-420`.
- It restricts users and sends notifications directly at `:436-477`, `:552-604`.
- It uses wall clock and static filesystem-backed settings at `:513-524`, `:615-643`.
- It combines AI, Telegram and messaging effects at `:802-899`.

Impact:

- Decision tests require unrelated operational dependencies.
- Policy methods have inconsistent purity: some return decisions, others execute effects.
- Tests need broad factories and can accidentally verify orchestration instead of decisions.

Smallest safe direction:

- Move existing operational methods to the existing facade/effect/ban/messaging seams incrementally.
- Start with `BanAndCleanupUserAsync`, `UnrestrictAndApproveUserAsync`, `CheckAiDetectAndNotifyAdminsAsync`, and `ExecuteModerationActionAsync`.
- Do not introduce another moderation facade or compatibility layer.

Required characterization before movement:

- Current returned `ModerationResult` for each branch.
- Current Telegram, notification, cache and cleanup effects for each moved method.

### F06 — Final moderation action reports success after effect failure

Severity: P0

Facts:

- `FinalModerationActionStep` catches exceptions from `HandleUserMessageAsync` at `ClubDoorman/Services/Handlers/Pipeline/Steps/FinalModerationActionStep.cs:41-48`.
- It then sets `UserResultHandled`, writes a `moderated` result and publishes a success event at `:49-61`.

Impact:

- Failed ban/delete/notification can be indistinguishable from success.
- Tests cannot assert failure through `StepResult` or the context.
- Golden/semantic events can record successful moderation when the effect failed.

Smallest safe direction:

- Return `StepResult.Fail(ex, "moderation-action-exception")`.
- Do not set success state or publish `moderated` after the exception.
- If partial success is a real supported state, represent it explicitly rather than treating it as success.

Focused verification target:

- Direct `FinalModerationActionStep` test with a throwing `IModerationFacade`.

### F07 — Pipeline failure is swallowed at the caller boundary

Severity: P1

Facts:

- `IMessagePipeline.RunAsync` returns `Task`, not a result.
- `MessagePipeline` converts exceptions to `StepResult.Fail` and breaks, but discards the result at `ClubDoorman/Services/Handlers/Pipeline/MessagePipeline.cs:19-39`.
- `MessageHandler` must infer failure from context flags and logging rather than a terminal result.

Impact:

- Caller tests cannot distinguish expected stop, failure, or exhausted pipeline.
- Handler tests gravitate toward log assertions and "does not throw" assertions.

Smallest safe direction:

- Return the terminal `StepResult` from `RunAsync`.
- Represent normal completion explicitly.
- Avoid adding another status flag to `MessageContext`.

### F08 — Pipeline outcome is an anonymous object plus flag matrix

Severity: P2

Facts:

- `MessageContext.UserResult` is `object?` at `ClubDoorman/Services/Handlers/Pipeline/MessageContext.cs:26-27`.
- Steps assign anonymous objects, including `CaptchaPendingStep.cs:42-46` and `FinalModerationActionStep.cs:57-60`.
- `MessageHandler` uses reflection to inspect the result at `ClubDoorman/Services/Handlers/MessageHandler.cs:223-226`.
- The context contains many overlapping handled flags at `MessageContext.cs:16-33`.

Impact:

- Outcome property changes are not compile-time checked.
- Tests cannot make simple typed assertions.
- Adding branches tends to add flags rather than clarify the terminal contract.

Smallest safe direction:

- After F07, replace `object?` with one small typed pipeline outcome.
- Do not combine this with a broad rewrite of every context flag.

### F09 — Message processing remains split between handler and pipeline

Severity: P2

Facts:

- Whitelist/disabled-chat decisions occur before the pipeline at `ClubDoorman/Services/Handlers/MessageHandler.cs:138-154`.
- Permission/network checks occur at `:156-165`.
- Static settings mutation occurs at `:167`.
- Pipeline starts at `:169-178`.
- Constructor dependencies remain for compatibility helpers and delayed deletion at `:28-65`, `:241-278`, `:313-359`.
- `_events` is injected but unused in the handler.

Impact:

- Pipeline tests do not cover admission/silent-mode behavior.
- Handler tests still require broad dependency setup.
- It is unclear which layer owns several branches.

Smallest safe direction:

- Remove unused dependencies first.
- Decide explicitly whether admission belongs in the adapter or a step; do not split it accidentally across both.
- Move/delete legacy helpers only after call-site inventory.

### F10 — Seven competing `MessageHandler` construction paths remain

Severity: P2

Facts:

Current test-side `new MessageHandler(...)` sites:

- `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs:177`, `:194`, `:608`.
- `ClubDoorman.Test/TestInfrastructure/FakeServicesFactory.cs:235`.
- `ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs:329`.
- `ClubDoorman.Test/TestKit/Infra/TestKitAutoFixture.cs:158`.
- `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs:120`.

Production DI has another authoritative pipeline list at `ClubDoorman/Infrastructure/ServiceCollectionExtensions.cs:151-164`.

Impact:

- Constructor and pipeline changes cascade through copied worlds.
- Some paths use full production-like step lists; others silently use two-step pipelines.
- A test's behavior depends on which world it selected, not only its setup.

Smallest safe direction:

- Keep the production registration authoritative.
- For tests, prefer direct step/service construction.
- Retire old construction paths by migrating call sites; do not add an eighth path.

### F11 — Persistence is not consistently isolated

Severity: P0

Facts:

- `ApprovedUsersStorage` reads `DOORMAN_DATA_ROOT`, falls back to relative `data`, and loads files in its constructor at `ClubDoorman/Services/UserManagement/ApprovedUsersStorage.cs:18-36`.
- Direct real constructions occur in:
  - `ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs:27`.
  - `ClubDoorman.Test/Integration/AiAnalysisTests.cs:109`.
  - `ClubDoorman.Test/StepDefinitions/Common/AiAnalysisSteps.cs:75`.
  - `ClubDoorman.Test/StepDefinitions/Common/UserManagementSteps.cs:24`.
  - `ClubDoorman.Test/TestInfrastructure/ApprovedUsersStorageTestFactory.cs:24`.
  - `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactory.cs:22`.
  - `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs:508`.
- Storage mutation and persistence are exercised in `UserCleanupServiceTests.cs:38-45`, `:81-88`, `:138-145`.

Risk:

- Tests can load stale approvals and write to a shared runner-relative directory.
- Parallel or repeated runs can affect each other.

Observed qualification:

- During this review, the fallback resolved under test output: `ClubDoorman.Test/bin/Debug/net9.0/data`.
- The exact path is runner-dependent, so repository/runtime data mutation remains possible in other invocation contexts.

Smallest safe direction:

- Give every test using real storage a unique temporary `DOORMAN_DATA_ROOT`.
- Restore the previous environment value and remove the directory in teardown.
- Prefer constructor path injection later; do not add a general filesystem abstraction.

Slice 5 isolation note (2026-07-18):

- `UserCleanupServiceTests` now uses a unique temporary `DOORMAN_DATA_ROOT` per test and restores the previous value in teardown.
- This isolates only that focused fixture. Direct `ApprovedUsersStorage` construction in `Integration/AiAnalysisTests.cs`, `ApprovedUsersStorageTestFactory`, `UserCleanupServiceTestFactory`, and `MessageHandlerTestFactory` remains separate follow-up work.

### F12 — Global `MemoryCache.Default` is a cross-test bus

Severity: P2

Facts:

- Moderation writes last-message state at `Features/Moderation/03-Policies.cs:108-115`.
- AI cache uses it at `Services/AI/AiChecks.cs:87-92`, `:120-126`, `:668`.
- Violations use it at `Services/Violation/ViolationTracker.cs:66-113`.
- Callback payloads pass through it in notification, messaging and callback handlers.
- `MessageHandlerTryFindUserIdTests` clears the entire default cache at `ClubDoorman.Test/Unit/Handlers/MessageHandlerTryFindUserIdTests.cs:41-51`.

Impact:

- Reused user/chat IDs contaminate tests.
- Parallel tests can consume or remove another test's callback payload.
- Expiration uses wall clock.
- Unrelated seams are coupled through string cache keys.

Smallest safe direction:

- Inject `IMemoryCache` where cache semantics are generic.
- Use narrowly named stores where payload ownership matters, especially callbacks and violations.
- Use a fresh cache per test container.

### F13 — Static chat settings couple logic to disk and wall clock

Severity: P2

Facts:

- `ChatSettingsManager` holds static mutable cache and relative paths at `ClubDoorman/Infrastructure/ChatSettingsManager.cs:5-10`.
- It performs direct reads/writes and swallows write exceptions at `:24-82`.
- It is used by handler, moderation policy, messaging, channel moderation and statistics.

Impact:

- Working directory and prior tests affect behavior.
- Tests cannot inject settings or observe persistence failures.

Smallest safe direction:

- Convert the existing API to an injected singleton with explicit path and `TimeProvider`.
- Preserve the current JSON format.
- Do not design a general settings framework.

### F14 — Captcha expiry has competing schedulers and real-time tasks

Severity: P1

Facts:

- `CreateCaptchaAsync` creates a `CancellationTokenSource` and starts an untracked 1.2-minute task at `ClubDoorman/Services/Captcha/CaptchaService.cs:147-205`.
- The task may start another untracked 20-minute unban task at `:182-194`.
- `BanExpiredCaptchaUsersAsync` separately polls the same dictionary at `:281-349`.
- Worker invokes that polling path every 15 seconds at `ClubDoorman/Worker.cs:71-77`.
- Callback handling contains another temporary-unban scheduler at `ClubDoorman/Services/Handlers/CallbackQueryHandler.cs:208-248`.
- `ICaptchaService` does not expose service-lifetime cancellation for this work.

Impact:

- Multiple owners race to remove the same captcha.
- Different expiry paths apply different violation and notification behavior.
- Tests leak 72-second and 20-minute tasks or use fixed sleeps.

Smallest safe direction:

- Choose one expiry owner.
- The least complex target is one hosted expiry loop with service-lifetime cancellation.
- Inject `TimeProvider` for timestamps and delays.
- Keep auto-unban requirements inside captcha flow as required by repository rules.

### F15 — Captcha tests include probabilistic and timing assertions

Severity: P2

Facts:

- `CaptchaServiceExtendedTests.CreateCaptchaAsync_MultipleUsers_CreatesUniqueCaptchas` requires independent random answers to differ at `ClubDoorman.Test/Unit/Services/CaptchaServiceExtendedTests.cs:140-183`.
- The test sleeps and retries to influence randomness.
- Two valid captchas are allowed to have the same answer.
- Other fixed-delay polling exists in `MessageHandlerDeleteMessageLaterTests.cs:41-44`, `:56-57`, `:73-76`.

Impact:

- The assertion is intrinsically flaky.
- CI load can break fixed-time expectations.

Smallest safe direction:

- Assert independent captcha state/keys, not different random answers.
- Replace time sleeps after F14 exposes controllable time or awaitable work.

### F16 — Worker owns untracked loops, disk access and duplicate state

Severity: P1

Facts:

- Worker has 12 injected dependencies plus a manually created `GlobalStatsManager` at `ClubDoorman/Worker.cs:28-62`.
- Three `PeriodicTimer` instances are constructed internally at `:47-49`.
- Four loops are started without tracking/awaiting at `:123-130`.
- Offset persistence directly reads/writes `data/offset.txt` at `:131-138`, `:156-168`.
- Another startup task is fire-and-forget at `:142-154`.
- DI already registers `GlobalStatsManager` at `ClubDoorman/Services/Statistics/StatisticsModule.cs:15-19`.

Impact:

- Hosted-service tests start real timers, filesystem access and refresh work.
- Background exceptions are not observed by `ExecuteAsync`.
- Worker statistics state can differ from the DI singleton inspected elsewhere.

Smallest safe direction:

- Inject the registered `GlobalStatsManager`.
- Track and await loop tasks during shutdown.
- Inject `TimeProvider` for periodic behavior.
- Isolate only offset persistence behind a small explicit seam or injected path.

### F17 — Classifier starts infinite filesystem-heavy work in its constructor

Severity: P1

Facts:

- `SpamHamClassifier` starts training and retraining tasks in its constructor at `ClubDoorman/Services/AI/SpamHamClassifier.cs:33-46`.
- A compatibility constructor creates concrete `AppConfig` at `:49-59`.
- Retraining is an infinite uncancelled loop at `:74-94`.
- Prediction polls initialization for up to ten real seconds at `:96-123`.
- Dataset and stop-word paths are hardcoded at `:61`, `:165-195`.
- The full test run visibly started multiple training/retraining loops.

Impact:

- Resolving a classifier starts work unrelated to the current test.
- Tests can leak infinite loops and race model readiness.
- Constructor completion does not mean the object is ready.

Smallest safe direction:

- Make classifier construction side-effect free.
- Move initial/retraining work into one cancellable hosted service.
- Remove the compatibility constructor instead of manually constructing config.

### F18 — AI provider is constructed internally and baseline mode is not a sufficient seam

Severity: P1

Facts:

- `AiChecks` stores concrete `OpenAiClient` and creates it from configuration at `ClubDoorman/Services/AI/AiChecks.cs:19-49`.
- Calls use that concrete provider throughout, including `:234-242`.
- Public methods primarily disable calls when `_api == null`; the intended `GoldenBaselineMode` guarantee in `docs/architecture-seams.md:112-115` is not consistently enforced at this class boundary.
- `AppConfigTestFactory.CreateDefault()` can enable the provider path.
- Test output repeatedly reported OpenRouter as configured.

Impact:

- Tests cannot inject deterministic provider responses.
- Presence of a process API key can change test behavior.
- Prompt/response handling is difficult to test without mocking all of `IAiChecks`.

Smallest safe direction:

- Short-circuit public AI operations when `GoldenBaselineMode` is true.
- Inject a narrow completion client/factory when testing provider behavior becomes a current need.
- Do not enable real API calls in normal test runs.

### F19 — Ordinary tests contain hidden environment and transport dependencies

Severity: P1

Facts:

- Captcha BDD creates a real `TelegramBotClient` and production wrapper at `ClubDoorman.Test/StepDefinitions/Common/CaptchaSteps.cs:27-39`.
- AI and E2E fixtures search for `.env` and mutate process environment at `ClubDoorman.Test/Integration/AiAnalysisTests.cs:45-104`, `SimpleE2ETests.cs:15-72`, and `StepDefinitions/Common/AiAnalysisSteps.cs:16-70`.
- Previous values are not restored.
- `SimpleE2ETests` skips all tests without secrets at `:45-72`, although the current tests only use local classifiers at `:82-132`.

Impact:

- Tests are not hermetic.
- Environment leakage can make behavior order-dependent.
- Deterministic local classifier coverage is skipped in clean CI.

Qualification:

- A real API request during the reviewed full run was not proven.
- The reachable/configured external path is proven.

Smallest safe direction:

- Replace real Telegram construction with wrapper mocks/fakes.
- Move local classifier tests out of secret-gated setup.
- Save and restore environment values in any remaining environment contract test.

Post-cleanup status (2026-07-18):

- Deleted captcha and AI BDD bindings no longer construct real Telegram clients or search for `.env`.
- Deleted `SimpleE2ETests` no longer gates local classifier examples on unrelated secrets.
- `Integration/AiAnalysisTests.cs` remains non-hermetic and is not resolved by these deletions.

### F20 — Message ID compensation can attribute effects to the wrong message

Severity: P1

Facts:

- `CreateValidMessageWithId` ignores the requested ID and produces `MessageId == 0` at `ClubDoorman.Test/TestData/TestDataFactory.Generated.cs:30-48`.
- `FakeTelegramClient` still uses that path for generated messages at `ClubDoorman.Test/TestInfrastructure/FakeTelegramClient.cs:66-80`, `:230`, `:307`.
- For ID `0`, deletion searches the first envelope matching only the chat at `:98-116`.
- With multiple messages in one chat, this cannot identify the intended message.
- `Reset()` does not clear `_messageEnvelopes` at `:635-650`.
- `TestKitTelegram.CreateCallbackQuery` accepts IDs that are not reliably applied at `ClubDoorman.Test/TestKit/TestKit.Telegram.cs:125-145`.

Impact:

- Delete/report/reply tests can pass for the wrong message.
- State can leak across fake resets.

Smallest safe direction:

- Keep `MessageEnvelope` as the sole source of authoritative IDs.
- Do not infer an envelope by chat alone.
- Clear envelope state on fake reset.
- Migrate only tests where ID identity matters; do not mechanically rewrite id-irrelevant tests.

Note:

- `ClubDoorman.Test/Integration/MessageHandlerBanBasicTests.cs:64-83` demonstrates the intended envelope assertion pattern.

### F21 — Many tests only prove that execution completed

Severity: P2

Facts:

- Search found 85 instances covering `Assert.Pass`, `DoesNotThrowAsync`, tautological `BeTrue()` and TODO markers.
- Representative handler cases: `ClubDoorman.Test/Unit/Handlers/MessageHandlerHandleUserMessageTests.cs:57-109`, `:204-217`, `:287-330`.
- Integration cases: `ClubDoorman.Test/Integration/MessageHandlerIntegrationTests.cs:57-164`.
- Handler extended cases: `ClubDoorman.Test/Unit/Handlers/MessageHandlerExtendedTests.cs:266-420`.
- Statistics cases: `ClubDoorman.Test/Unit/Services/GlobalStatsManagerTests.cs:48-89`, `:131-167`.
- `MessagePipeline` catches exceptions, so "did not throw" does not prove that the intended branch succeeded.

Impact:

- Named behavior can regress while tests remain green.
- Failure messages do not identify the broken observable contract.

Smallest safe direction:

- For each touched test, identify one observable result or side effect.
- Delete the test if no valuable behavior claim can be stated.
- Do not replace `Assert.Pass` with a generic non-null assertion.

### F22 — Test layers and categories do not describe actual coverage

Severity: P2

Facts:

- `InfrastructureE2ETests` tests a hardcoded mock and an in-memory fake.
- `FakeTelegramClientExtendedTests` is E2E-labelled while testing one fake.
- `AiAnalysisTests` is E2E-labelled while factories synthesize outcomes.
- `SimpleE2ETests` currently contains local classifier tests.
- Unit-labelled handler tests can construct a full 14-step pipeline through `MessageHandlerTestFactory`.

Impact:

- Category filters do not predict runtime, dependencies or confidence level.
- CI cannot select a trustworthy fast seam suite versus external smoke suite.

Smallest safe direction:

- Correct categories when a file is already being migrated.
- Do not perform a category-only mass rename before test value is known.

### F23 — Duplicated data builders have diverged

Severity: P2

Facts:

- Combined builders exist in `ClubDoorman.Test/TestKit/TestKit.Builders.cs:44-353`.
- Separate builders exist under `ClubDoorman.Test/TestKit/Builders/`.
- `WithMessageId` exists only in the combined builder and intentionally discards the supplied ID.
- Separate builders contain scenarios absent from the combined set.
- Defaults and ban reasons differ.

Impact:

- Similar-looking tests create materially different objects.
- Builder changes do not reach all call sites.

Smallest safe direction:

- Inventory usage first.
- Prefer the separate data-only builders.
- Remove duplicate classes only after call-site migration.

### F24 — Process-static random/test generators are nondeterministic

Severity: P2

Facts:

- `TestKitBogus` uses a static unseeded faker at `ClubDoorman.Test/TestKit/Infra/TestKitBogus.cs:13-35`.
- `TestKitAutoFixture` uses per-call `new Random()` at `:257-264`.
- `TestKit.Telegram` has a static non-atomic message counter at `ClubDoorman.Test/TestKit/TestKit.Telegram.cs:15-17`, `:40-42`, `:248-258`.

Risk:

- Failures may not reproduce from seed.
- Parallel use can produce duplicate or reordered IDs.

Smallest safe direction:

- Use fixed seeds only where random data adds value.
- Prefer explicit values for behavior tests.
- Use atomic counters if shared ID generation remains necessary.

### F25 — Additional constructor-time filesystem and network work

Severity: P2

Facts:

- `SimpleFilters` loads stop words during static initialization at `ClubDoorman/Services/SimpleFilters.cs:6-9`.
- `BadMessageManager` loads files in its constructor at `Services/BadMessage/BadMessageManager.cs:16-43`.
- `SuspiciousUsersStorage` loads a fixed relative file at `Services/SuspiciousUsers/SuspiciousUsersStorage.cs:25-31`.
- `GlobalStatsManager` creates directories and reads files in its constructor at `Services/Statistics/GlobalStatsManager.cs:18-28`.
- `UserManager` owns concrete HTTP clients and constructs another inside refresh at `Services/UserManagement/UserManager.cs:17-20`, `:38-48`.

Impact:

- Resolving a service graph can touch disk/network before the test acts.
- Working-directory assumptions leak into unrelated tests.
- Banlist refresh cannot be tested without real transport control.

Smallest safe direction:

- Prioritize paths currently blocking focused tests.
- Inject a named `HttpClient` or narrow banlist client for `UserManager`.
- Inject explicit paths/providers for file-backed services; avoid a universal filesystem abstraction.

### F26 — Service locator and DI duplication hide dependencies

Severity: P2

Facts:

- `ModerationEffectsBuilder` stores `IServiceProvider` and resolves dependencies per branch at `ClubDoorman/Features/Moderation/ModerationEffectsBuilder.cs:40-147`.
- Callback and chat-member handlers are registered as both concrete and interface singletons at `ClubDoorman/Services/Handlers/HandlersModule.cs:23-30`, which can create distinct instances.
- `Program.cs:107-118` calls `BuildServiceProvider` during registration, creating a second service-provider graph.

Impact:

- Constructor signatures do not reveal real dependencies.
- Tests can configure/inspect a different handler instance from the dispatcher instance.
- Missing registrations fail only when a specific branch executes.

Smallest safe direction:

- Inject existing effect dependencies directly into the builder.
- Register concrete handlers once and map `IUpdateHandler` to the same instance.
- Remove the registration-time nested provider.

### F27 — Moderation wrappers duplicate a broad contract

Severity: P2

Facts:

- Broad facade and policy contracts overlap in `ClubDoorman/Features/Moderation/05-Contracts.cs:10-242`.
- `ModerationFacade` is largely pass-through at `02-ModerationFacade.cs:38-96`.
- `ModerationServiceAdapter` is another pass-through at `04-Services.cs:11-78`.
- Different consumers mock different wrappers for similar operations.

Impact:

- Tests must choose between policy, facade and legacy service without a clear behavioral distinction.
- Contract changes cascade through multiple mocks and adapters.

Smallest safe direction:

- First narrow policy to decisions as in F05.
- Migrate consumers toward one facade while keeping one compatibility adapter only as long as concrete consumers require it.
- Do not add a fourth moderation abstraction.

### F28 — Constructor size reflects mixed responsibilities

Severity: P2

Observed dependency counts:

| Type | Dependencies | Reference |
|---|---:|---|
| `CallbackQueryHandler` | 15 | `Services/Handlers/CallbackQueryHandler.cs:32-79` |
| `Worker` | 12 injected plus manual state | `Worker.cs:28-62` |
| `ModerationPolicy` | 12 | `Features/Moderation/03-Policies.cs:26-75` |
| `ChatMemberHandler` | 11 | `Services/Handlers/ChatMemberHandler.cs:28-65` |
| `UserBanService` | 10 | `Services/UserBan/UserBanService.cs:32-65` |
| `AiCascadeService` | 10 | `Services/AI/AiCascadeService.cs:22-55` |
| `MessageHandler` | 10 | `Services/Handlers/MessageHandler.cs:28-65` |

Interpretation:

- Dependency count alone is not the defect.
- It is evidence of mixed responsibilities when dependencies belong to unrelated branches and force broad test construction.
- Worker retains dependencies used only by legacy/dead private methods at `Worker.cs:208-238`, `:304-405`.

Smallest safe direction:

- Delete dead methods and unused dependencies first.
- Split only around an existing branch/owner, not to satisfy an arbitrary constructor limit.

### F29 — Delayed actions are duplicated with inconsistent cancellation

Severity: P2

Facts:

Delayed delete/unban/flag expiry logic exists in:

- `Services/Handlers/MessageHandler.cs:313-359`.
- `Worker.cs:379-397`.
- `Services/Notifications/NotificationService.cs:418-436`.
- `Services/Notifications/ButtonsService.cs:125-143`.
- `Services/Messaging/MessageService.cs:252-264`, `:310-322`.
- `Services/UserManagement/JoinedUserFlags.cs:21-31`.

Impact:

- Each copy has different exception, lifetime and cancellation semantics.
- Tests rely on sleeps or cannot observe completion.

Smallest safe direction:

- Delete dead copies first.
- If duplication remains current pain, add one narrowly scoped delayed-operation scheduler backed by `TimeProvider` for Telegram actions/flag expiry.
- Do not create a general job framework.

### F30 — Dead and misleading test infrastructure remains

Severity: P3

Facts:

- `TestTimeoutHelper` and `test-timeouts.json` are copied and maintained but no tests call the helper.
- `ClubDoorman.Test/ClubDoorman.Test.csproj:52-54`, `:79-80` copies the file.
- The project suppresses a broad list of compiler/nullability warnings at `ClubDoorman.Test.csproj:14-18`.
- Default filtering excludes `demo`, while `.runsettings` separately excludes `disabled`; application of `.runsettings` is not automatic from the project file.

Impact:

- Dead machinery obscures the actual timeout model.
- Warning suppression can hide test setup defects.
- Disabled-test policy depends on invocation details.

Smallest safe direction:

- Remove dead timeout infrastructure after confirming no external scripts use it.
- Reduce warning suppression incrementally only when affected files are touched.
- Make category/filter policy explicit in one invocation path.

## Root-Cause Map

```text
Mixed production responsibilities
  -> large dependency graphs
  -> broad test factories/builders
  -> factories emulate missing production behavior
  -> tests assert helper effects or "does not throw"
  -> green suite overstates confidence

Hidden global state/time/filesystem
  -> tests require cleanup, sleeps and environment setup
  -> parallel/order-dependent risk
  -> broad E2E labels and secret gating

Pipeline hides terminal result
  -> handler cannot observe failure directly
  -> tests inspect logs/flags/reflection
  -> effect failure can be recorded as success
```

This map matters for planning: deleting weak tests alone reduces maintenance cost but does not fix the production seams that keep generating broad tests. Conversely, refactoring production first is unsafe while false-positive tests remain enabled.

## Planning Principles

Any implementation plan derived from this review should follow these rules:

1. Restore trust before broad refactoring.
2. Use one seam and one behavior group per slice.
3. Prefer deletion over preserving a test with no meaningful contract.
4. Do not create another general-purpose harness.
5. Do not edit shared test infrastructure merely to make one test easier.
6. Make production changes only where current test pain or correctness risk is demonstrated.
7. Run focused tests first and the full suite after each broad/shared slice.
8. Treat test-count reduction as neutral; behavior signal matters.
9. Preserve no real external calls in normal tests.
10. Keep current persisted formats and externally visible bot behavior unless a slice explicitly changes them.

## Candidate Workstreams

These are planning inputs, not approved phases. They are ordered by dependency and risk.

### Workstream A — Remove false-green coverage

Candidate bounded slices:

1. Inventory all enabled BDD scenarios whose bindings are empty, tautological or assertion-free.
2. Record `KEEP/REWRITE/DELETE/QUARANTINE` decisions in the existing quarantine decision format.
3. Delete or quarantine one feature file at a time.
4. For a rewritten behavior, add one direct seam test before removing the old scenario.
5. Audit remaining `Assert.Pass`/`DoesNotThrowAsync` handler tests by file and delete cases without an observable contract.

Do not:

- Implement empty BDD steps with fake state assignments.
- Preserve scenarios solely because stakeholders can read Gherkin.

### Workstream B — Isolate test process state

Candidate bounded slices:

1. Isolate `ApprovedUsersStorage` tests with unique temporary roots and restored environment.
2. Move secret-independent classifier tests out of `SimpleE2ETests` setup.
3. Remove real Telegram client construction from captcha BDD.
4. Fix `FakeTelegramClient.Reset()` and envelope identity without changing unrelated fake behavior.
5. Replace global-cache clearing in focused tests with an injected/fresh cache after the owning production seam supports it.

### Workstream C — Retire competing handler worlds

Candidate bounded slices:

1. Inventory direct usages of `MessageHandlerBuilder`; classify callers before changing the builder.
2. Migrate one `MessageHandlerTestFactory` scenario group to its owning pipeline step/service.
3. Migrate or delete `FakeServicesFactory` AI scenarios.
4. Remove each construction path only after zero call sites remain.
5. Keep a small routing/smoke set that verifies externally meaningful outcomes.

### Workstream D — Make pipeline failure observable

Candidate bounded slices:

1. Add a focused failing-effect test for `FinalModerationActionStep` documenting the desired failure contract.
2. Stop recording moderation success after effect failure.
3. Change `IMessagePipeline.RunAsync` to return terminal `StepResult` and update direct consumers/tests.
4. Introduce a typed terminal outcome only after the returned result contract is stable.

Risk:

- This changes production failure semantics and requires explicit approval before implementation.

### Workstream E — Narrow moderation decision ownership

Candidate bounded slices:

1. Inventory `IModerationPolicy` methods as decision-only versus operational.
2. Choose one operational method with existing facade/effect ownership.
3. Add/confirm focused characterization at current owning seams.
4. Move that one method without adding a wrapper.
5. Remove dependencies made unused by the move.
6. Repeat only when the previous slice reduces real setup pain.

### Workstream F — Make time and background work controllable

Candidate bounded slices:

1. Decide the single captcha expiry owner.
2. Add `TimeProvider` and service-lifetime cancellation to that owner.
3. Remove the competing per-captcha or polling scheduler.
4. Replace captcha timing tests with controlled-time tests.
5. Inject the DI `GlobalStatsManager` into Worker and track loop tasks.
6. Move classifier training/retraining from constructor to a cancellable hosted service.

Do not:

- Build a generic scheduler before deleting dead/duplicate delayed-action code.

### Workstream G — Resolve secondary structural debt

Candidate bounded slices:

1. Fix duplicate handler DI registrations and remove registration-time `BuildServiceProvider`.
2. Replace `ModerationEffectsBuilder` service location with explicit existing dependencies.
3. Consolidate duplicate data builders after usage inventory.
4. Remove dead timeout infrastructure.
5. Address package version drift and the ImageSharp advisory separately from test architecture changes.

## Suggested First Plan Boundary

The first implementation plan should not include production architecture changes.

Recommended first boundary:

**Goal:** make the reported test count honest enough to support later refactoring.

Included:

- Classify enabled no-op/tautological BDD scenarios.
- Delete or quarantine confirmed false-green scenarios.
- Move secret-independent local classifier tests out of secret-gated E2E setup.
- Isolate direct real `ApprovedUsersStorage` usage in the focused tests selected for the slice.
- Record the new baseline and skipped reasons.

Excluded:

- Pipeline contract changes.
- Moderation policy movement.
- Captcha scheduler changes.
- Shared handler harness redesign.
- Global cache migration.

Why this boundary:

- It reduces false confidence without changing bot behavior.
- It makes later architecture changes safer.
- Each scenario/file decision can be reviewed independently.
- It builds directly on the existing cleanup and quarantine strategy.

## Verification Matrix For Future Slices

| Changed area | Focused command | Required broader check |
|---|---|---|
| BDD feature/bindings | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~<FeatureName>"` | Full test project after a deletion batch |
| Pipeline | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~MessagePipelineTests|FullyQualifiedName~<StepName>"` | Full suite per repo rule |
| Moderation policy/facade | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~Moderation"` | Full suite per repo rule |
| Captcha | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~CaptchaService"` | Full suite per repo rule |
| Worker/background services | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~Worker"` | Full suite |
| Fake Telegram IDs | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~FakeTelegramClient|FullyQualifiedName~MessageHandlerBanBasicTests"` | Full suite for shared fake changes |
| Storage isolation | `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --filter "FullyQualifiedName~ApprovedUsersStorage|FullyQualifiedName~UserCleanupService"` | Repeat focused run, then full suite if shared setup changed |

Common hygiene:

```bash
git diff --check -- <touched-files>
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore --verbosity minimal
```

## Success Criteria

The cleanup/testability effort is successful when:

- No enabled scenario passes through empty or tautological bindings.
- Normal tests make no real Telegram or AI calls.
- Secret-free local tests are not skipped because of unrelated credentials.
- Real file-backed storage uses an isolated path in tests.
- New behavior tests use one obvious production seam.
- Handler tests cover routing/orchestration, not ban/AI/messaging internals.
- A failed moderation effect is observable as failure.
- Pipeline callers receive a typed or explicit terminal result.
- Captcha expiration has one owner and controllable time.
- Constructors do not start infinite background work.
- Legacy construction paths shrink through call-site migration rather than expansion.
- Full-suite green status has a documented meaning and skipped tests have explicit reasons.

## Risks And Unknowns

- Existing dirty test files may already be implementing parts of these recommendations. Reinspect before every slice.
- The reviewed full run saw API-enabled configuration but did not prove a live external request.
- Actual intra-assembly parallel execution depends on NUnit/adapter behavior despite project parallelization properties. Shared-state risk exists regardless.
- Some old worklog baselines and target files are stale because cleanup has already removed tests. Use current filesystem state before planning a slice.
- Moving side effects out of `ModerationPolicy` may expose undocumented ordering dependencies. Characterize one branch at a time.
- Changing pipeline failure semantics can alter production logging and retry behavior; treat it as a behavior change.
- One captcha expiry owner must preserve temporary-ban and auto-unban requirements.
- Package vulnerability remediation may require dependency/API changes and should be planned separately.

## Open Decisions Before Implementation Planning

1. Should false-green BDD scenarios be deleted immediately or retained as explicitly non-executable documentation outside the test project?
2. Which production correction comes first after test trust improves: pipeline failure semantics or captcha scheduler ownership?
3. Is callback/violation cache persistence required across process restarts? Current code suggests in-memory semantics, but this should be confirmed before introducing narrow stores.
4. Which 5-10 broad smoke behaviors are important enough to retain after seam-test migration?

## Review Snapshot

No production or test source was changed by this review. This document is the only review artifact added in this step.
