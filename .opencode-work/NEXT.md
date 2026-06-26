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

Title: Continue audit of dirty `MessageHandlerMutationCoverageTests.cs`, but do not edit until current owner of those changes is clear.

Mode: read first, classify, then delete only obvious low-value cases.

Goal: decide whether to delete, rewrite into `UserBanService` seam tests, or leave because user changes are in progress.

Allowed:

- Read the file and nearby seam tests before editing.
- Classify tests as keep/rewrite/delete/quarantine.
- Delete a small batch only when coverage is clearly duplicated or low-value.
- Prefer no replacement unless the behavior protects a real regression.
- If the file remains dirty from someone else, ask before editing or choose another clean target.

Not allowed:

- Add shared harness.
- Edit `TestInfrastructure` or `TestKit`.
- Change production code.
- Make `WithMessageId` obsolete-true.
- Preserve mutation-only tests by default.
- Touch `MessageHandlerMutationCoverageTests.cs` while it has unrelated dirty changes without explicit approval.

Suggested verification:

- Focused target file/class if still present.
- Relevant seam tests for replacements.
- Full suite after deletion/rewrite batch.

## Alternative Later Slice

Add shared harness only if another concrete migration shows the same setup pain repeats and the API can be tiny.
