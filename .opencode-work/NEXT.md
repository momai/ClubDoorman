# Next Decision

## Question

What is the next cleanup target after deleting `MessageHandlerGoldenMasterTests.cs`?

## Current Answer

Continue value-based deletion/rewrite audit. Do not add shared harness yet.

## Why

- The current owner pain is test maintenance cost, not missing test helpers.
- `MessageHandlerGoldenMasterTests.cs` was deleted successfully after audit.
- Focused `UserBanServiceTests` passed after deletion.
- Full suite passed after deletion: `927 passed / 12 skipped / 0 failed`.
- The suite can shrink safely when brittle broad tests duplicate seam tests.
- A shared harness now still risks creating another construction path instead of reducing test tax.

## Candidate Next Slice

Title: Audit `MessageHandlerMutationCoverageTests.cs` for delete/rewrite/quarantine.

Mode: read first, classify, then delete only obvious low-value cases.

Goal: remove tests that duplicate seam coverage, assert implementation details, or only protect mutation-score artifacts.

Allowed:

- Read the file and nearby seam tests before editing.
- Classify tests as keep/rewrite/delete/quarantine.
- Delete a small batch only when coverage is clearly duplicated or low-value.
- Prefer no replacement unless the behavior protects a real regression.

Not allowed:

- Add shared harness.
- Edit `TestInfrastructure` or `TestKit`.
- Change production code.
- Make `WithMessageId` obsolete-true.
- Preserve mutation-only tests by default.

Suggested verification:

- Focused target file/class if still present.
- Relevant seam tests for replacements.
- Full suite after deletion/rewrite batch.

## Alternative Later Slice

Add shared harness only if another concrete migration shows the same setup pain repeats and the API can be tiny.
