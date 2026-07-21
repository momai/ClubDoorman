# Channel Effects Migration Worklog

Purpose: Safe, staged migration of channel moderation path to unified effects pipeline.

## Legend
- [x] Completed
- [ ] Pending / Planned
- [~] In progress

## Stages
1. [x] Stage 1: Scaffolding
   - Added env flag `DOORMAN_CHANNEL_EFFECTS_ENABLE` (`Config.ChannelEffectsEnabled`, `IAppConfig.ChannelEffectsEnabled`).
   - Introduced `IChannelModerationEffectsBuilder` + logging-only `ChannelModerationEffectsBuilder` (Stage 1 stub).
   - Wired DI registrations.
   - Added conditional branch in `ChannelModerationService` (effects path when flag ON, legacy fallback otherwise).
   - Verified build + filtered test suite (886 passed / 1 skipped / 0 failed).
2. [~] Stage 2: Flag-enabled tests
   - (Pending) Add focused unit/integration tests with flag ON asserting logging effect invocation & parity.
   - (Pending) Golden master style test capturing moderation decisions both paths.
3. [~] Stage 3: Dual-run & log comparison
   - Dual-run flag `DOORMAN_CHANNEL_EFFECTS_DUAL_RUN_ENABLE` added & implemented (comparison logs + mismatch warning).
   - (Pending) Tests asserting log match / mismatch scenarios (synthetic) & metrics aggregation.
4. [ ] Stage 4: Real channel effects
   - Replace logging stub with real effect mapping (Delete, Ban, Report placeholder, Allow, etc.) reusing existing effect classes where applicable.
   - Add idempotency safeguards (e.g., ensure message deletion/ban executed at most once across dual-run).
5. [ ] Stage 5: Sunset legacy switch
   - Remove switch block from `ModerateChannelMessageContentAsync` & related legacy-only code.
   - Simplify `ChannelModerationService` to just build + execute effects (mirroring user message path).
6. [ ] Stage 6: Cleanup & docs
   - Remove temporary logging/diff instrumentation & this worklog (or condense into `CHANGELOG.md` & architecture docs).
   - Update developer docs with final channel effects architecture.

## Notes
- Current implementation is intentionally minimal (log-only effect) to observe flow before introducing side-effects.
- No new tests yet for flag path (next immediate task).
- Keep legacy code untouched until Stage 4 passes parity validation.
- Remove "мусор" (temporary scaffolding + this file) at Stage 6.
 - Parallel refactor: see `worklog-message-pipeline.md` for user message pipeline extraction (kept separate to avoid scope mixing).

## Next Action
Proceed with Stage 2: introduce tests guarding behavior with `DOORMAN_CHANNEL_EFFECTS_ENABLE=1` (after initial Message Pipeline structural steps to avoid test churn overlap).

Timestamp: 2025-08-20T00:00:00Z (updated)
