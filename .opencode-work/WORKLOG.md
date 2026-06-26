# OpenCode Worklog

Purpose: durable handoff context for test infrastructure reset work. Keep factual. Update after each slice.

## Current Epic

Test Cleanup: reduce test maintenance tax by deleting, quarantining, or rewriting brittle low-value tests around clear seams.

## Operating Model

- GPT-5.5/OpenCode is orchestrator/senior.
- Qwen is used only for bounded tasks or approved implementation slices.
- For Qwen implementation, load/use `implement-slice` and pass the full slice contract in the task prompt.
- Do not let Qwen design shared infra, delete broad tests, change production behavior, or do sweeping refactors.
- Prefer small verified slices over a big session.

## Repo Rules In Force

- Do not edit `ClubDoorman.Test/TestInfrastructure/` or `ClubDoorman.Test/TestKit/` unless seam contract changes or the slice explicitly justifies it.
- Run focused tests first, full suite at the end.
- Do not use naked `Message.MessageId` as proof of real IDs in id-sensitive tests.
- Use `MessageEnvelope` + `FakeTelegramClient.RegisterMessageEnvelope` + `WasMessageDeleted` for id-sensitive scenarios.
- Never revert user/unrelated changes.

## Baseline

- Branch: `next`.
- Baseline command: `dotnet test`.
- Baseline result: `942 passed / 12 skipped / 0 failed`.
- Existing dirty/untracked files before this work:
  - `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs`
  - `ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs`
  - `ClubDoorman.Test/Unit/Handlers/MessageHandlerMutationCoverageTests.cs`
  - `AGENTS.md`
  - `docs/architecture-seams.md`
  - `docs/test-infra-strategy.md`
  - `docs/test-layer-map.md`

## Completed

### Slice 1: Baseline And Inventory

- No files changed.
- Ran `git status --short`, `git branch --show-current`, `dotnet test`.
- Inventory facts:
  - `new MessageHandler(` in `ClubDoorman.Test/**/*.cs`: 15 matches.
  - Direct test construction outside shared helpers:
    - `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs`: 8 direct constructions.
    - `ClubDoorman.Test/Integration/MessageHandlerBanExceptionTests.cs`: 1 direct construction.
  - Shared helper constructions:
    - `MessageHandlerTestFactory.cs`: 3.
    - `FakeServicesFactory.cs`: 1.
    - `TestKitAutoFixture.cs`: 1.
    - `TestKit.MessageHandlerBuilder.cs`: 1.
  - `FakeServicesFactory` usage concentrated in `ClubDoorman.Test/Integration/AiAnalysisTests.cs`.
  - `TestKitAutoFixture.CreateMessageHandler` direct test usage in `MessageHandlerStatsCommandTests.cs`.
  - Correct `MessageEnvelope` path exists and is used in `MessageHandlerMutationCoverageTests.cs`, `MessageHandlerFakeTests.cs`, and `MessageHandlerBanTests.cs`.

### Slice 2: Test Policy README

- Edited `ClubDoorman.Test/README.md`.
- Added new-test policy: use nearest seam, not broad helper.
- Marked legacy construction paths:
  - `MessageHandlerTestFactory`
  - `FakeServicesFactory`
  - `MessageHandlerBuilder`
  - `TestKitAutoFixture.CreateMessageHandler`
  - `MessageBuilder.WithMessageId`
- Changed `MessageHandlerTestFactory` wording from canonical/default to legacy for existing tests.
- Verification:
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.

### Slice 5 Partial: MessageId Hardening

- Edited `ClubDoorman.Test/TestKit/TestKit.Builders.cs`.
- Added `[Obsolete("Message.MessageId remains 0. Use MessageEnvelope with FakeTelegramClient for id-sensitive scenarios.", false)]` to `MessageBuilder.WithMessageId`.
- No behavior change.
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~TestKit"`: `18 passed`.
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.
  - `git diff --check -- ClubDoorman.Test/README.md ClubDoorman.Test/TestKit/TestKit.Builders.cs`: passed.

### Slice 3 Local: `MessageHandlerSemanticsTests` Construction Cleanup

- Edited only `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs`.
- Did not add shared harness.
- Did not edit `TestInfrastructure` or `TestKit`.
- Changed local `CreateHandler` helper to accept optional injected mocks:
  - `Mock<ICaptchaService>? captchaServiceMock`
  - `Mock<IModerationFacade>? moderationFacadeMock`
  - `Mock<IAiCascadeService>? aiCascadeMock`
- Defaults are applied only when the corresponding injected mock is absent.
- Replaced duplicated manual pipeline/handler setup in captcha, already-approved, AI profile, and moderated action semantics tests.
- Reduced direct `new MessageHandler(` in `MessageHandlerSemanticsTests.cs` from 8 to 1; remaining one is the local `CreateHandler` factory.
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~MessageHandlerSemanticsTests"`: `16 passed`.
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.
  - `git diff --check -- ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs`: passed.

### Slice 6 Local: `DeleteMessageLater` MessageId Assertion Hardening

- Edited only `ClubDoorman.Test/Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs`.
- Did not add shared harness.
- Did not edit production code.
- Migrated `DeleteMessageLater_WithShortTimeout_InvokesDelete` from mock verification using `message.MessageId` to fake-client envelope assertion:
  - creates `FakeTelegramClient`
  - creates `MessageEnvelope` with explicit `messageId: 12345`
  - creates `Message` through `TestKitTelegram.CreateMessageFromEnvelope`
  - creates handler with `_factory.CreateMessageHandlerWithFake(fakeClient)`
  - asserts `fakeClient.WasMessageDeleted(envelope)`
- Left failure/cancel tests unchanged because they intentionally verify mock behavior, not real message-id mapping.
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~MessageHandlerDeleteMessageLaterTests"`: `8 passed`.
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.
  - `git diff --check -- ClubDoorman.Test/Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs .opencode-work`: passed.

### Slice 7: Remaining MessageId Inventory And Captcha Assertion Hardening

- Used `explore` subagent for read-only inventory. No edits/tests by subagent.
- Inventory classified remaining MessageHandler-related `message.MessageId` usages:
  - best small target: `MessageHandlerHandleUserMessageTests.HandleUserMessageAsync_WithUserInCaptcha_DeletesMessageAndReturns`
  - next candidate: `Integration/MessageHandlerBanBasicTests.DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage`
  - defer: `MessageHandlerGoldenMasterTests` because it mostly exercises `UserBanService`, not `MessageHandler`
  - `MessageHandlerSendSuspiciousMessageTests` usages are mostly forwarding/plumbing and notification DTO fields
- Edited only `ClubDoorman.Test/Unit/Handlers/MessageHandlerHandleUserMessageTests.cs`.
- Migrated captcha deletion assertion from `BotMock.Verify(... message.MessageId ...)` to fake-client envelope assertion:
  - creates `FakeTelegramClient`
  - creates `MessageEnvelope` with explicit `messageId: 12345`
  - creates `Message` through `TestKitTelegram.CreateMessageFromEnvelope`
  - locally wires `BotMock.DeleteMessage` to `fakeClient.DeleteMessage`
  - asserts `fakeClient.WasMessageDeleted(envelope)`
- Initial focused run failed because `CreateMessageHandlerWithFake` pass-through covered `DeleteMessageWithOutcomeAsync`, while `CaptchaPendingStep` calls `DeleteMessage`. Fixed locally with explicit `BotMock.DeleteMessage` pass-through.
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~MessageHandlerHandleUserMessageTests"`: first failed, then passed `11 passed` after local pass-through fix.
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.
  - `git diff --check -- ClubDoorman.Test/Unit/Handlers/MessageHandlerHandleUserMessageTests.cs .opencode-work`: passed.

## Findings

- `MessageHandlerBanExceptionTests` is not a good first migration target:
  - It manually builds a 2-step pipeline.
  - It mocks `_messageServiceMock`, but that mock is not passed into the handler/pipeline.
  - Assertions are weak: mostly `logger.Log(...) Times.AtLeastOnce`.
  - Treat as candidate for later characterization or deletion decision, not canonical harness example.

- `MessageHandlerSemanticsTests` was a good local-cleanup target:
  - It had 8 direct `new MessageHandler` constructions.
  - It already had a local `CreateHandler` helper and real pipeline setup.
  - Local cleanup reduced duplication without adding shared harness.
- `DeleteMessageLater` showed the existing fake-client/envelope path can harden specific MessageId assertions without adding shared harness.
- `MessageHandlerHandleUserMessageTests` showed `CreateMessageHandlerWithFake` is not a complete fake-client harness for all delete paths; for `DeleteMessage`, local pass-through setup is still needed.

### Slice 8: Batched Attempt Rejected, `MessageHandlerBanBasicTests` Hardened

- Tried a larger delegated batch for delete-only `message.MessageId` assertions in:
  - `ClubDoorman.Test/Integration/MessageHandlerBanBasicTests.cs`
  - `ClubDoorman.Test/Unit/Handlers/MessageHandlerGoldenMasterTests.cs`
- Subagent exceeded the slice contract:
  - changed `UserBanService` seam setup from `BotMock` to `FakeTelegramClient`
  - changed ban/forward/exception assertions, not only delete assertions
  - touched forwarding behavior despite explicit stop rule
  - introduced broad fake-client assumptions and line-ending noise
- Rejected that batch and restored `MessageHandlerGoldenMasterTests.cs` to no diff.
- Kept no subagent changes directly.
- Implemented one safe manual edit in `MessageHandlerBanBasicTests.DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage`:
  - creates `FakeTelegramClient`
  - creates `MessageEnvelope` with explicit `messageId: 12345`
  - creates `Message` through `TestKitTelegram.CreateMessageFromEnvelope`
  - uses `_factory.CreateMessageHandlerWithFake(fakeClient)` for the `DeleteMessageWithOutcomeAsync` path
  - asserts `fakeClient.WasMessageDeleted(envelope)`
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~MessageHandlerBanBasicTests"`: `1 passed`.
  - `git diff --check -- ClubDoorman.Test/Integration/MessageHandlerBanBasicTests.cs ClubDoorman.Test/Unit/Handlers/MessageHandlerGoldenMasterTests.cs`: passed.
  - `dotnet test --no-restore`: `942 passed / 12 skipped / 0 failed`.

## Subagent Lessons

- Larger batches are only safe when the subagent is confined to one seam and one already-proven helper path.
- Do not delegate `MessageHandlerGoldenMasterTests` as a mechanical MessageHandler cleanup: it mostly exercises `UserBanService` and broad fake-client replacement changes test semantics.
- For `UserBanService` delete assertions, first design/review a narrow seam-specific approach; do not mix ban, forward, delete, and exception assertions in one batch.

## Current Recommendation

Do not add shared harness yet. Do not continue mechanical MessageId cleanup blindly.

Default for brittle legacy tests is now classify, then delete/quarantine unless value is clear. Rewrite only when the behavior is worth preserving.

### Slice 9: Delete `MessageHandlerGoldenMasterTests`

- Committed previous work first:
  - commit `b9cf9df test: document cleanup strategy`
- Audited `ClubDoorman.Test/Unit/Handlers/MessageHandlerGoldenMasterTests.cs`.
- Decision: delete the whole file.
- Reasons:
  - file name says `MessageHandler`, but tests directly call `UserBanService`
  - `_messageHandler` setup exists but is unused by the tests
  - most assertions are brittle mock choreography and log-string checks
  - many delete assertions use `message.MessageId`, which is unreliable in this repo
  - main behaviors are already covered by `Unit/Services/UserBanServiceTests.cs` and `Unit/Services/UserBanServiceTests.Modern.cs`
  - missing unique coverage was mostly error/log choreography, not worth preserving before deletion
- Verification:
  - `dotnet test --no-restore --filter "FullyQualifiedName~UserBanServiceTests"`: `24 passed`
  - `dotnet test --no-restore`: `927 passed / 12 skipped / 0 failed`
- Net effect:
  - removed 15 brittle golden-master tests
  - suite total reduced from `942 passed / 12 skipped` to `927 passed / 12 skipped`
  - no production code changed

Next recommended target: audit `MessageHandlerMutationCoverageTests.cs` or `MessageHandlerBanExceptionTests.cs` with the same delete/rewrite/quarantine lens.

## Risks / Unknowns

- `ClubDoorman.Test/README.md` existed before this work and may be untracked in git state; preserve user intent.
- Some docs/examples still mention legacy paths; not fully cleaned.
- Obsolete warning is `false`; making it `true` is a separate HITL decision.
