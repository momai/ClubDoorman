# Test Infrastructure Strategy

This is an operational strategy for reducing test complexity. It is not a new architecture and it does not require a big-bang rewrite.

Goal: make small behavior changes testable through one obvious seam, without pulling half the bot into setup.

## Current Pain

The test suite has several competing ways to build the same world:

| Area | Current role | Problem |
|------|--------------|---------|
| `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs` | Large factory for `MessageHandler` and many old dependencies | Acts like a test DI container; pipeline changes cascade through it. |
| `ClubDoorman.Test/TestInfrastructure/FakeServicesFactory.cs` | Fake client/services and pipeline construction | Overlaps with `MessageHandlerTestFactory` and builds another world. |
| `ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs` | Fluent builder for `MessageHandler` | Another construction path; should stay thin or be retired. |
| `ClubDoorman.Test/TestKit/Infra/TestKitAutoFixture.cs` | AutoFixture customization for service construction | Hidden construction path for tests. |
| Local handler test setup | Some tests create pipeline steps inline | Duplicates production step lists and constructor dependencies. |

This makes tests hard to change because a small constructor, pipeline order, or dependency change can break several independent builders.

## Decision

Do not add another general-purpose test harness.

Use the nearest production seam for new or changed behavior tests. Keep existing heavy helpers only as legacy compatibility until a concrete test is migrated.

## Canonical Testing Paths

| Behavior under test | Preferred test seam | Preferred setup |
|---------------------|---------------------|-----------------|
| Pure text/filter/model logic | Concrete class or static method | Plain object initializers; no TestKit unless it improves readability. |
| Moderation decision | `ModerationPolicy` / `ModerationFacade` | Mock external dependencies; assert `ModerationResult.Action` and `Reason`. |
| Message pipeline behavior | Concrete `IMessageStep` or `MessagePipeline` with fake steps | Construct `MessageContext` directly; do not route through full `MessageHandler` unless routing is the behavior. |
| Handler routing | `IUpdateHandler.CanHandle` / `HandleAsync` | Minimal mocks; assert handler side effects only. |
| Ban/delete/mute side effects | `IUserBanService` | Mock `ITelegramBotClientWrapper`; verify Telegram calls and cleanup calls. |
| Telegram adapter behavior | `ITelegramBotClientWrapper` / `TelegramBotClientWrapper` | Mock SDK where possible; no real Telegram calls. |
| AI verdict behavior | `IAiChecks`, classifiers, or `IAiCascadeService` | `GoldenBaselineMode=true` or mocked classifiers/API wrappers. |
| Full bot behavior | Existing integration/E2E tests | Rare. Do not add new full-world tests for small business rules. |

## Helper Ownership

| Helper group | Status | Rule |
|--------------|--------|------|
| `TestKit/Builders/*` | Allowed for test data | Keep these as object builders only: `Message`, `User`, `Chat`, DTOs. Do not add service orchestration here. |
| `TestInfrastructure/*Factory.cs` | Legacy/shared factory contracts | Do not expand by default. Touch only when the seam contract changes or while migrating one concrete test. |
| `TestKit.MessageHandlerBuilder` | Transitional | Keep thin. Do not add new behavior emulation. Prefer step-level or service-level tests. |
| `FakeTelegramClient` | Useful fake | Use for Telegram side-effect recording. Do not duplicate its behavior in mocks unless the test needs strict call verification. |
| `FakeServicesFactory` | Legacy integration helper | Do not add new scenarios unless the test is explicitly broad integration. |

## Telegram Message Rule

`Telegram.Bot.Types.Message` is not a reliable mutable test DTO for every field. In this repo the important limitation is `Message.MessageId`: existing comments in `TestDataFactory.CreateValidMessageWithId`, `TestKit.Telegram`, and `FakeTelegramClient.CreateMessageFromEnvelope` state that `MessageId` remains `0` in normal test construction.

Use this rule:

| Test needs | Use | Do not use |
|------------|-----|------------|
| Text/moderation/AI logic where `MessageId` is irrelevant | Plain `new Message { From, Chat, Text, Date }` or object builder | `MessageEnvelope` just for style. |
| Delete/forward/reply/report assertions where `MessageId` matters | `MessageEnvelope` + `FakeTelegramClient.RegisterMessageEnvelope` / `CreateMessageFromEnvelope` / `WasMessageDeleted(envelope)` | `CreateValidMessageWithId()` or `MessageBuilder.WithMessageId()` as if they set `MessageId`. |
| Telegram side-effect assertions | `FakeTelegramClient` recorded actions (`DeletedMessages`, `SentMessages`, `BannedUsers`) | Reimplementing Telegram side effects in unrelated mocks. |

If a test fails because `MessageId == 0`, do not add reflection or another message builder. Move that test to the `MessageEnvelope` path or avoid asserting a Telegram SDK field that cannot be set reliably.

## Migration Rules

- Migrate one test file or one small scenario group at a time.
- Preserve the old test's behavior claim before changing setup.
- Prefer deleting local setup over adding helpers.
- Prefer moving assertions closer to a production seam over making `MessageHandlerTestFactory` smarter.
- Do not edit shared factories just to make one test prettier.
- Do not introduce new production interfaces only to simplify tests unless the production seam is already painful.

## First Migration Queue

1. `ClubDoorman.Test/Unit/Handlers/MessageHandlerHandleUserMessageTests.cs`
   - Move one scenario group from legacy handler adapter tests to concrete pipeline step tests.
   - Candidate seams: `CaptchaPendingStep`, `BanlistCheckStep`, `BaseModerationStep`, `FinalModerationActionStep`.

2. `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs`
   - Remove inline pipeline construction for one scenario by using a narrower seam or one shared local helper inside the file.

3. `ClubDoorman.Test/Integration/MessageHandlerBan*.cs`
   - For ban/delete assertions, decide whether the behavior belongs at `UserBanService`, a pipeline step, or full handler routing.

4. `ClubDoorman.Test/TestKit/TestKit.Builders.cs` and `ClubDoorman.Test/TestKit/Builders/MessageBuilder.cs`
   - Resolve duplicate `MessageBuilder` definitions only after a focused inventory of usages.
   - Do not remove one until all call sites are known.

5. `CreateValidMessageWithId()` / `WithMessageId()` call sites
   - Replace only call sites where the test truly relies on message id.
   - Use `MessageEnvelope` there; leave id-irrelevant tests alone.

## What Success Looks Like

- A new moderation rule needs one moderation seam test, not a full handler integration setup.
- A new pipeline branch needs one step test, not a full `MessageHandler` world.
- A Telegram delete assertion uses one explicit `MessageEnvelope` path.
- `TestKit` stays focused on data builders, not service orchestration.
- Existing legacy factories shrink because tests move away from them, not because they are rewritten globally.
