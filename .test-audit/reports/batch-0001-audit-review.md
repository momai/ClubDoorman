# Batch 0001 Audit Review

## Summary

- **Batch**: batch-0001.json (7 tests)
- **Audited records**: 7
- **Validator result**: PASS (0 errors, 0 warnings)
- **Model**: Qwen3.6-27B (local, via opencode)

## Counts by Decision

| Decision | Count |
|----------|-------|
| delete   | 6     |
| rewrite  | 1     |
| keep     | 0     |
| quarantine | 0  |
| unknown  | 0     |

## Counts by Assertion Type

| Assertion Type        | Count |
|-----------------------|-------|
| fake_client_tracking  | 5     |
| does_not_throw        | 1     |
| no_meaningful_assertion | 1   |

## Counts by Actual Seam

| Actual Seam | Count |
|-------------|-------|
| Multiple    | 6     |
| MessageHandler | 1  |

## 3 Reasonable Examples

1. **`E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis`** → delete
   - Correct: assertion is `NotBeNull` on a handler just created. Console.WriteLine debug output. Zero signal. Clear delete.

2. **`HandleAsync_ModerationServiceError_LogsAndContinues`** → rewrite
   - Correct: the error-handling behavior is genuinely useful, but tested through 4 mock setups and legacy factory. Rewrite at MessageHandler seam with simpler arrangement is the right call.

3. **`E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto`** → delete
   - Correct: message has no photo field set, only Text. Assertions don't verify photo handling. Name vs reality mismatch.

## 3 Questionable Examples

1. **`E2E_AI_Analysis_Channel_ShouldNotShowCaptcha`** → delete
   - The "no captcha" behavior for channels IS a real contract. Deleting this test removes the only test of that channel-specific behavior. A rewrite to test at the CaptchaService or pipeline step seam might be better than delete. However, the current test doesn't verify the behavior, so delete is defensible.

2. **`E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis`** → delete
   - Same concern: deduplication of AI analysis is a real behavior. If the pipeline actually implements this, deleting the broken test means no test covers it. A rewrite at the pipeline/AI checks seam would be safer. But the current test is useless.

3. **`E2E_AI_Analysis_OperationOrder_ShouldBeCorrect`** → delete
   - Operation ordering is a meaningful pipeline contract. Deleting the broken test leaves no coverage. Rewrite at the pipeline seam would be better. But again, current test has zero ordering assertions.

**Pattern**: tests 2-7 are all from the same file (`AiAnalysisTests.cs`) and share the same anti-pattern: `NotBeNull` + `SentMessages.NotBeEmpty`. The delete decisions are consistent and correct for the current state. The questionability is about whether these behaviors should be re-tested at the correct seam (rewrite vs delete).

## Usability Assessment

**The local model is usable for more batches.** The canary run demonstrates:

- Prompt produces valid JSONL that passes the validator
- Decisions are consistent with the schema's guidance
- Reasons are specific and reference actual code patterns
- No hallucinated fields or enum values

**Caveats**:
- batch-0001 is unusually one-sided (6 delete, 1 rewrite, 0 keep). This may reflect the batch composition (mostly AiAnalysisTests) rather than model bias.
- The model may lean toward delete for integration tests with weak assertions. This is correct per the guidance but worth monitoring.

## Recommended Changes Before Scaling

1. **Prompt**: Add explicit guidance that "delete" means the behavior has NO useful test anywhere. If the behavior is important but tested poorly, prefer "rewrite" with a replacement seam. This would address the questionable examples above.

2. **Source excerpts**: The current 2000-char truncation may cut off assertions. Consider increasing excerpt length or including the assertion block specifically.

3. **Schema**: Consider adding a `related_tests` field to note when multiple tests in the same class share the same anti-pattern, to help batch decisions.

4. **Batch composition**: Mix test types within batches to avoid one-sided results. batch-0001 is 1 MessageHandler test + 6 AiAnalysisTests.
