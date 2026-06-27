# Audit Run State — 2026-06-27 08:06 UTC (FINAL)

## Overview
- Date/time: 2026-06-27 08:06 UTC
- Total raw tests: 847
- Current audited unique IDs: 846
- Coverage: 99.9%
- Last completed wave: Wave 50 (batch 0107 — final batch)
- Validation status: PASS (all 107 batches)
- Current anomaly count: 179
- Stop/review status: COMPLETE — all batches audited
- JSONL manually edited: NO

## Audited Batches
All 107 batches (0001-0107)

## Decision Counts (Final)
| Decision | Count | Percentage |
|----------|-------|------------|
| keep     | 258   | 30.5%      |
| rewrite  | 178   | 21.0%      |
| delete   | 403   | 47.6%      |
| quarantine | 7  | 0.8%       |
| unknown  | 0     | 0.0%       |

## Waves Completed
50

## Notes
- Summarizer bug fixed during run (`.format()` conflict with `{}` in test IDs)
- 6 duplicate IDs across audit files (dedup handled by summarizer)
- batch-0027, batch-0105, batch-0106 required retry (worker failed to produce output)
