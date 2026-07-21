# Agent Repo Instructions

Operational guide for making changes without understanding the whole bot.
Every section tells you what to read, what seam to touch, what NOT to touch, and which tests to run.

---

## When changing moderation behavior

- **Read:** `docs/architecture-seams.md` §5 (Moderation Decision)
- **Primary seam:** `ClubDoorman/Features/Moderation/05-Contracts.cs` (`IModerationPolicy`, `ModerationResult`), `03-Policies.cs` (`ModerationPolicy`), `02-ModerationFacade.cs` (`ModerationFacade`), `04-Services.cs` (`ModerationServiceAdapter`), `01-ModerationFeature.cs` (DI)
- **Forbidden edits:** Do not call Telegram API, AI endpoints, DB, or cache from decision code. Side effects belong in facade/effects/ban/messaging seams. Do not modify `ClubDoorman/Services/Moderation/` wrappers without updating the adapter.
- **Focused tests:** `ClubDoorman.Test` — tests exercising `ModerationPolicy` / `ModerationFacade` directly. Assert `ModerationResult.Action` and `Reason`. No Telegram API calls needed.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing Telegram adapter

- **Read:** `docs/architecture-seams.md` §1 (Telegram Adapter)
- **Primary seam:** `ClubDoorman/Services/Telegram/ITelegramBotClientWrapper.cs` (interface), `TelegramBotClientWrapper.cs` (implementation)
- **Forbidden edits:** Must not depend on DI container, feature layers, or moderation logic. Only depends on `Telegram.Bot` SDK types and `Microsoft.Extensions.Logging`.
- **Focused tests:** Any test that already uses `ITelegramBotClientWrapper` — verify mock calls match new signatures.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing message routing / handlers

- **Read:** `docs/architecture-seams.md` §2 (Update Dispatcher) and §3 (Update Handlers)
- **Primary seam:** `ClubDoorman/Services/Handlers/IUpdateHandler.cs`, `ClubDoorman/Services/Dispatcher/UpdateDispatcher.cs`, `MessageHandler.cs`, `CallbackQueryHandler.cs`, `ChatMemberHandler.cs`
- **Routing:** `UpdateDispatcher` iterates handlers sequentially; calls `CanHandle(update)` then `HandleAsync(update, ct)`.
- **Forbidden edits:** Do not hardcode handler types in the dispatcher. Do not call Telegram API directly from the dispatcher. Handlers must receive all services via DI.
- **Focused tests:** `UpdateDispatcherTestFactory` + handler tests — verify `CanHandle` / `HandleAsync` called on expected handlers.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing message pipeline / steps

- **Read:** `docs/architecture-seams.md` §4 (Message Pipeline)
- **Primary seam:** `ClubDoorman/Services/Handlers/Pipeline/IMessagePipeline.cs`, `MessagePipeline.cs`, `IMessageStep.cs`, `MessageContext.cs`, `StepResult.cs`
- **Steps:** `SystemOrBotMessageStep`, `CommandStep`, `NewMembersStep`, `PrivateSkipStep`, `LeftMemberCleanupStep`, `ChannelMessageStep`, `CaptchaPendingStep`, `BanlistCheckStep`, `AlreadyApprovedStep`, `ClubMemberSkipStep`, `AiProfileAnalysisStep`, `FinalModerationActionStep`
- **Forbidden edits:** Steps must not call the real Telegram API directly. Side effects go through injected services/wrappers. Steps should return `StepResult.Fail(ex)` for handled errors instead of leaking to the pipeline.
- **Focused tests:** Tests building a pipeline with mock steps — verify execution order, early termination, context mutations.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing AI verdict provider

- **Read:** `docs/architecture-seams.md` §6 (AI Verdict Provider)
- **Primary seam:** `ClubDoorman/Services/AI/IAiChecks.cs` ↔ `AiChecks.cs`, `IAiCascadeService.cs` ↔ `AiCascadeService.cs`, `ISpamHamClassifier.cs` ↔ `SpamHamClassifier.cs`, `IMimicryClassifier.cs` ↔ `MimicryClassifier.cs`, `AIModule.cs`
- **Key config:** `IAppConfig.GoldenBaselineMode` disables AI/ML calls; returns safe defaults. Polly retry + 30s timeout on AI HTTP calls.
- **Forbidden edits:** `IAiChecks` must not depend on Telegram SDK beyond `User`/`Message` types. `AiChecks` uses `ITelegramBotClientWrapper` only for photo download. Do not introduce DB/cache dependencies.
- **Focused tests:** Mock `ITelegramBotClientWrapper` and set `GoldenBaselineMode=true` to skip all external calls. For ML, mock `ISpamHamClassifier`.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing captcha flow

- **Read:** `docs/architecture-seams.md` §7 (Captcha Flow)
- **Primary seam:** `ClubDoorman/Services/Captcha/ICaptchaService.cs`, `CaptchaService.cs`, `CaptchaModule.cs`
- **Key behavior:** Generates inline keyboard with emoji buttons; stores active captchas in `ConcurrentDictionary<string, CaptchaInfo>`; auto-bans on timeout (1.2 min); auto-unbans after 20 min.
- **Forbidden edits:** CaptchaService is intentionally not migrated to `UserBanService` (requires auto-unban). Must not depend on AI services.
- **Focused tests:** `CaptchaServiceTestFactory` + `CaptchaServiceTestFactoryTests` — verify `CreateCaptchaAsync` / `ValidateCaptchaAsync` with mocked dependencies. Use `CancellationToken` to avoid real timers.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing ban/mute/delete side effects

- **Read:** `docs/architecture-seams.md` §8 (Ban / Mute / Delete Side Effects)
- **Primary seam:** `ClubDoorman/Services/UserBan/IUserBanService.cs`, `UserBanService.cs`, `UserBanModule.cs`, `BanType.cs`
- **Ban types:** `BanTypeEnum`: `LongName`, `Blacklist`, `AutoBan`, `ManualBan`, `ProfileBan`, `ChannelBan`, `CaptchaBan`, `RepeatedViolation`
- **Key methods:** `BanUserForLongNameAsync`, `BanBlacklistedUserAsync`, `AutoBanAsync`, `AutoBanChannelAsync`, `HandleBlacklistBanAsync`, `TrackViolationAndBanIfNeededAsync`, `BanUserAsync`, `DeleteMessageByIdAsync`
- **Forbidden edits:** Must not depend on AI services. Must not create its own `ITelegramBotClientWrapper`.
- **Focused tests:** Mock `ITelegramBotClientWrapper`; invoke ban methods; verify `BanChatMemberAsync` / `DeleteMessageAsync` called with correct parameters.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing admin commands

- **Read:** `docs/architecture-seams.md` §3 (Update Handlers) + admin ops files
- **Primary seam:** `ClubDoorman/Features/AdminOps/CommandRouter.cs`, `ICommandRouter.cs`, `ICommandHandler.cs`, `CommandProcessingService.cs`, `01-AdminOpsFeature.cs` (DI), `CommandsModule.cs`
- **Command handlers:** `StartCommandHandler`, `CheckCommandHandler`, `SayCommandHandler`, `HamCommandHandler`, `SpamCommandHandler`, `StatsCommandHandler`, `StatsAliasCommandHandler`, `SuspiciousCommandHandler`
- **Forbidden edits:** Do not bypass `CommandRouter` for new commands. Do not add Telegram API calls directly in command handlers — use injected services.
- **Focused tests:** `CallbackQueryHandlerTestFactory` + handler tests — feed synthetic updates; verify side effects via mock calls.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing persistence / cache

- **Read:** `docs/architecture-seams.md` §8 (Ban side effects) + user management files
- **Primary seam:** `ClubDoorman/Services/UserManagement/IUserManager.cs`, `UserManager.cs`, `UserIndex.cs`, `IUserIndex.cs`, `ApprovedUsersStorage.cs`, `JoinedUserFlags.cs`, `IUserCleanupService.cs`, `UserCleanupService.cs`, `UserManagementModule.cs`
- **Also:** `ClubDoorman/Services/Violation/IViolationTracker.cs`, `ViolationTracker.cs`, `ViolationModule.cs`
- **Forbidden edits:** Do not introduce new persistence backends without updating the module registration. Do not bypass `IViolationTracker` for violation tracking.
- **Focused tests:** Tests using `ApprovedUsersStorageTestFactory`, `UserManagerExtendedTests`, or `ViolationTracker` tests.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing config / policy

- **Read:** `docs/architecture-seams.md` §5 (Moderation Decision) + config files
- **Primary seam:** `ClubDoorman/Services/Core/Configuration/IAppConfig.cs`, `AppConfig.cs`, `ConfigurationModule.cs`, `ConfigurationHelper.cs`
- **Options classes:** `AiOptions.cs`, `AutoBanOptions.cs`, `ChatAccessOptions.cs`, `ChatFilteringOptions.cs`, `CoreOptions.cs`, `FeatureToggleOptions.cs`, `TestHarnessOptions.cs`, `ViolationThresholdOptions.cs`, `LoggingFlagsOptions` (in `ClubDoorman/Models/Logging/`)
- **Forbidden edits:** Do not add new options without registering them in `ConfigurationModule.cs` and `IAppConfig`. Do not change existing option names (breaks config files).
- **Focused tests:** Tests that construct `AppConfigTestFactory` or mock `IAppConfig`. Verify new options are accessible.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing logging / observability

- **Read:** `docs/architecture-seams.md` §10 (Logging / Golden Master / Event Publishing)
- **Primary seam:** `ClubDoorman/Services/Logging/IGoldenMasterRecorder.cs`, `GoldenMasterRecorder.cs`, `NullGoldenMasterRecorder.cs`, `IModerationEventPublisher.cs`, `GoldenMasterModerationEventPublisher.cs`, `NullModerationEventPublisher.cs`, `ReasonCodeMapper.cs`
- **Key config:** `LoggingFlagsOptions` — `GoldenMasterEnabled`, `GoldenSampleRate`, `GoldenDeterministicIds`, `GoldenFixedDateFolder`, `GoldenBasePath`
- **Data sanitization:** User IDs masked (`U` + last 4 digits), usernames hashed (SHA256 first 8 hex chars), text truncated to 160 chars, properties sorted for deterministic JSON.
- **Forbidden edits:** Must not depend on Telegram SDK beyond `Update` type. Must not have side effects when disabled (sample rate = 0).
- **Focused tests:** Use `NullGoldenMasterRecorder` / `NullModerationEventPublisher` in tests. For real recorder, set `GoldenMasterEnabled=true`, `GoldenSampleRate=1.0`, `GoldenDeterministicIds=true`, verify file output.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

## When changing messaging / notifications

- **Read:** `docs/architecture-seams.md` §9 (Messaging / Service-Chat Notifications)
- **Primary seam:** `ClubDoorman/Services/Messaging/IMessageService.cs` ↔ `MessageService.cs`, `IServiceChatDispatcher.cs` ↔ `ServiceChatDispatcher.cs`, `ILogChatService.cs` ↔ `LogChatService.cs`, `INotificationService.cs`, `MessageTemplates.cs`, `MessagingModule.cs`
- **Routing logic:** `IServiceChatDispatcher.ShouldSendToAdminChat` — type-based switch: `SuspiciousMessageNotificationData`, `AiProfileAnalysisData`, `AiDetectNotificationData`, `ErrorNotificationData` → admin-chat; everything else → log-chat.
- **Forbidden edits:** Must not depend on moderation policy directly. Must not depend on AI services.
- **Focused tests:** Mock `ITelegramBotClientWrapper`; invoke notification methods; verify correct chat ID and message formatting.
- **Full-suite rule:** After focused tests pass, run the full suite before handing off.

---

## Repo-Wide Rules

### Test layer rules

Four layers exist. Pick the right one — don't expand scope by default.
Read `docs/test-infra-strategy.md` before changing shared test helpers, `MessageHandler` tests, or Telegram `Message` builders.

| Layer | What it checks | Mocks / fakes | External deps |
|-------|---------------|---------------|---------------|
| **Unit** | Pure or near-pure logic in a single class/method. | None or trivial (`NullLogger`) | None |
| **Contract / Seam** | A seam's input/output on the concrete class. | All `ITelegramBotClientWrapper`, `IAppConfig`, `IModerationService`, etc. | None |
| **Integration** | Several components wired together (DI, pipeline, facade). | `FakeTelegramClient` (in-memory fake), some mocks | DI container; no real Telegram/AI/DB |
| **E2E / Smoke** | Broad bot/system behavior. May load real `.env`, call real AI. | `FakeTelegramClient` or real config | Real `.env`, real AI API, real FS |

**Rules:**

- **Do not edit** `ClubDoorman.Test/TestInfrastructure/` or `ClubDoorman.Test/TestKit/` unless the seam contract itself changes (interface signature, method name, required dependency). These are shared test contracts. If you need a helper, add it in your focused test file or create a narrowly-scoped helper — do not modify existing factories.
- **Run focused tests first.** Only the tests that directly exercise the seam you are changing. Run the full suite only at the end, or before handing off broad changes that touch multiple seams.
- **Do not start by simplifying tests globally.** If a small business logic change would require rewriting half the bot's tests, first look for a seam test that already covers it (e.g. `ModerationServiceTests` for moderation logic, `MessageHandlerTestFactory` for handler behavior, `TelegramBotClientWrapperTestFactory` for Telegram adapter, `AiChecksTestFactory` for AI checks). Do not expand factories by default.
- **Unit tests** cover pure logic (e.g. `ServiceChatDispatcher.ShouldSendToAdminChat`, `ModerationResult` properties). **Contract/seam tests** cover one concrete class with all external deps mocked (e.g. `ModerationServiceTests`, `CaptchaServiceFakeTests`). **Integration tests** wire several components with `FakeTelegramClient` (e.g. `InfrastructureE2ETests` — honest integration despite the name). **E2E/smoke tests** are rare and heavy (e.g. `SimpleE2ETests`).

### Test infrastructure simplification

Current test infrastructure has several legacy construction paths. Do not add another one.

- `TestKit/Builders/*` should stay focused on test data objects (`Message`, `User`, `Chat`, DTOs). Do not add service orchestration there.
- `TestInfrastructure/*Factory.cs` files are shared legacy factory contracts. Do not expand them by default; prefer moving one test closer to the production seam.
- `TestKit.MessageHandlerBuilder` is transitional. Keep it thin; do not add behavior emulation.
- `FakeServicesFactory` is legacy broad integration setup. Do not add new scenarios there unless the test is explicitly broad integration.
- If changing a `MessageHandler` test, first ask whether the assertion belongs at `MessageHandler`, a pipeline step, `ModerationFacade`, or `UserBanService`.

### Telegram Message tests

`Telegram.Bot.Types.Message.MessageId` is not reliably set by normal test construction in this repo; existing helpers document that it often remains `0`.

- If `MessageId` is irrelevant, use a plain `new Message { From, Chat, Text, Date }` or a data builder.
- If delete/forward/reply/report assertions need `MessageId`, use `MessageEnvelope` with `FakeTelegramClient.RegisterMessageEnvelope`, `CreateMessageFromEnvelope`, and `WasMessageDeleted(envelope)`.
- Do not use `CreateValidMessageWithId()` or `MessageBuilder.WithMessageId()` as proof that `Message.MessageId` was set.
- Do not add reflection hacks or new message builders to fight Telegram SDK model limits.

### TestInfrastructure / TestKit

Do NOT edit files in `ClubDoorman.Test/TestInfrastructure/` or `ClubDoorman.Test/TestKit/` unless the seam contract itself changes (interface signature, method name, or required dependency). These factories and builders are shared test contracts. If you need a new test helper, add it in your focused test file or create a narrowly-scoped helper — do not modify existing factories.

### Test execution order

1. **Run focused tests first.** Only the tests that directly exercise the seam you are changing.
2. **Run the full suite only at the end**, or before handing off broad changes that touch multiple seams.
3. Never run the full suite as a substitute for focused verification.

### DI discipline

All services must be resolved through DI. Do not `new` up concrete services in production code. If a service is missing from DI, register it in the appropriate `*Module.cs` (e.g., `CaptchaModule.cs`, `AIModule.cs`, `ConfigurationModule.cs`).

### No real external calls in tests

Never introduce real Telegram API, AI endpoint, or database calls in tests. Always mock or use `GoldenBaselineMode` / null implementations.
