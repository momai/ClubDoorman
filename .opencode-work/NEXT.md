# Next Decision

## Question

Should the next implementation step add a shared harness or continue with targeted local/id-sensitive cleanup?

## Current Answer

Continue with targeted cleanup. Do not add shared harness yet.

## Why

- Repo rules warn against editing shared `TestInfrastructure` / `TestKit` unless the seam contract changes or current pain proves it.
- `MessageHandlerSemanticsTests` cleanup succeeded without shared harness.
- `MessageHandlerDeleteMessageLaterTests` MessageId assertion hardening succeeded with existing fake-client/envelope APIs.
- `MessageHandlerHandleUserMessageTests` captcha delete assertion hardening succeeded, but needed local `BotMock.DeleteMessage` pass-through because `CreateMessageHandlerWithFake` only covered some fake paths.
- `MessageHandlerBanBasicTests` delete assertion hardening succeeded with `_factory.CreateMessageHandlerWithFake(fakeClient)`.
- A delegated larger batch against `MessageHandlerGoldenMasterTests` was rejected because it crossed into `UserBanService` seam semantics and changed ban/forward/exception assertions.
- We still do not have enough repeated setup pain for a shared API.
- A shared harness now still risks creating another construction path.

## Candidate Next Slice

Title: Plan a UserBanService-specific MessageId cleanup.

Mode: plan/review first; no implementation until the seam approach is explicit.

Goal: decide how to harden delete assertions in `MessageHandlerGoldenMasterTests` without changing the `UserBanService` seam semantics or replacing all `BotMock` assertions with fake-client state.

Allowed:

- Treat `MessageHandlerGoldenMasterTests` as `UserBanService` seam tests despite the filename.
- Keep ban/forward/exception assertions on `BotMock` unless explicitly changing that seam test style.
- Consider whether delete assertions should remain mock-based with an explicit expected id source, or whether this file should be deferred.
- If delegating, use read-only inventory or one exact test only.

Not allowed:

- Add shared harness.
- Edit `TestInfrastructure` or `TestKit`.
- Delete tests.
- Change production code.
- Make `WithMessageId` obsolete-true.

Suggested verification:

- Planning only: no tests required.
- Implementation follow-up: focused `MessageHandlerGoldenMasterTests`, then full suite.

## Alternative Later Slice

Add shared harness only if another concrete migration shows the same setup pain repeats and the API can be tiny.
