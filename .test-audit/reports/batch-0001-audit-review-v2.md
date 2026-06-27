# Batch 0001 Audit Review v2

## Command Used

```
# Prompt patched in-place, then audit produced by local model (Qwen3.6-27B via opencode)
# Equivalent to:
# cat .test-audit/audit-prompt.md .test-audit/batches/batch-0001.json | <local-model> > .test-audit/audit/batch-0001.audit.v2.jsonl
```

## Validator Result

```
validated_files=1 audited_records=7 unique_audited_ids=7 raw_ids=847 warnings=0 errors=0
```

PASS — 0 errors, 0 warnings.

## Audited Record Count

7 records (all tests in batch-0001).

## v1 Decision Counts

| Decision   | Count |
|------------|-------|
| delete     | 6     |
| rewrite    | 1     |
| keep       | 0     |
| quarantine | 0     |
| unknown    | 0     |

## v2 Decision Counts

| Decision   | Count |
|------------|-------|
| rewrite    | 6     |
| quarantine | 1     |
| delete     | 0     |
| keep       | 0     |
| unknown    | 0     |

## Comparison Table

| Test Name | v1 Decision | Reference Decision | v2 Decision | Match | Note |
|-----------|-------------|-------------------|-------------|-------|------|
| `HandleAsync_ModerationServiceError_LogsAndContinues` | rewrite | rewrite | rewrite | yes | Unchanged. Error-handling contract correctly preserved. |
| `E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis` | delete | rewrite | rewrite | yes | Fixed. v1 missed that first-message AI analysis is a core bot contract. |
| `E2E_AI_Analysis_MessageHandler_ShouldSendNotification` | delete | rewrite | rewrite | yes | Fixed. v1 missed that notification delivery is owner-visible. |
| `E2E_AI_Analysis_Channel_ShouldNotShowCaptcha` | delete | rewrite | rewrite | yes | Fixed. v1 missed that channel captcha bypass is a real contract. |
| `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` | delete | quarantine | quarantine | yes | Fixed. v1 deleted without considering deduplication behavior value. |
| `E2E_AI_Analysis_OperationOrder_ShouldBeCorrect` | delete | rewrite | rewrite | yes | Fixed. v1 missed that pipeline ordering is a meaningful contract. |
| `E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto` | delete | rewrite | rewrite | yes | Fixed. v1 missed that photo-based AI analysis matters to owner. |

**Match rate: 7/7 (100%)**

## Remaining Questionable Decisions

None. All v2 decisions align with reference expectations.

The `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` quarantine decision is the only non-rewrite. This is appropriate: the deduplication seam is genuinely unclear (could be pipeline step, AI checks, or user manager cache), and the current test provides no evidence the pipeline actually implements deduplication. Quarantine is the correct conservative call.

## Prompt Calibration Assessment

The patched policy worked exactly as intended:

- **v1 had 6 delete decisions** — the model confused weak assertions with low behavior value.
- **v2 has 0 delete decisions** — the model now correctly separates test quality from contract value.
- **v2 has 6 rewrite + 1 quarantine** — weak assertions now map to rewrite (when contract is clear) or quarantine (when seam is unclear).

The key change: the new policy forced the model to ask "would the bot owner care if this behavior regressed?" before choosing delete. This flipped 6 delete decisions to rewrite/quarantine.

## Recommendation

**Scale to 3-batch calibration pack.**

The prompt calibration is confirmed working on batch-0001. Before scaling to full audit:

1. Run v2 prompt on 2 more batches (batch-0002 and batch-0003) to verify the policy generalizes beyond this specific file (`AiAnalysisTests.cs`).
2. Monitor whether the model now overcorrects toward rewrite for genuinely low-value tests.
3. If v2 decisions on batches 2-3 look reasonable, proceed to full audit with the patched prompt.

No further prompt adjustments needed at this point.
