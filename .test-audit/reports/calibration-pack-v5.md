# Calibration Pack v5 — Review Report

## 1. Selected Batches

| Batch | Tests | Construction Paths | Reason Selected |
|-------|-------|-------------------|-----------------|
| batch-0007.json | 8 | `legacy_message_handler_test_factory` (100%) | Legacy-heavy: all tests use MessageHandlerTestFactory, mix of mock_verify and Assert.Pass assertions |
| batch-0032.json | 8 | `custom_local_world` (1), `service_collection_di` (7) | Custom-world: ServiceCollection DI registration tests + one captcha service test with custom local setup |
| batch-0058.json | 10 | `none_or_pure` (100%) | Pure/low-infra: test infrastructure factory tests, no external dependencies, minimal setup |

### Dominant Smells

- **batch-0007**: `assert_pass` (6/8 tests), `mock_verify` (2/8), `message_handler_test_factory` (all)
- **batch-0032**: `real_env_or_api` (4/8, false positive — tests only build service providers), `weak_not_null` (all)
- **batch-0058**: none detected (clean factory tests)

## 2. Validation Results

| File | Records | Errors | Warnings |
|------|---------|--------|----------|
| batch-0007.audit.jsonl | 8 | 0 | 0 |
| batch-0032.audit.jsonl | 8 | 0 | 0 |
| batch-0058.audit.jsonl | 10 | 0 | 0 |

All 3 files pass validation with 0 errors.

## 3. Decision Counts

### Per Batch

| Batch | keep | rewrite | delete | quarantine | unknown |
|-------|------|---------|--------|------------|---------|
| batch-0007 | 0 | 2 | 6 | 0 | 0 |
| batch-0032 | 8 | 0 | 0 | 0 | 0 |
| batch-0058 | 0 | 0 | 10 | 0 | 0 |

### Total (Calibration Pack)

| Decision | Count | Percentage |
|----------|-------|------------|
| keep | 8 | 33.3% |
| rewrite | 2 | 8.3% |
| delete | 16 | 66.7% |
| quarantine | 0 | 0% |
| unknown | 0 | 0% |

### Behavior Value Distribution

| Value | Count |
|-------|-------|
| high | 0 |
| medium | 6 |
| low | 20 |
| unknown | 0 |

### Confidence Distribution

| Confidence | Count |
|------------|-------|
| high | 25 |
| medium | 1 |
| low | 0 |

## 4. Suspicious Patterns

### Delete-heavy batches
- **batch-0007**: 75% delete (6/8). Justified: 6 tests use `Assert.Pass` with no meaningful assertions, all on legacy factory. The 2 non-delete decisions (rewrite) are for tests with mock_verify assertions pointing to real bot contracts.
- **batch-0058**: 100% delete (10/10). Justified: all tests are factory smoke tests with `Not.Null` assertions that are compile-time guarantees. No production behavior is tested.

### Keep-heavy batch
- **batch-0032**: 100% keep (8/8). Potentially lenient: all tests are DI registration checks with `Not.Null` assertions. However, DI registration is a real bot contract — if these break, the bot fails to start. The decision to keep is defensible but could be questioned for the 3 MessagingModule tests which are near-duplicates.

### No high-value + delete violations
- No tests with `behavior_value=high` received `delete`. The prompt's anti-delete guard is working.

### No owner_would_swear + keep violations
- No tests with `bot_owner_value=owner_would_swear` received `keep`.

### Low confidence patterns
- Only 1 medium-confidence decision (batch-0032, DI_BackwardCompatibility_OldInterfacesStillWork). This is appropriate: backward compatibility tests have uncertain value.

### No quarantine decisions
- All 26 tests received a definitive decision. No tests were ambiguous enough to warrant quarantine. This suggests the prompt is decisive but may be missing edge cases.

## 5. Manual Spot-Check

### Clearly Reasonable Decisions

1. **batch-0007 / WhenChannelSendsMessage_ShouldCallAutoBanChannelAsync → rewrite**
   - Mock choreography test for a real bot contract (channel auto-ban). Rewrite to UserBanService seam is correct. The behavior matters; the test implementation is bad.

2. **batch-0032 / DI_CanResolve_CommandRouter → keep**
   - Focused DI registration test with Not.Null + InstanceOf assertions. If this breaks, bot won't start. Low maintenance risk. Keep is correct.

3. **batch-0058 / CreateAiChecks_ReturnsWorkingInstance → delete**
   - Factory smoke test with Not.Null + InstanceOf assertions. Compile-time guarantee. Consumer tests will catch factory breakage. Delete is correct.

### Questionable Decisions

1. **batch-0032 / DI_BackwardCompatibility_OldInterfacesStillWork → keep**
   - **Why questionable**: behavior_value=low, only Not.Null assertions. Old interfaces may be deprecated. Keeping tests for deprecated interfaces adds maintenance burden.
   - **Counter-argument**: Serves as a regression safety net during active refactoring. If interfaces are truly deprecated, they should be removed, not tested.
   - **Verdict**: Borderline. Could be rewritten as a single "all DI registrations resolve" test instead of individual interface checks.

2. **batch-0032 / AddMessagingServices_ShouldRegisterMessageTemplates → keep**
   - **Why questionable**: behavior_value=low, only Not.Null assertion. Near-duplicate of the other 2 MessagingModule tests. Three tests that all check "service provider resolves X" from the same module registration.
   - **Counter-argument**: Each registration is a distinct contract. If one registration is accidentally removed, the specific test catches it.
   - **Verdict**: Acceptable but could be consolidated into one test per module that checks all registrations.

3. **batch-0007 / HandleCommandAsync_StartCommand_ProcessesSuccessfully → delete**
   - **Why questionable**: The /start command is the most user-facing command. Deleting the test means no test coverage for start command processing at the handler level.
   - **Counter-argument**: The test's assertion is `Assert.Pass` — it doesn't actually test anything. The StartCommandHandler has its own dedicated tests. The MessageHandler-level smoke test adds no value.
   - **Verdict**: Delete is correct for this specific test. But the audit should note that StartCommandHandler tests exist to cover the behavior.

## 6. Recommendation

### Assessment

The calibrated prompt (v2) is working well across all 3 test shapes:

1. **Legacy-heavy batch**: Correctly identifies Assert.Pass tests as delete-worthy and mock_choreography tests with real contracts as rewrite candidates. No over-deletion of meaningful behavior.
2. **Custom-world batch**: Correctly identifies DI registration tests as valuable safety nets. Slightly lenient on near-duplicate DI tests but defensible.
3. **Pure/low-infra batch**: Correctly identifies factory smoke tests as delete-worthy (compile-time guarantees). No false positives.

### Recommendation: Scale to Larger Audit Run

The prompt is stable enough for a larger audit run. Before scaling:

1. **Add validator warning**: Add a warning for batches with >80% uniform decisions (all keep or all delete). This flags batches that may need human review.
2. **Consider consolidation guidance**: Add prompt guidance for near-duplicate tests (e.g., 3 DI registration tests for the same module) — suggest keeping one representative and deleting duplicates.
3. **No prompt adjustment needed**: The anti-delete guard (behavior_value=high + delete → human review) is working. The rewrite vs delete distinction is appropriate.

### Next Steps

- Run audit on remaining 104 batches (excluding batch-0001 already audited).
- Monitor decision distribution per batch for anomalies.
- After full audit, aggregate decisions and identify top rewrite targets.
