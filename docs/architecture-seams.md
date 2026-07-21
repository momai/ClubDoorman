# Architecture Seams

This is a map of current repo seams, not a proposed architecture.
Every seam below describes an existing interface or boundary with its concrete files, inputs, outputs, forbidden dependencies, and how to test it without real Telegram / AI / DB / cache.

---

## 1. Telegram Adapter

**Seam:** `ITelegramBotClientWrapper` ↔ real Telegram Bot API

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/Telegram/ITelegramBotClientWrapper.cs` |
| **Implementation** | `ClubDoorman/Services/Telegram/TelegramBotClientWrapper.cs` |
| **Input** | `Update` objects from Telegram; `ChatId`, `Message`, `User` parameters for API calls |
| **Output** | `Message`, `bool`, `DeleteMessageResult`, `Chat`, `ChatFullInfo`, `ChatMember`, `User[]`, `Update[]` |
| **Key methods** | `SendMessageAsync`, `BanChatMemberAsync`, `DeleteMessageAsync`, `DeleteMessageWithOutcomeAsync`, `UnbanChatMemberAsync`, `GetChatFullInfo`, `ForwardMessage`, `SendPhoto`, `EditMessageText`, `RestrictChatMember`, `GetChatAdministratorsAsync`, `GetInfoAndDownloadFile`, `AnswerCallbackQuery`, `EditMessageReplyMarkup` |
| **Forbidden deps** | Must not depend on DI container, feature layers, or moderation logic. Only depends on `Telegram.Bot` SDK types and `Microsoft.Extensions.Logging`. |
| **Test strategy** | Mock `ITelegramBotClientWrapper` in every consumer. No real Telegram calls needed. |

---

## 2. Update Dispatcher

**Seam:** `UpdateDispatcher` → `IEnumerable<IUpdateHandler>`

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/Handlers/IUpdateHandler.cs` |
| **Dispatcher** | `ClubDoorman/Services/Dispatcher/UpdateDispatcher.cs` |
| **Implementors** | `MessageHandler`, `CallbackQueryHandler`, `ChatMemberHandler` |
| **Input** | `Telegram.Bot.Types.Update` |
| **Output** | Side effects via handlers (messages sent, bans, deletions); void return |
| **Routing logic** | Sequential `foreach` over handlers; calls `handler.CanHandle(update)` first, then `handler.HandleAsync(update, ct)` |
| **Forbidden deps** | Must not hardcode handler types. Must not call Telegram API directly; side effects stay inside handlers/services selected by `IUpdateHandler`. |
| **Test strategy** | Construct `UpdateDispatcher` with a list of mock `IUpdateHandler`s; assert `CanHandle` / `HandleAsync` called on expected handlers. |

---

## 3. Update Handlers

**Seam:** `IUpdateHandler` implementations

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/Handlers/IUpdateHandler.cs` (`CanHandle`, `HandleAsync`) |
| **MessageHandler** | `ClubDoorman/Services/Handlers/MessageHandler.cs` — handles `Message` and `EditedMessage` updates |
| **CallbackQueryHandler** | `ClubDoorman/Services/Handlers/CallbackQueryHandler.cs` — handles callback queries (captcha, admin buttons) |
| **ChatMemberHandler** | `ClubDoorman/Services/Handlers/ChatMemberHandler.cs` — handles `chat_member` updates (join/leave) |
| **Input** | `Telegram.Bot.Types.Update` |
| **Output** | Side effects (Telegram API calls via `ITelegramBotClientWrapper`, moderation decisions, notifications) |
| **MessageHandler deps** | `ITelegramBotClientWrapper`, `IAppConfig`, `IChannelModerationService`, `ICommandRouter`, `IBotPermissionsService`, `IGoldenMasterRecorder`, `IModerationEventPublisher`, `IMessagePipeline`, `IOptions<LoggingFlagsOptions>` |
| **Forbidden deps** | Handlers must not directly instantiate services; all via DI. `MessageHandler` must not bypass the pipeline for message processing. |
| **Test strategy** | Mock all DI dependencies; feed synthetic `Update` objects; verify side effects via recorded calls on mocks. |

---

## 4. Message Pipeline

**Seam:** `IMessagePipeline` → ordered `IMessageStep` chain

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/Handlers/Pipeline/IMessagePipeline.cs` |
| **Implementation** | `ClubDoorman/Services/Handlers/Pipeline/MessagePipeline.cs` |
| **Step interface** | `ClubDoorman/Services/Handlers/Pipeline/IMessageStep.cs` (`Order`, `Name`, `ExecuteAsync`) |
| **Context** | `ClubDoorman/Services/Handlers/Pipeline/MessageContext.cs` |
| **Result** | `ClubDoorman/Services/Handlers/Pipeline/StepResult.cs` (`Continue()`, `StopOk()`, `Fail()`) |
| **Steps (ordered)** | `SystemOrBotMessageStep`, `CommandStep`, `NewMembersStep`, `PrivateSkipStep`, `LeftMemberCleanupStep`, `ChannelMessageStep`, `CaptchaPendingStep`, `BanlistCheckStep`, `AlreadyApprovedStep`, `ClubMemberSkipStep`, `AiProfileAnalysisStep`, `FinalModerationActionStep` |
| **Input** | `MessageContext` (wraps `Update`, `Message`, `User`, `Chat`, flags) |
| **Output** | Modified `MessageContext` (flags like `CommandHandled`, `UserResultHandled`, `ModerationResult`), side effects |
| **Execution model** | Sequential; stops on first `StepResult.Stop == true` or `StepResult.Failed == true` |
| **Forbidden deps** | Steps must not call the real Telegram API directly; side effects go through injected services/wrappers. Steps should return `StepResult.Fail(ex)` for handled step errors instead of leaking expected control-flow failures to the pipeline. |
| **Test strategy** | Build a pipeline with mock steps; verify order of execution, early termination, and context mutations. |

---

## 5. Moderation Decision

**Seam:** `IModerationPolicy` (Features layer) ↔ `IModerationService` (Services layer adapter)

| Item | Detail |
|------|--------|
| **Core interface** | `ClubDoorman/Features/Moderation/05-Contracts.cs` → `IModerationPolicy` |
| **Implementation** | `ClubDoorman/Features/Moderation/03-Policies.cs` → `ModerationPolicy` |
| **Facade** | `ClubDoorman/Features/Moderation/02-ModerationFacade.cs` → `ModerationFacade` |
| **Adapter** | `ClubDoorman/Features/Moderation/04-Services.cs` → `ModerationServiceAdapter` (implements `IModerationService`) |
| **Services wrapper** | `ClubDoorman/Services/Moderation/IModerationService.cs` (legacy consumers) |
| **DI registration** | `ClubDoorman/Features/Moderation/01-ModerationFeature.cs` registers `IModerationPolicy`; `ClubDoorman/Services/Moderation/ModerationModule.cs` registers `IModerationService` via factory |
| **Input** | `Message`, `User` |
| **Output** | `ModerationResult` (record: `Action`, `Reason`, `Confidence?`) |
| **Actions** | `Allow`, `Delete`, `Ban`, `Report`, `RequireManualReview`, `RequireAiAnalysis` |
| **Forbidden deps** | Decision code must not call the real Telegram API, AI endpoints, DB, or cache directly. Telegram side effects belong in facade/effects/ban/messaging seams, not in pure decision tests. |
| **Test strategy** | Instantiate `ModerationPolicy` with mocked dependencies; feed `Message` objects; assert `ModerationResult.Action` and `Reason`. No Telegram API calls needed. |

---

## 6. AI Verdict Provider

**Seam:** `IAiChecks` / `IAiCascadeService` / `ISpamHamClassifier` / `IMimicryClassifier` — external AI & ML services

| Item | Detail |
|------|--------|
| **AI checks** | `ClubDoorman/Services/AI/IAiChecks.cs` ↔ `AiChecks.cs` — profile analysis, spam probability, cascade ML→AI analysis |
| **AI cascade** | `ClubDoorman/Services/AI/IAiCascadeService.cs` ↔ `AiCascadeService.cs` — orchestrates AI profile analysis and cascade analysis |
| **ML classifier** | `ClubDoorman/Services/AI/ISpamHamClassifier.cs` ↔ `SpamHamClassifier.cs` — ML.NET binary classifier for spam/ham |
| **Mimicry** | `ClubDoorman/Services/AI/IMimicryClassifier.cs` ↔ `MimicryClassifier.cs` — heuristic mimicry detection on first 3 messages |
| **Input** | `User` profile data, `Message` text, ML scores, message lists |
| **Output** | `SpamProbability` (Probability, Reason), `SpamPhotoBio`, `(bool Spam, float Score)`, `double` mimicry score |
| **External deps** | `tryAGI.OpenAI` (OpenRouter API, model `google/gemini-2.5-flash`), `Microsoft.ML` |
| **Fast mode** | `IAppConfig.GoldenBaselineMode` disables AI/ML calls; returns safe defaults |
| **Retry policy** | Polly retry + 30s timeout on AI HTTP calls |
| **Forbidden deps** | `IAiChecks` must not depend on Telegram SDK beyond `User`/`Message` types. `AiChecks` uses `ITelegramBotClientWrapper` only for photo download. |
| **Test strategy** | Mock `ITelegramBotClientWrapper` and `IAppConfig(GoldenBaselineMode=true)` to skip all external calls. For ML, set up a minimal dataset or mock `ISpamHamClassifier`. |

---

## 7. Captcha Flow

**Seam:** `ICaptchaService` — captcha challenge creation and validation

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/Captcha/ICaptchaService.cs` |
| **Implementation** | `ClubDoorman/Services/Captcha/CaptchaService.cs` |
| **DI module** | `ClubDoorman/Services/Captcha/CaptchaModule.cs` |
| **Input** | `CreateCaptchaRequest` (Chat, User, UserJoinMessage) |
| **Output** | `CaptchaInfo?` (null if captcha disabled for chat), validation result `bool` |
| **Key behavior** | Generates inline keyboard with emoji buttons; stores active captchas in `ConcurrentDictionary<string, CaptchaInfo>`; auto-bans on timeout (1.2 min); auto-unbans after 20 min |
| **External deps** | `ITelegramBotClientWrapper`, `IMessageService`, `IViolationTracker`, `IUserBanService` |
| **Forbidden deps** | CaptchaService is intentionally not migrated to `UserBanService` (requires auto-unban). Must not depend on AI services. |
| **Test strategy** | Mock all DI dependencies; verify `CreateCaptchaAsync` returns `CaptchaInfo`; verify `ValidateCaptchaAsync` returns correct results for valid/invalid keys. Use `CancellationToken` to avoid real timers. |

---

## 8. Ban / Mute / Delete Side Effects

**Seam:** `IUserBanService` — centralized ban operations

| Item | Detail |
|------|--------|
| **Interface** | `ClubDoorman/Services/UserBan/IUserBanService.cs` |
| **Implementation** | `ClubDoorman/Services/UserBan/UserBanService.cs` |
| **DI module** | `ClubDoorman/Services/UserBan/UserBanModule.cs` (stub — registration handled by `CaptchaModule` and other modules) |
| **Ban types** | `BanTypeEnum`: `LongName`, `Blacklist`, `AutoBan`, `ManualBan`, `ProfileBan`, `ChannelBan`, `CaptchaBan`, `RepeatedViolation` |
| **Input** | `Message`, `User`, `Chat`, `BanTypeEnum`, reason strings |
| **Output** | Telegram API side effects (ban, delete, notification); void return |
| **Key methods** | `BanUserForLongNameAsync`, `BanBlacklistedUserAsync`, `AutoBanAsync`, `AutoBanChannelAsync`, `HandleBlacklistBanAsync`, `TrackViolationAndBanIfNeededAsync`, `BanUserAsync` (overloads), `DeleteMessageByIdAsync` |
| **External deps** | `ITelegramBotClientWrapper`, `IMessageService`, `IUserFlowLogger`, `IViolationTracker`, `IAppConfig`, `IStatisticsService`, `GlobalStatsManager`, `IUserManager`, `IUserCleanupService` |
| **Forbidden deps** | Must not depend on AI services. Must not create its own `ITelegramBotClientWrapper`. |
| **Test strategy** | Mock `ITelegramBotClientWrapper`; invoke ban methods; verify `BanChatMemberAsync` / `DeleteMessageAsync` called with correct parameters. |

---

## 9. Messaging / Service-Chat Notifications

**Seam:** `IMessageService` / `IServiceChatDispatcher` / `ILogChatService` / `INotificationService` — notification delivery

| Item | Detail |
|------|--------|
| **Message service** | `ClubDoorman/Services/Messaging/IMessageService.cs` ↔ `MessageService.cs` — sends notifications to admin/log/user chats |
| **Service-chat dispatcher** | `ClubDoorman/Services/Messaging/IServiceChatDispatcher.cs` ↔ `ServiceChatDispatcher.cs` — routes notifications to admin-chat vs log-chat based on type |
| **Log chat service** | `ClubDoorman/Services/Messaging/ILogChatService.cs` ↔ `LogChatService.cs` — sends log notifications with inline action buttons |
| **Notification service** | `ClubDoorman/Services/Messaging/INotificationService.cs` — `DeleteAndReportMessage`, `DeleteAndReportToLogChat`, `DontDeleteButReportMessage`, `SendSuspiciousMessageWithButtons` |
| **Templates** | `ClubDoorman/Services/Messaging/MessageTemplates.cs` |
| **Input** | `NotificationData` subclasses (`AutoBanNotificationData`, `AiProfileAnalysisData`, `SuspiciousMessageNotificationData`, etc.), `Message`, `User`, `Chat` |
| **Output** | Telegram API calls (send message, forward, send photo, reply markup) |
| **Routing logic** | `IServiceChatDispatcher.ShouldSendToAdminChat` — type-based switch: `SuspiciousMessageNotificationData`, `AiProfileAnalysisData`, `AiDetectNotificationData`, `ErrorNotificationData` → admin-chat; everything else → log-chat |
| **External deps** | `ITelegramBotClientWrapper`, `IAppConfig`, `IMessageService`, `IUserBanService` |
| **Forbidden deps** | Must not depend on moderation policy directly. Must not depend on AI services. |
| **Test strategy** | Mock `ITelegramBotClientWrapper`; invoke notification methods; verify correct chat ID and message formatting. |

---

## 10. Logging / Golden Master / Event Publishing

**Seam:** `IGoldenMasterRecorder` / `IModerationEventPublisher` — deterministic recording and semantic event publishing

| Item | Detail |
|------|--------|
| **Golden Master recorder** | `ClubDoorman/Services/Logging/IGoldenMasterRecorder.cs` ↔ `GoldenMasterRecorder.cs` — records input/output snapshots with canonicalization and sampling |
| **Null recorder** | `NullGoldenMasterRecorder.cs` — no-op for tests / legacy constructors |
| **Event publisher** | `ClubDoorman/Services/Logging/IModerationEventPublisher.cs` ↔ `GoldenMasterModerationEventPublisher.cs` — bridges moderation events to Golden Master recorder |
| **Null publisher** | `NullModerationEventPublisher.cs` — no-op |
| **Reason code mapper** | `ClubDoorman/Services/Logging/ReasonCodeMapper.cs` — maps human-readable reason strings to stable `RuleCode` enum values |
| **Input** | `Update` (for input recording), `ModerationEvent` (for output publishing) |
| **Output** | JSON files on disk (`golden/<date>/<correlationId>.{input|sem}.json`) |
| **Key config** | `LoggingFlagsOptions` — `GoldenMasterEnabled`, `GoldenSampleRate`, `GoldenDeterministicIds`, `GoldenFixedDateFolder`, `GoldenBasePath` |
| **Data sanitization** | User IDs masked (`U` + last 4 digits), usernames hashed (SHA256 first 8 hex chars), text truncated to 160 chars, properties sorted for deterministic JSON |
| **Forbidden deps** | Must not depend on Telegram SDK beyond `Update` type. Must not have side effects when disabled (sample rate = 0). |
| **Test strategy** | Use `NullGoldenMasterRecorder` / `NullModerationEventPublisher` in tests. For real recorder tests, set `GoldenMasterEnabled=true`, `GoldenSampleRate=1.0`, `GoldenDeterministicIds=true`, and verify file output. |

---

## Cross-Seam Dependencies (Summary)

```
UpdateDispatcher -> IUpdateHandler implementations
MessageHandler -> IMessagePipeline -> ordered IMessageStep implementations
Pipeline/facade code -> moderation decision, captcha, ban, messaging, logging seams
Side-effect services -> ITelegramBotClientWrapper for real Telegram API calls
AI services -> external AI/ML providers behind IAiChecks / classifier interfaces
Logging seams -> disk output only when enabled
```

Most consumers depend on interfaces registered by DI modules. When a concrete type is used directly, tests should still isolate the seam by replacing its external dependencies rather than expanding broad test infrastructure.
