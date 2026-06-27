# VSlice 7c — Anomaly Review Report

**Date**: 2026-06-27
**Slice**: VSlice 7c — review 7b audit anomalies before scaling

---

## Anomalies Reviewed

5 new anomalies from VSlice 7b (batches 0003, 0004, 0005).

---

## Per-Anomaly Analysis

### 1. Delete without replacement justification — `SendSuspiciousMessageWithButtons_WithSpecialCharacters_HandlesCorrectly`

| Field | Value |
|-------|-------|
| Batch | batch-0004 |
| Decision | delete |
| behavior_value | low |
| confidence | high |
| needs_human_review | false |
| Reason excerpt | "...No distinct bot contract exists for special-character handling..." |

**Verdict**: Decision is acceptable. Test is weak (mock_verify with `It.IsAny` on all params), no distinct bot contract.

**Problem**: Summarizer heuristic. Reason explains why no replacement is needed ("no distinct bot contract exists") but the keyword list didn't include this phrasing.

**Action**: Fixed summarizer keyword list. Added phrases: "no distinct bot contract", "no unique behavior", "fully covered", "already tested", "duplicate of", "redundant with", "redundant.", "same as", "no contract", "no behavioral contract", "no safety net", "no distinct".

**JSONL**: Leave as-is.

---

### 2. ID-sensitive kept — `BanUserForLongName_ValidChat_BansUserAndSendsNotification` (Modern)

| Field | Value |
|-------|-------|
| Batch | batch-0005 |
| Decision | keep |
| behavior_value | high |
| confidence | high |
| needs_human_review | false |
| Raw flag | `message_id.id_sensitive_suspected=true` |

**Verdict**: Decision is acceptable. Test uses `TK.CreateValidMessage()` / `TK.BuildMessage().AsValid().InChat(chat).Build()` — stable test helpers. The `id_sensitive_suspected` flag is triggered by static analysis detecting `.MessageId` access in test data construction, not by unreliable Telegram SDK `Message.MessageId` usage.

**Problem**: Prompt gap. No guidance existed for how to handle ID-sensitive tests. Subagent had no reason to flag or avoid `keep` for these.

**Action**: Added "ID-sensitive tests" section to audit prompt. Guidance: prefer `rewrite` (to `MessageEnvelope`) or `quarantine`. Only `keep` if test uses `MessageEnvelope` or `MessageId` access is incidental through stable test helpers.

**JSONL**: Leave as-is.

---

### 3. High-value delete — `BanUserForLongName_ValidChat_BansUserAndSendsNotification` (Legacy)

| Field | Value |
|-------|-------|
| Batch | batch-0005 |
| Decision | delete |
| behavior_value | high |
| confidence | high |
| needs_human_review | true |
| Reason excerpt | "Exact duplicate of the same test in UserBanServiceTests.Modern.cs..." |

**Verdict**: Decision is acceptable. Legacy test is an exact duplicate of the Modern version. `needs_human_review=true` is correctly set per the validator rule.

**Problem**: None. Validator rule (`behavior_value=high + decision=delete REQUIRES needs_human_review=true`) works correctly. Summarizer flag is appropriate for awareness.

**Action**: None needed.

**JSONL**: Leave as-is.

---

### 4. Delete without replacement justification — same legacy test

| Field | Value |
|-------|-------|
| Batch | batch-0005 |
| Decision | delete |
| behavior_value | high |
| confidence | high |
| needs_human_review | true |
| Reason excerpt | "No unique behavior to preserve; the contract is fully covered by the Modern test." |

**Verdict**: Decision is acceptable. Same test as anomaly 3. Reason explains why no replacement is needed ("fully covered by the Modern test").

**Problem**: Same summarizer heuristic gap as anomaly 1. Fixed by the same keyword list expansion.

**Action**: Fixed by summarizer patch (see anomaly 1).

**JSONL**: Leave as-is.

---

### 5. Quarantine count: 2

| Field | Value |
|-------|-------|
| Records | 2 |
| Tests | `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` (batch-0001.v2), `E2E_AI_Analysis_WithRealApi_ShouldWork` (batch-0005) |

**Verdict**: Informational. Both are real-API tests that are `[Ignore]`d. Quarantine is appropriate: too brittle to keep, too uncertain to delete.

**Problem**: None.

**Action**: None needed.

**JSONL**: Leave as-is.

---

## Changes Made

### 1. `tools/test-audit/summarize_audit.py`

Widened the "delete without replacement justification" keyword list from 12 phrases to 24 phrases. Added:
- "no distinct bot contract"
- "no unique behavior"
- "fully covered"
- "already tested"
- "duplicate of"
- "redundant with"
- "redundant."
- "same as"
- "no contract"
- "no behavioral contract"
- "no safety net"
- "no distinct"

Effect: Anomalies dropped from 8 to 6. Two false positives eliminated.

### 2. `.test-audit/audit-prompt.md`

Added "ID-sensitive tests" section after "Strong delete signals". Guidance:
- Tests with `id_sensitive_suspected=true` should prefer `rewrite` (to `MessageEnvelope`) or `quarantine`.
- `keep` only if test uses `MessageEnvelope` or `MessageId` access is incidental through stable test helpers.

### 3. `tools/test-audit/validate_audit.py`

No change needed. Validator already enforces `behavior_value=high + decision=delete REQUIRES needs_human_review=true` correctly.

---

## Summary

| Anomaly | Decision Quality | Root Cause | Fix |
|---------|-----------------|------------|-----|
| Delete without justification (special chars) | Acceptable | Summarizer heuristic | Summarizer keyword list |
| ID-sensitive kept (Modern) | Acceptable | Prompt gap | Prompt section added |
| High-value delete (Legacy) | Acceptable | None — working | None |
| Delete without justification (Legacy) | Acceptable | Summarizer heuristic | Summarizer keyword list |
| Quarantine count: 2 | Informational | None — working | None |

No audit decisions are unsafe. All decisions are defensible with the reasoning provided.

---

## Recommendation

**Proceed to next 3-batch run.**

Harness is stable. Prompt and summarizer patches will improve future batch quality:
- Summarizer will catch fewer false-positive "delete without justification" flags.
- Subagent will handle ID-sensitive tests more carefully.

No batches need to be re-audited.

---

## Acceptance Criteria Checklist

| # | Criterion | Status |
|---|-----------|--------|
| No new batch audits run | PASS |
| Anomaly review report exists | PASS — this file |
| No production/test code changed | PASS |
| Existing audit JSONL not manually edited | PASS |
| Prompt/validator/summarizer changes justified | PASS — see above |
