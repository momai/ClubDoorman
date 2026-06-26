# Next Decision

## Question

What is the next cleanup target after deleting weak MessageHandler-named ban/demo files?

## Current Answer

Continue value-based deletion/rewrite audit. Do not add shared harness yet.

## Why

- The current owner pain is test maintenance cost, not missing test helpers.
- Deleted `MessageHandlerGoldenMasterTests.cs`, `MessageHandlerBanExceptionTests.cs`, `MessageHandlerMutationCoverageTests.cs`, and `MessageHandlerBanAdvancedTests.cs` after audit.
- Focused seam tests passed after each deletion.
- Current full suite: `903 passed / 12 skipped / 0 failed`.
- The suite can shrink safely when brittle broad tests duplicate seam tests.
- A shared harness now still risks creating another construction path instead of reducing test tax.

## Candidate Next Slice

Title: Audit `MessageHandlerBanTests.cs` before touching it.

Mode: read first, classify, then delete only obvious low-value cases.

Goal: split decisions by test group, because this file mixes real routing checks, mocked side-effect checks, ignored AI tests, and repeated-violation scenarios.

Allowed:

- Read the file and nearby seam tests before editing.
- Classify tests as keep/rewrite/delete/quarantine.
- Delete a small batch only when coverage is clearly duplicated or low-value.
- Prefer no replacement unless the behavior protects a real regression.
- If a candidate file is dirty from someone else, inspect first and ask before overwriting unclear changes.

Not allowed:

- Add shared harness.
- Edit `TestInfrastructure` or `TestKit`.
- Change production code.
- Make `WithMessageId` obsolete-true.
- Preserve mutation-only tests by default.
- Touch unrelated dirty files without explicit approval.

Suggested verification:

- Focused target file/class if still present.
- Relevant seam tests for replacements.
- Full suite after deletion/rewrite batch.

## Alternative Later Slice

Add shared harness only if another concrete migration shows the same setup pain repeats and the API can be tiny.
