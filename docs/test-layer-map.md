# Test Layer Map

Classification of existing tests by layer — not a rewrite plan. Every entry notes what the test checks, what it mocks/stubs/fakes, which seam it covers, and whether the layer classification is honest or mixed.

---

## Layer Definitions

| Layer | What it checks | Mocks/stubs/fakes | External deps |
|-------|---------------|-------------------|---------------|
| **Unit** | Pure or near-pure logic in a single class/method. Minimal or no mocks. | None or trivial (e.g. `NullLogger`) | None |
| **Contract / Seam** | A seam's input/output contract. The concrete class under test, all external dependencies mocked. | All `ITelegramBotClientWrapper`, `IAppConfig`, `IModerationService`, etc. | None (all external calls mocked) |
| **Integration** | Several app components wired together (DI container, pipeline, facade). Minimal external infrastructure. | `FakeTelegramClient` (in-memory fake), some mocks for leaf services | DI container; no real Telegram/AI/DB |
| **E2E / Smoke** | Broad bot/system behavior. May load real `.env`, call real AI endpoints, or verify environment readiness. | `FakeTelegramClient` or real configuration | Real `.env`, real AI API, real file system |

---

## Unit Tests

### `ClubDoorman.Test.Unit.Infrastructure.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `TelegramBotClientWrapperTests` | Constructor accepts `TelegramBotClient`, `BotId` property, `GetChatFullInfo` copies `Photo` | Real `TelegramBotClient("fake-token")`, `NullLogger` | §1 Telegram Adapter | **Honest** — tests the wrapper's public surface with a real SDK client (no network calls). One test reads source file with `File.ReadAllText` — brittle but still unit-level. |
| `WorkerTests` | `Worker` lifecycle / `RunAsync` | Minimal | Internal infrastructure | **Honest** |
| `WorkerGetChatLinkTests` | `Worker.GetChatLink` behavior | Minimal | Internal infrastructure | **Honest** |
| `StatisticsServiceGetChatLinkTests` | `StatisticsService.GetChatLink` behavior | Minimal | Internal infrastructure | **Honest** |

### `ClubDoorman.Test.Unit.Handlers.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `MessageHandlerCanHandleTests` | `MessageHandler.CanHandle` returns correct bool for various `Update` shapes | `MessageHandlerTestFactory` with mocked dependencies | §3 Update Handlers | **Honest** — tests a single method with all external deps mocked via factory. |
| `MessageHandlerHandleAsyncBasicTests` | `HandleAsync` basic flow | `MessageHandlerTestFactory` with mocks | §3 Update Handlers | **Honest** |
| `MessageHandlerHandleSayCommandTests` | `/say` command handling | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerHandleUserMessageTests` | User message processing | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerSemanticsTests` | MessageHandler semantic behavior (moderation actions) | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerStatsCommandTests` | `/stats` command | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerDeleteMessageLaterTests` | Deferred message deletion | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerNullCoalescingTests` | Null safety in MessageHandler | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerExtendedTests` | Extended MessageHandler behavior | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerFakeTests` | MessageHandler with `FakeModerationService` | `FakeModerationService` (deterministic) | §3 Update Handlers | **Mixed** — uses a fake service (not a mock) but still tests one handler. |
| `MessageHandlerTryFindUserIdTests` | `TryFindUserId` helper | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerSendSuspiciousMessageTests` | Sending suspicious message notifications | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerMutationCoverageTests` | Mutation coverage for MessageHandler | `MessageHandlerTestFactory` | §3 Update Handlers | **Honest** |
| `MessageHandlerGoldenMasterTests` | Golden Master recording in MessageHandler | `MessageHandlerTestFactory` + `GoldenMasterRecorder` | §3 Update Handlers, §10 Logging | **Honest** |
| `CallbackQueryHandlerTests` | `CallbackQueryHandler.HandleAsync` | `CallbackQueryHandlerTestFactory` with mocks | §3 Update Handlers | **Honest** |
| `CallbackQueryHandlerSemanticsTests` | CallbackQueryHandler semantic behavior | `CallbackQueryHandlerTestFactory` | §3 Update Handlers | **Honest** |

### `ClubDoorman.Test.Unit.Services.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `AiChecksTests` | `AiChecks` constructor, null handling, `GetSpamProbability`, `GetAttentionBaitProbability` | Mock `ITelegramBotClientWrapper`, `NullLogger`, `AppConfigTestFactory` (GoldenBaselineMode) | §6 AI Verdict Provider | **Honest** — real `AiChecks` with mocked external deps. |
| `AiChecksExtendedTests` | Extended `AiChecks` scenarios | Mocks | §6 AI Verdict Provider | **Honest** |
| `UserBanServiceTests` | `BanUserForLongNameAsync`, `BanBlacklistedUserAsync`, `AutoBanAsync`, `AutoBanChannelAsync` | Mocks for all dependencies | §8 Ban / Mute / Delete | **Honest** — real `UserBanService` with all external deps mocked. |
| `CaptchaServiceFakeTests` | `CreateCaptchaAsync`, `ValidateCaptchaAsync`, `GenerateKey`, `RemoveCaptcha` | Mocks for `ITelegramBotClientWrapper`, `IMessageService`, `IViolationTracker`, `IUserBanService` | §7 Captcha Flow | **Honest** — real `CaptchaService` with mocked deps. |
| `CaptchaServiceExtendedTests` | Extended captcha scenarios | Mocks | §7 Captcha Flow | **Honest** |
| `ServiceChatDispatcherTests` | `ShouldSendToAdminChat` routing logic | None (pure logic) | §9 Messaging | **Honest** — pure unit test. |
| `MessageTemplatesHtmlTests` | HTML formatting in message templates | None | §9 Messaging | **Honest** |
| `CommandRouterTests` | Command routing | Mocks | §3 Update Handlers (admin commands) | **Honest** |
| `CommandProcessingServiceTests` | Command processing logic | Mocks | §3 Update Handlers | **Honest** |
| `CommandRouterIntegrationTests` | CommandRouter with multiple command handlers | Mocks | §3 Update Handlers | **Honest** |
| `ChannelModerationServiceTests` | Channel moderation logic | Mocks | §3 Update Handlers | **Honest** |
| `ChannelModerationEffectsBuilderTests` | Effects builder for channel moderation | Mocks | §3 Update Handlers | **Honest** |
| `BotPermissionsServiceTests` | Bot permission checks | Mocks | §3 Update Handlers | **Honest** |
| `ModerationServiceBusinessLogicTests` | Business logic in moderation service | Mocks | §5 Moderation Decision | **Honest** |
| `ModerationActionDispatcherTests` | Action routing, missing/duplicate registration failures | In-memory recording handlers | §5 Moderation Decision | **Honest** — real dispatcher with no external dependencies. |
| `ModerationActionHandlersTests` | Side-effect calls, ordering, silent mode, confidence propagation | Mocks for handler dependencies | §5 Moderation Decision | **Honest** — real stateless action handlers. |
| `ModerationFacadeTests` | Facade-to-dispatcher runtime context propagation | Mock policy and dispatcher | §5 Moderation Decision | **Honest** — real facade. |
| `SuspiciousUsersStorageTests` | `SuspiciousUsersStorage` behavior | `NullLogger` | §6 AI Verdict Provider | **Honest** |
| `UserCleanupServiceTests` | User cleanup logic | Mocks | §8 Ban / Mute / Delete | **Honest** |
| `UpdateDispatcherTests` | `UpdateDispatcher` routing to handlers | Mock `IUpdateHandler` | §2 Update Dispatcher | **Honest** |
| `GlobalStatsManagerTests` | `GlobalStatsManager` behavior | `NullLogger` | Internal | **Honest** |

### `ClubDoorman.Test.Unit.Services.*ModuleTests` (DI Registration)

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `TelegramModuleTests` | DI registration for Telegram services | `ServiceCollection` | §1 Telegram Adapter | **Honest** — tests DI container configuration. |
| `CommandsModuleTests` | DI registration for commands | `ServiceCollection` | §3 Update Handlers | **Honest** |
| `AIModuleTests` | DI registration for AI services | `ServiceCollection` | §6 AI Verdict Provider | **Honest** |
| `CaptchaModuleTests` | DI registration for captcha | `ServiceCollection` | §7 Captcha Flow | **Honest** |
| `MessagingModuleTests` | DI registration for messaging | `ServiceCollection` | §9 Messaging | **Honest** |
| `ConfigurationModuleTests` | DI registration for config | `ServiceCollection` | §5 Moderation Decision | **Honest** |
| `StatisticsModuleTests` | DI registration for statistics | `ServiceCollection` | §8 Ban / Mute / Delete | **Honest** |
| `UserManagementModuleTests` | DI registration for user management | `ServiceCollection` | §8 Ban / Mute / Delete | **Honest** |

### `ClubDoorman.Test.Unit.Moderation.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `ModerationServiceTests` | `ModerationService` (Features layer) behavior | Mocks | §5 Moderation Decision | **Honest** |
| `ModerationServiceExtendedTests` | Extended moderation service scenarios | Mocks | §5 Moderation Decision | **Honest** |
| `SpamHamClassifierTests` | ML classifier behavior | `NullLogger` | §6 AI Verdict Provider | **Honest** |
| `ModerationRegistrationTests` | DI registration for moderation | `ServiceCollection` | §5 Moderation Decision | **Honest** |

### `ClubDoorman.Test.Unit.Golden.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `GoldenNormalizationTests` | Canonicalization of updates | None | §10 Logging | **Honest** |
| `GoldenManifestTests` | Golden manifest file structure | None | §10 Logging | **Honest** |
| `GoldenAggregateTests` | Golden aggregate logic | None | §10 Logging | **Honest** |
| `GoldenHygieneTests` | Golden hygiene / cleanup | None | §10 Logging | **Honest** |
| `GoldenV2ExportTests` | Golden V2 export | None | §10 Logging | **Honest** |
| `GoldenDeterminismTests` | Deterministic output | None | §10 Logging | **Honest** |

### `ClubDoorman.Test.Unit.Logging.*`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `RuleCodeWhitelistTests` | `RuleCode` enum stability | None | §10 Logging | **Honest** — guard test. |
| `ReasonCodeMapperEdgeTests` | Reason code mapping edge cases | None | §10 Logging | **Honest** |

### `ClubDoorman.Test.Unit.Services.UserManagerExtendedTests`

| Test class | What it checks | Mocks / fakes | Seam | Layer honest? |
|-----------|---------------|---------------|------|---------------|
| `UserManagerExtendedTests` | `UserManager` extended scenarios | Mocks | §8 Ban / Mute / Delete (persistence) | **Honest** |

---

## Contract / Seam Tests (Top-level)

### `ClubDoorman.Test.ModerationServiceTests.cs`
- **What it checks**: `ModerationService` (Services layer adapter) — `CheckMessageAsync`, `CheckUserNameAsync`, `BanAndCleanupUserAsync`.
- **Mocks / fakes**: `ModerationServiceTestFactory` with mocked `ISpamHamClassifier`, `IBadMessageManager`, `IUserBanService`.
- **Seam**: §5 Moderation Decision (adapter layer).
- **Layer honest?**: **Honest** — real service with mocked dependencies.

### `ClubDoorman.Test.ModerationServiceSimpleTests.cs`
- **What it checks**: `ModerationServiceAdapter` with `FakeModerationService` — `CheckUserName`, `CheckMessage`.
- **Mocks / fakes**: `FakeModerationService` (deterministic policy), `ModerationServiceAdapter` (adapter).
- **Seam**: §5 Moderation Decision (adapter + fake policy).
- **Layer honest?**: **Honest** — tests the adapter seam with a fake policy implementation.

### `ClubDoorman.Test.ErrorHandlingTests.cs`
- **What it checks**: Null/empty validation in `IModerationFacade`, `AiChecks`, custom exception creation.
- **Mocks / fakes**: Mocked `IModerationFacade` (for facade tests), `AiChecksTestFactory` (for AiChecks tests), `ModerationServiceTestFactory`.
- **Seam**: §5 Moderation Decision, §6 AI Verdict Provider.
- **Layer honest?**: **Mixed** — facade tests use a mock that throws (tests the mock, not the real facade). AiChecks tests are honest.

---

## Integration Tests

### `ClubDoorman.Test.Integration.MessageHandlerIntegrationTests.cs`
- **What it checks**: Full `MessageHandler.HandleAsync` flow for commands (`/start`, `/stats`, `/suspicious`) and messages.
- **Mocks / fakes**: `MessageHandlerTestFactory` with `WithStandardMocks()` — all external deps mocked.
- **Seam**: §3 Update Handlers, §4 Message Pipeline.
- **Layer honest?**: **Mixed** — named "integration" but uses full mock setup. Tests one handler end-to-end with mocked dependencies. Closer to seam test.

### `ClubDoorman.Test.Integration.MessageHandlerBanBasicTests.cs`
- **What it checks**: Ban flow through `MessageHandler`.
- **Mocks / fakes**: `MessageHandlerTestFactory` with mocks.
- **Seam**: §3 Update Handlers, §8 Ban / Mute / Delete.
- **Layer honest?**: **Mixed** — seam-level test labeled as integration.

### `ClubDoorman.Test.Integration.MessageHandlerBanTests.cs`
- **What it checks**: Ban flow with more scenarios.
- **Mocks / fakes**: `MessageHandlerTestFactory` with mocks.
- **Seam**: §3 Update Handlers, §8 Ban / Mute / Delete.
- **Layer honest?**: **Mixed** — same pattern.

### `ClubDoorman.Test.Integration.MessageHandlerBanAdvancedTests.cs`
- **What it checks**: Advanced ban scenarios.
- **Mocks / fakes**: `MessageHandlerTestFactory` with mocks.
- **Seam**: §3 Update Handlers, §8 Ban / Mute / Delete.
- **Layer honest?**: **Mixed**.

### `ClubDoorman.Test.Integration.MessageHandlerBanExceptionTests.cs`
- **What it checks**: Ban with exception handling.
- **Mocks / fakes**: `MessageHandlerTestFactory` with mocks.
- **Seam**: §3 Update Handlers, §8 Ban / Mute / Delete.
- **Layer honest?**: **Mixed**.

### `ClubDoorman.Test.Integration.AiAnalysisTests.cs`
- **What it checks**: AI analysis flow through message/callback handling; file is marked `integration`, `e2e`, and `ai-analysis`.
- **Mocks / fakes**: `MessageHandlerTestFactory`, `FakeTelegramClient`, several Moq dependencies; may load `.env` when present.
- **Seam**: §6 AI Verdict Provider, §3 Update Handlers.
- **Layer honest?**: **Mixed / E2E-labeled** — combines seam tests with broad setup and optional environment loading.

### `ClubDoorman.Test.Integration.AiChecksPhotoLoggingTest.cs`
- **What it checks**: Photo logging / photo analysis path in `AiChecks`; file is marked `integration` and `ai-photo`.
- **Mocks / fakes**: `FakeTelegramClient`; loads `.env` and requires `DOORMAN_OPENROUTER_API` or skips.
- **Seam**: §6 AI Verdict Provider, §10 Logging.
- **Layer honest?**: **E2E / smoke-like** — uses fake Telegram but real environment/API configuration.

### `ClubDoorman.Test.Integration.FakeTelegramClientExtendedTests.cs`
- **What it checks**: Extended behavior of `FakeTelegramClient`.
- **Mocks / fakes**: `FakeTelegramClient` (the fake itself).
- **Seam**: §1 Telegram Adapter (test infrastructure).
- **Layer honest?**: **Honest** — tests the fake client's behavior.

### `ClubDoorman.Test.Integration.UserJoinFacadeIntegrationTests.cs`
- **What it checks**: `UserJoinFacade` proxy behavior — `HandleNewMembersAsync`, `ProcessNewUserAsync`.
- **Mocks / fakes**: `TK.CreateUserJoinFacadeBuilder().WithStandardMocks()`.
- **Seam**: §3 Update Handlers (new members step).
- **Layer honest?**: **Honest** — tests the facade seam with mocked policy.

### `ClubDoorman.Test.Integration.UserJoinServiceIntegrationTests.LegacyPlaceholder.cs`
- **What it checks**: Legacy placeholder.
- **Mocks / fakes**: —
- **Seam**: —
- **Layer honest?**: **N/A** — placeholder, no real tests.

### `ClubDoorman.Test.Integration.InfrastructureE2ETests.cs`
- **What it checks**: Moderation flow order, `FakeTelegramClient` tracking, test data factories, moderation result properties, async operations.
- **Mocks / fakes**: `FakeTelegramClient` (in-memory fake), `TestData` factories, some mocks.
- **Seam**: §1 Telegram Adapter, §5 Moderation Decision, §8 Ban / Mute / Delete.
- **Layer honest?**: **Honest integration** — exercises several app components together with a fake Telegram client. No real external calls.

### `ClubDoorman.Test.Integration.SimpleE2ETests.cs`
- **What it checks**: Real spam detection, mimicry detection, complete AI analysis with photo.
- **Mocks / fakes**: `FakeTelegramClient`, real `SpamHamClassifier`, real `MimicryClassifier`, real `AiChecks`.
- **Seam**: §6 AI Verdict Provider, §1 Telegram Adapter.
- **Layer honest?**: **E2E** — loads real `.env` file, may call real OpenRouter API. Skipped if `.env` not found.

### `ClubDoorman.Test.Integration.EnvironmentTest.cs`
- **What it checks**: `.env` file loading and environment variable accessibility.
- **Mocks / fakes**: None — real file system.
- **Seam**: —
- **Layer honest?**: **E2E / smoke** — exercises real file I/O, skips if `.env` missing.

---

## E2E / Smoke Tests

| Test class | What it checks | Mocks / fakes | External deps | Layer honest? |
|-----------|---------------|---------------|---------------|---------------|
| `SimpleE2ETests` | Real spam/ham detection, mimicry, full AI analysis with photo download | `FakeTelegramClient` | Real `.env`, real OpenRouter API (OpenRouter), real file system | **E2E** — heavy, requires `.env`, may be slow. |
| `EnvironmentTest` | `.env` file loading | None | Real file system | **Smoke** — checks environment readiness. |

---

## Test Infrastructure

### `ClubDoorman.Test.TestInfrastructure.*`

| File | What it is | Type |
|------|-----------|------|
| `FakeServicesFactory.cs` | Factory creating `FakeCaptchaService`, `FakeCallbackQueryHandler`, `FakeModerationService`, `MessageHandler` with full pipeline | **Fake / Test Double** |
| `FakeTelegramClient.cs` | In-memory fake for `ITelegramBotClientWrapper` — tracks sent messages, bans, deletions | **Fake / Test Double** |
| `FakeCaptchaService.cs` | Deterministic captcha service | **Fake** |
| `FakeCallbackQueryHandler.cs` | Deterministic callback handler | **Fake** |
| `FakeModerationService.cs` | Deterministic moderation policy | **Fake** |
| `ModerationServiceTestFactory.cs` | Factory for `ModerationService` with mocked dependencies | **Factory** |
| `MessageHandlerTestFactory.cs` | Factory for `MessageHandler` with mocked dependencies | **Factory** |
| `CallbackQueryHandlerTestFactory.cs` | Factory for `CallbackQueryHandler` | **Factory** |
| `CaptchaServiceTestFactory.cs` | Factory for `CaptchaService` | **Factory** |
| `AiChecksTestFactory.cs` | Factory for `AiChecks` | **Factory** |
| `TelegramBotClientWrapperTestFactory.cs` | Factory for `TelegramBotClientWrapper` | **Factory** |
| `SpamHamClassifierTestFactory.cs` | Factory for `SpamHamClassifier` | **Factory** |
| `MimicryClassifierTestFactory.cs` | Factory for `MimicryClassifier` | **Factory** |
| `StatisticsServiceTestFactory.cs` | Factory for `StatisticsService` | **Factory** |
| `ServiceChatDispatcherTestFactory.cs` | Factory for `ServiceChatDispatcher` | **Factory** |
| `UpdateDispatcherTestFactory.cs` | Factory for `UpdateDispatcher` | **Factory** |
| `UserCleanupServiceTestFactory.cs` | Factory for `UserCleanupService` | **Factory** |
| `ApprovedUsersStorageTestFactory.cs` | Factory for `ApprovedUsersStorage` | **Factory** |
| `SuspiciousUsersStorageTestFactory.cs` | Factory for `SuspiciousUsersStorage` | **Factory** |
| `AppConfigTestFactory.cs` | Factory for `IAppConfig` | **Factory** |
| `MockAiChecksFactory.cs` | Factory for mocked `IAiChecks` | **Factory** |
| `AiServiceExceptionTestFactory.cs` | Factory for `AiServiceException` | **Factory** |
| `ConfigurationExceptionTestFactory.cs` | Factory for `ConfigurationException` | **Factory** |
| `TelegramApiExceptionTestFactory.cs` | Factory for `TelegramApiException` | **Factory** |
| `ModerationExceptionTestFactory.cs` | Factory for `ModerationException` | **Factory** |
| `UserManagementExceptionTestFactory.cs` | Factory for `UserManagementException` | **Factory** |
| `GlobalStatsManagerTestFactory.cs` | Factory for `GlobalStatsManager` | **Factory** |
| `TestAdmin.cs` | Admin test data | **Test Data** |
| `TestMarkers.cs` | Test category markers | **Test Data** |
| `BehaviorComparisonTestRunner.cs` | Compares behavior between implementations | **Test Runner** |

### `ClubDoorman.Test.TestKit.*`

| File | What it is | Type |
|------|-----------|------|
| `TestKit.Main.cs` | Main entry point (`TK`) | **Test Helper** |
| `TestKit.Builders.cs` | Object builders (Message, User, Chat, etc.) | **Builder** |
| `TestKit.Mocks.cs` | Mock creation helpers | **Test Helper** |
| `TestKit.Facade.cs` | Facade builders | **Builder** |
| `TestKit.Telegram.cs` | Telegram-specific helpers | **Test Helper** |
| `TestKit.Specialized.cs` | Specialized scenarios | **Builder** |
| `TestKit.GoldenMaster.cs` | Golden Master helpers | **Test Helper** |
| `TestKit.UserJoinFacadeBuilder.cs` | `UserJoinFacade` builder | **Builder** |
| `TestKit.UserJoinServiceBuilder.cs` | `UserJoinService` builder | **Builder** |
| `TestKit.MessageHandlerBuilder.cs` | `MessageHandler` builder | **Builder** |
| `TestKit.AutoFixture.cs` / `TestKit.Bogus.cs` | Data generation | **Builder** |
| `Builders/MockBuilders/*` | Mock-specific builders | **Builder** |
| `Builders/ScenarioBuilder.cs` | Scenario builders | **Builder** |
| `TestCategories.cs` | Test category constants | **Test Data** |
| `TestKit.BuilderTests.cs` | Tests for the builders themselves | **Unit** (of test infrastructure) |

---

## Top-Level Tests (Mixed / Utility)

| File | What it checks | Mocks / fakes | Seam | Layer honest? |
|------|---------------|---------------|------|---------------|
| `CriticalFunctionalityTests.cs` | `TextProcessor.NormalizeText`, `SimpleFilters`, `CaptchaInfo`, `ModerationResult`, `ChatStats` properties | None | Multiple (data models) | **Honest unit** — pure logic / property tests. |
| `SimpleFiltersTests.cs` | `SimpleFilters.FindAllRussianWordsWithLookalikeSymbols`, `HasStopWords`, `FormatStripped` | None | §6 AI Verdict Provider (filtering) | **Honest unit** — pure static methods. |
| `SimpleFiltersNullTests.cs` | Null/empty handling in `SimpleFilters` | None | §6 AI Verdict Provider | **Honest unit**. |
| `TextProcessorNullTests.cs` | `TextProcessor.NormalizeText` null/empty/multiline | None | §6 AI Verdict Provider | **Honest unit**. |
| `GoldenMasterRecorderTests.cs` | Golden Master recorder writes deterministic input JSON | `NullLogger`, `Options.Create` | §10 Logging | **Honest unit** — writes to temp dir, cleans up. |

---

## Rewrite Candidates

These are tests or test groups where a concrete seam from `docs/architecture-seams.md` could be used to simplify infrastructure while preserving coverage.

| Candidate | Current issue | Concrete seam | What would change |
|-----------|--------------|---------------|-------------------|
| `MessageHandlerIntegrationTests` (and all `MessageHandlerBan*Tests`) | Labeled "integration" but use full mock setup — no real component interaction beyond the handler. | §3 Update Handlers — `MessageHandler` with `MessageHandlerTestFactory` | Rename to `MessageHandlerSeamTests` or `MessageHandlerHandleTests`. No code change needed — just clarify intent. |
| `AiAnalysisTests`, `AiChecksPhotoLoggingTest` | Mix seam-level fake Telegram setup with `.env`/API-sensitive behavior. | §3 Update Handlers, §6 AI Verdict Provider | Split later only if needed: keep real API coverage as E2E, move fake-only behavior toward seam tests. |
| `ErrorHandlingTests` (facade tests) | Tests a mock of `IModerationFacade` that throws — verifies the mock, not the real facade. | §5 Moderation Decision — `ModerationFacade` | Replace mock-facade tests with real `ModerationFacade` tests only after selecting this seam in a HITL slice. |
| `SimpleE2ETests` | Loads real `.env`, uses real classifiers and may call OpenRouter through `AiChecks`; already marked `[Category("e2e")]`. | §6 AI Verdict Provider | Keep as E2E. For fast coverage, add separate GoldenBaselineMode seam tests rather than weakening this test. |
| `EnvironmentTest` | Loads `.env` and asserts keys exist — environment smoke test. | — | Keep as environment smoke unless a later HITL slice changes test categories. |
| `TelegramBotClientWrapperTests` | One test reads source file via `File.ReadAllText` to verify `Photo = chat.Photo` line. | §1 Telegram Adapter | Replace with a behavioral test that constructs a `Chat` with `Photo` and verifies the wrapper copies it correctly through reflection or a real SDK client call. |
| `InfrastructureE2ETests` | Despite "E2E" name, uses `FakeTelegramClient` and mocks — honest integration level. | §1 Telegram Adapter, §5 Moderation Decision | Rename to `InfrastructureIntegrationTests` for clarity. |
| `FakeTelegramClientExtendedTests` | Tests the fake client itself. | §1 Telegram Adapter (test infra) | Rename to `FakeTelegramClientTests` or move into `TestInfrastructure` tests. |

---

## Summary

| Layer | Count (approx.) | Notes |
|-------|----------------|-------|
| **Unit** | ~60 tests | Well-distributed across `Unit/Handlers/`, `Unit/Services/`, `Unit/Golden/`, `Unit/Logging/`, `Unit/Moderation/`. Honest classification. |
| **Contract / Seam** | ~25 tests | `ModerationServiceTests`, `ModerationServiceSimpleTests`, `ErrorHandlingTests` (partial). Honest. |
| **Integration** | ~30 tests | `Integration/` has a mix of honest integration (DI config, `FakeTelegramClient`-based) and seam-level tests mislabeled as integration. |
| **E2E / Smoke** | Few tests | `SimpleE2ETests` (real AI/API-sensitive path), `AiChecksPhotoLoggingTest` (real `.env`/API key), `EnvironmentTest` (.env smoke). |
| **Test Infrastructure** | ~50 factory tests | Factories + factory tests. Honest — they test the test helpers. |
| **TestKit** | ~20 tests | Tests for builders. Honest unit tests of test infrastructure. |
| **Top-level misc** | ~15 tests | `CriticalFunctionalityTests`, `SimpleFilters*`, `TextProcessorNullTests`. Honest unit. |

**Total**: ~200+ test classes, ~941 passing tests (as of verification).

---

## Verification

**Command**: `dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "Category!=demo"`

**Result**: **PASSED** — 941 passed, 12 skipped, 0 failed, 953 total. Duration: 16s.
