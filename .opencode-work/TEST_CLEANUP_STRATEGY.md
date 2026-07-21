# Test Cleanup Strategy

## Problem

The test suite is no longer just a safety net. It has become a maintenance tax.

Current pain:

- A small production change can require much more test fixing than code fixing.
- The bot owner is considering deleting tests entirely.
- This means parts of the suite are brittle, over-specified, duplicated, or testing the wrong seam.

The goal is not to preserve 942 tests. The goal is to keep only tests that provide useful confidence at acceptable maintenance cost.

## Goal

End state:

- Fewer tests.
- Tests target clear production seams.
- Every kept test answers: what real regression does this catch?
- Expensive brittle tests are deleted or quarantined.
- Useful broad tests are replaced by smaller seam tests when cheaper.
- Full suite is cheaper to maintain than production code changes.

## Target Shape

### Keep

- Pure policy/unit tests:
  - moderation decisions
  - routing decisions
  - config mapping
  - reason code mapping
- Seam tests:
  - `ModerationPolicy`
  - `ModerationFacade`
  - pipeline steps
  - `UserBanService`
  - `MessageService`
  - `CommandRouter`
- A small integration smoke set:
  - normal message allowed
  - spam delete path
  - captcha pending path
  - admin command path
  - ban side-effect path

### Delete Or Quarantine

- Golden-master tests that mostly verify mock call choreography.
- Tests asserting implementation details or call order without business value.
- Tests named `MessageHandler*` that actually test `UserBanService`, messaging, moderation, and Telegram together.
- Mutation coverage tests that duplicate real seam coverage.
- Tests requiring broad legacy factory setup to assert one trivial call.
- Tests that only prove mocks are wired to mocks.
- Tests around `Message.MessageId` that pass by asserting `0 == 0`.

## Decision

Do not migrate every legacy test.

Default for brittle legacy tests:

1. classify
2. delete or quarantine unless value is clear
3. rewrite only if the behavior is worth preserving

Migration is only for tests that protect real behavior.

## Tracks

### Track 1: Test Value Audit

Stop treating all existing tests as sacred.

For each suspicious file, classify each test or test group:

- `KEEP`: high signal, low maintenance
- `REWRITE`: useful behavior, bad seam
- `DELETE`: low signal, brittle, or duplicate
- `QUARANTINE`: maybe useful, too broad/flaky/expensive right now

First target files:

- `ClubDoorman.Test/Unit/Handlers/MessageHandlerGoldenMasterTests.cs`
- `ClubDoorman.Test/Unit/Handlers/MessageHandlerMutationCoverageTests.cs`
- `ClubDoorman.Test/Integration/MessageHandlerBanExceptionTests.cs`
- broad `ClubDoorman.Test/Integration/MessageHandlerBan*Tests.cs`
- old factory tests that only verify construction

Audit output per file:

- why it exists
- what real production regression it catches
- whether a cheaper seam test already covers it
- decision: keep, rewrite, delete, or quarantine

### Track 2: Delete Obvious Junk

Delete only when one of these is true:

- The test asserts mock choreography, not behavior.
- The test duplicates a narrower seam test.
- The test passes because setup and verify use the same broken value.
- The test is named for one component but exercises multiple unrelated seams.
- The test has no meaningful assertion.
- The test requires huge setup for trivial behavior.

Verification:

- focused test command if a replacement is added
- full suite after each deletion batch

### Track 3: Replace Broad Tests With Seam Tests

Example broad test smell:

- ban called
- delete called
- forward called
- statistics called
- logger called

Do not preserve this shape by default.

If the behaviors matter, split or replace with focused seam tests:

- `UserBanService` bans user
- `UserBanService` deletes join message
- `UserBanService` notifies log chat

If already covered elsewhere, delete instead.

### Track 4: Freeze Legacy Infra

Do not fix all legacy infra as a goal.

- Document as deprecated.
- Forbid new tests from using it.
- Do not expand it.
- Remove helper only when no tests depend on it.

Legacy paths currently known:

- `MessageHandlerTestFactory`
- `FakeServicesFactory`
- `TestKit.MessageHandlerBuilder`
- `TestKitAutoFixture.CreateMessageHandler`
- `MessageBuilder.WithMessageId`

### Track 5: Keep A Small Smoke Suite

Keep a small set of broad tests for owner confidence.

Target size: 5-10 broad smoke tests, not dozens.

Candidate smoke behaviors:

- bot receives normal message
- spam message deleted/reported
- captcha pending deletes message
- blacklist user banned
- admin `/check` command works

These tests should assert externally meaningful outcomes, not detailed mock choreography.

## Immediate Next Step

Do not continue `MessageId` cleanup blindly.

Next useful step:

Audit `MessageHandlerGoldenMasterTests.cs` for deletion/rewrite.

Expected output table:

| Test | Decision | Reason | Replacement |
| --- | --- | --- | --- |
| test name | keep/delete/rewrite/quarantine | why | existing or proposed seam test |

After review, delete a small batch of obvious junk.

## Practical Rule

For every test touched, ask:

If this test fails after a real code change, would the bot owner thank us or swear at us?

If the answer is swear, delete or rewrite.

## Subagent Use

Use subagents for read-only audit first:

- no edits
- classify tests
- identify duplicates
- propose deletes
- name replacement seam if needed

Do not let a subagent implement deletes until the classification is reviewed.

Implementation batches must be scoped to one seam and one decision type.

## Verification Commands

- Focused test after rewriting or replacing a file:
  - `dotnet test --no-restore --filter "FullyQualifiedName~<TestClassName>"`
- Full suite after deletion/rewrite batches:
  - `dotnet test --no-restore`
- Diff hygiene:
  - `git diff --check -- <touched-files>`

## Current Status Notes

- Full suite baseline has been passing at `942 passed / 12 skipped / 0 failed`.
- Some MessageId hardening was done, but this is no longer the main strategy.
- `MessageHandlerGoldenMasterTests.cs` should not be treated as mechanical MessageHandler cleanup; it mostly exercises `UserBanService` behavior.
- A previous larger subagent batch against `MessageHandlerGoldenMasterTests.cs` was rejected because it changed seam semantics.
