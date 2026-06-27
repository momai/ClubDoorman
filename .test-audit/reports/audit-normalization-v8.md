# Audit Normalization Report (VSlice 8)

**Generated**: 2026-06-27 08:17 UTC


## Raw Data

- Raw tests total: **847**
- Audit files included: **107**
- Total audit records (all files, before dedup): **852**
- Unique audited IDs before normalization: **846**

## Coverage Gaps

- Raw IDs without audit: **1**
  - `AdminDisplayName_WithUsername_ReturnsUsername`
- Audit IDs not in raw: **0**
- Duplicate audit IDs: **6**

## Normalized Output

- Normalized record count: **846**
- Duplicate IDs resolved: **6**
- Duplicate conflicts requiring human review: **3**

## Decision Counts (After Normalization)

| Decision | Count |
|----------|-------|
| keep | 258 |
| rewrite | 178 |
| delete | 403 |
| quarantine | 7 |
| unknown | 0 |

## Queue Counts

- Obvious delete: **255**
- Rewrite candidates: **178**
- Human review: **143**
- Keep: **258**

## Missing Raw ID Resolution

- **1** raw test(s) remain unaudited
- Status: **NOT RESOLVED** (audit not generated for missing test)
- Recommendation: generate audit for missing test before proceeding to implementation

## Duplicate Conflicts

- **3** duplicate conflict(s) remain for human review
  - `CheckMessageAsync_MimicryDetected_ReturnsBanAction`
  - `CheckUserName_WithNormalName_ReturnsAllow`
  - `LeftMemberCleanup_EmitsSemanticRule`

## Recommended First Implementation Slice

Start with **delete-obvious** queue (255 tests). These are:
- High confidence deletes
- Low behavior value
- No human review needed
- Not anomaly flagged

Group by file using `delete-by-file.md` to create small focused PRs.
Target: files with 2-5 delete candidates for first PR.
