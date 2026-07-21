# Checklists

## Before Any Slice

- Confirm exact slice goal and non-goals.
- Check whether task is small enough to do directly or needs Qwen.
- If using Qwen for implementation, load/use `implement-slice` and pass bounded task text.
- Inspect relevant files before editing.
- Check worktree for unrelated changes; do not revert them.

## When To Use Qwen

Use Qwen for:

- grep/count/table inventory.
- mechanical migration of 3-5 tests after pattern is established.
- finding usages of old helpers.
- finding id-sensitive tests.
- finding weak assertions.

Do not use Qwen for:

- shared harness design.
- deleting broad tests.
- production behavior changes.
- DI/pipeline rewrites.
- deciding public test API changes.

## Test Infra Guardrails

- New tests should use the nearest production seam.
- `MessageHandler` tests should stay thin orchestration tests.
- Pipeline steps should be tested directly when behavior belongs there.
- Command behavior should be tested through `CommandRouter` / command handlers.
- Ban/delete/report effects should be tested through service seams or `FakeTelegramClient`.
- Id-sensitive scenarios must use `MessageEnvelope` and `FakeTelegramClient` envelope APIs.
- Do not expand `MessageHandlerTestFactory`, `FakeServicesFactory`, `MessageHandlerBuilder`, or `TestKitAutoFixture.CreateMessageHandler` for new scenarios.

## Verification Order

- Run focused command first.
- Run full suite at end of broad/shared changes.
- For docs-only changes, `git diff --check` is enough unless a full check was already cheap/useful.
- For `TestKit`/shared helper changes, run focused TestKit tests and full `dotnet test --no-restore`.

## Review Checklist

- Did this slice change only intended files?
- Did it avoid production behavior changes?
- Did it avoid broad formatting churn?
- Are assertions still behavioral, not just test-count preservation?
- Are MessageId assertions using the envelope path when real ID matters?
- Did Qwen stay inside the approved slice if used?

## Stop And Ask User

- Deleting broad tests without replacement.
- Making obsolete warnings into errors.
- Changing production pipeline/DI/handler behavior.
- Changing public test APIs beyond warning-only annotations.
- Baseline or focused checks fail for non-obvious reasons.
