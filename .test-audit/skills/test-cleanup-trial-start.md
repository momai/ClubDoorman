# Test Cleanup Trial Start

Use `.test-audit/skills/test-cleanup-orchestrator.md`.

Run trial mode.

## Goal

Apply one tiny delete-obvious cleanup slice to verify the implementation harness.

This is a trial, not iterative cleanup.

## Mode

`delete-obvious`

## Limits

* max workers: 1
* files to modify: 1
* tests to delete: 2–5
* rewrite tests: 0
* production code changes: 0
* audit artifact changes: 0
* queue changes: 0

## Selection

Use:

* `.test-audit/queues/delete-by-file.md`
* `.test-audit/queues/delete-obvious.md`
* `.test-audit/audit-normalized/current-audit.jsonl`

Select exactly one safe file with 2–5 obvious-delete candidates.

Prefer:

* DTO/property tests;
* factory smoke tests;
* mock self-tests;
* tests that only verify object creation;
* tests that only verify mock setup.

Avoid:

* files containing `AdminDisplayName_WithUsername_ReturnsUsername`;
* human-review candidates;
* rewrite candidates in the same local region;
* keep candidates in the same local region;
* high-value behavior;
* MessageHandler broad integration tests;
* real API / E2E tests;
* DI registration tests;
* ID-sensitive tests.

## Required flow

1. Select one file and 2–5 test IDs.
2. Cross-check selected IDs against normalized audit and delete-obvious queue.
3. Invoke `qwen-implementer` using `.test-audit/skills/test-cleanup-worker.md`.
4. Worker deletes only selected tests.
5. Inspect git diff.
6. Run validation.

Preferred validation:

```bash id="xeolzd"
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --no-restore
```

7. Create report:

`.test-audit/reports/cleanup-trial-v9a.md`

8. Update run state:

`.test-audit/reports/test-cleanup-run-state.md`

## Report must include

* selected file;
* selected test IDs;
* audit reasons;
* why this was safe for trial;
* qwen-implementer task/session ID if available;
* exact changes made;
* validation command;
* validation result;
* git diff summary;
* whether production code changed;
* whether non-selected tests changed;
* recommendation:

  * proceed to iterative delete-obvious mode;
  * adjust skill;
  * stop.

## Stop condition

Stop after the trial.

Do not continue to another file.
Do not run iterative mode.
Do not rewrite tests.
Do not delete more than selected tests.
