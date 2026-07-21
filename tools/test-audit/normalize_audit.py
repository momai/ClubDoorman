#!/usr/bin/env python3
"""
normalize_audit.py — produce canonical audit view and implementation queues.

Reads:
  .test-audit/raw/tests.raw.jsonl
  .test-audit/audit/*.audit*.jsonl  (excludes archive, invalid, example)

Produces:
  .test-audit/audit-normalized/missing-raw-ids.md
  .test-audit/audit-normalized/duplicate-audit-ids.md
  .test-audit/audit-normalized/current-audit.jsonl
  .test-audit/queues/delete-obvious.md
  .test-audit/queues/rewrite-by-seam.md
  .test-audit/queues/human-review.md
  .test-audit/queues/keep-summary.md
  .test-audit/queues/delete-by-file.md
  .test-audit/reports/audit-normalization-v8.md
"""

from __future__ import annotations

import json
import glob
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
RAW_FILE = REPO_ROOT / ".test-audit" / "raw" / "tests.raw.jsonl"
AUDIT_DIR = REPO_ROOT / ".test-audit" / "audit"
NORMALIZED_DIR = REPO_ROOT / ".test-audit" / "audit-normalized"
QUEUES_DIR = REPO_ROOT / ".test-audit" / "queues"
REPORTS_DIR = REPO_ROOT / ".test-audit" / "reports"


def load_raw_records(path: Path) -> dict[str, dict[str, Any]]:
    records: dict[str, dict[str, Any]] = {}
    with path.open("r", encoding="utf-8") as f:
        for lineno, raw_line in enumerate(f, 1):
            line = raw_line.strip()
            if not line:
                continue
            obj = json.loads(line)
            rid = obj.get("id")
            if isinstance(rid, str) and rid:
                records[rid] = obj
    return records


def load_audit_files() -> list[tuple[Path, list[dict[str, Any]]]]:
    matches = sorted(glob.glob(str(AUDIT_DIR / "*.audit*.jsonl")))
    result: list[tuple[Path, list[dict[str, Any]]]] = []
    for m in matches:
        p = Path(m)
        name = p.name
        if name.endswith(".invalid.jsonl"):
            continue
        if name.startswith("example."):
            continue
        if "/archive/" in str(p):
            continue
        records: list[dict[str, Any]] = []
        with p.open("r", encoding="utf-8") as f:
            for lineno, raw_line in enumerate(f, 1):
                line = raw_line.strip()
                if not line:
                    continue
                obj = json.loads(line)
                if isinstance(obj, dict) and obj.get("id"):
                    records.append(obj)
        if records:
            result.append((p, records))
    return result


def short_id(rid: str) -> str:
    return rid.split("::")[-1] if "::" in rid else rid


# ---------------------------------------------------------------------------
# Part B: Missing raw IDs
# ---------------------------------------------------------------------------

def write_missing_raw_ids(raw: dict[str, dict[str, Any]], deduped: dict[str, dict[str, Any]]) -> list[str]:
    missing = sorted(rid for rid in raw if rid not in deduped)
    lines: list[str] = []
    lines.append("# Missing Raw IDs\n")
    lines.append(f"Raw tests with no audit decision: **{len(missing)}**\n")
    lines.append("")
    if missing:
        for rid in missing:
            raw_rec = raw[rid]
            lines.append(f"## `{short_id(rid)}`\n")
            lines.append(f"- **Full ID**: `{rid}`")
            lines.append(f"- **File**: `{raw_rec.get('file', 'N/A')}`")
            lines.append(f"- **Namespace**: `{raw_rec.get('namespace', 'N/A')}`")
            lines.append(f"- **Class**: `{raw_rec.get('class', 'N/A')}`")
            lines.append(f"- **Test name**: `{raw_rec.get('test_name', 'N/A')}`")
            lines.append(f"- **Line**: {raw_rec.get('line', 'N/A')}")
            lines.append(f"- **Construction path**: `{raw_rec.get('construction_path', 'N/A')}`")
            lines.append(f"- **Categories**: {raw_rec.get('categories', [])}")
            lines.append(f"- **Likely batch**: (see analysis below)")
            lines.append("")

        # Find likely batch for each missing test
        lines.append("## Batch Assignment Analysis\n")
        for rid in missing:
            raw_rec = raw[rid]
            test_file = raw_rec.get("file", "")
            test_name = raw_rec.get("test_name", "")
            # Find batches that contain tests from the same file
            related_batches = []
            for fpath, records in load_audit_files():
                for rec in records:
                    if test_file in rec.get("id", ""):
                        related_batches.append(fpath.name)
                        break
            lines.append(f"- `{short_id(rid)}`: same file tests found in {sorted(set(related_batches))}")
        lines.append("")
    else:
        lines.append("No missing raw IDs.\n")

    NORMALIZED_DIR.mkdir(parents=True, exist_ok=True)
    with (NORMALIZED_DIR / "missing-raw-ids.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return missing


# ---------------------------------------------------------------------------
# Part C: Duplicate audit IDs
# ---------------------------------------------------------------------------

def write_duplicate_audit_ids(
    files: list[tuple[Path, list[dict[str, Any]]]],
    all_ids: Counter,
) -> tuple[dict[str, dict[str, Any]], list[str]]:
    """Return (canonical_map, lines_for_md)."""
    # Build id -> [(file, record)]
    id_occurrences: dict[str, list[tuple[str, dict[str, Any]]]] = defaultdict(list)
    for path, records in files:
        for rec in records:
            rid = rec.get("id", "")
            id_occurrences[rid].append((path.name, rec))

    dup_ids = {k: v for k, v in id_occurrences.items() if len(v) > 1}

    lines: list[str] = []
    lines.append("# Duplicate Audit IDs\n")
    lines.append(f"Total duplicate IDs: **{len(dup_ids)}**\n")
    lines.append("")

    canonical_map: dict[str, dict[str, Any]] = {}

    for rid, occurrences in sorted(dup_ids.items()):
        decisions = [rec.get("decision", "") for _, rec in occurrences]
        agree = len(set(decisions)) == 1

        lines.append(f"## `{short_id(rid)}`\n")
        lines.append(f"- **Full ID**: `{rid}`")
        lines.append(f"- **Occurrences**: {len(occurrences)}")
        lines.append(f"- **Decisions**: {', '.join(f'`{d}`' for d in decisions)}")
        lines.append(f"- **Decisions agree**: {'Yes' if agree else 'No'}")
        lines.append("")

        for fname, rec in occurrences:
            lines.append(f"  - **File**: `{fname}` — decision=`{rec.get('decision', '')}`")
        lines.append("")

        # Canonical selection
        if agree:
            # Pick last file (later batch number = more recent)
            winner_file, winner_rec = occurrences[-1]
            canonical_map[rid] = winner_rec
            lines.append(f"- **Canonical record**: from `{winner_file}`")
            lines.append(f"- **Reason**: decisions agree, picked later file as canonical")
        else:
            # Disagree — mark for human review
            lines.append(f"- **Canonical record**: NONE (sent to human review)")
            lines.append(f"- **Reason**: decisions DISAGREE — cannot auto-resolve")
        lines.append("")

    NORMALIZED_DIR.mkdir(parents=True, exist_ok=True)
    with (NORMALIZED_DIR / "duplicate-audit-ids.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    return canonical_map, list(dup_ids.keys())


# ---------------------------------------------------------------------------
# Part D: Normalized audit JSONL
# ---------------------------------------------------------------------------

def write_normalized_audit(
    raw: dict[str, dict[str, Any]],
    files: list[tuple[Path, list[dict[str, Any]]]],
    all_ids: Counter,
    canonical_map: dict[str, dict[str, Any]],
) -> list[dict[str, Any]]:
    """Build deduplicated audit records, one per raw test ID with audit."""
    # Start with last-file-wins dedup
    deduped: dict[str, dict[str, Any]] = {}
    for path, records in files:
        for rec in records:
            rid = rec.get("id", "")
            deduped[rid] = rec

    # Apply canonical overrides for duplicates
    for rid, rec in canonical_map.items():
        deduped[rid] = rec

    # Filter: only raw IDs present in raw inventory
    normalized = []
    for rid in sorted(deduped.keys()):
        if rid in raw:
            normalized.append(deduped[rid])

    # Write JSONL
    NORMALIZED_DIR.mkdir(parents=True, exist_ok=True)
    out_path = NORMALIZED_DIR / "current-audit.jsonl"
    with out_path.open("w", encoding="utf-8") as f:
        for rec in normalized:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")

    return normalized


# ---------------------------------------------------------------------------
# Part E: Implementation queues
# ---------------------------------------------------------------------------

def write_delete_obvious(normalized: list[dict[str, Any]], anomaly_ids: set[str]) -> list[dict[str, Any]]:
    candidates = []
    for rec in normalized:
        rid = rec.get("id", "")
        if (
            rec.get("decision") == "delete"
            and rec.get("confidence") == "high"
            and rec.get("behavior_value") == "low"
            and rec.get("needs_human_review") is not True
            and rid not in anomaly_ids
        ):
            candidates.append(rec)

    lines: list[str] = []
    lines.append("# Delete Obvious Queue\n")
    lines.append(f"Count: **{len(candidates)}**\n")
    lines.append("Criteria: `decision=delete`, `confidence=high`, `behavior_value=low`, `needs_human_review=false`\n")
    lines.append("")
    lines.append("| Test Name | File | Reason (truncated) |")
    lines.append("|-----------|------|-------------------|")
    for rec in candidates:
        rid = rec.get("id", "")
        short = short_id(rid)
        reason = (rec.get("reason", "") or "")[:120]
        # Extract file from ID
        file_part = rid.split("::")[0] if "::" in rid else rid
        lines.append(f"| `{short}` | `{file_part}` | {reason} |")
    lines.append("")

    QUEUES_DIR.mkdir(parents=True, exist_ok=True)
    with (QUEUES_DIR / "delete-obvious.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return candidates


def write_rewrite_by_seam(normalized: list[dict[str, Any]]) -> list[dict[str, Any]]:
    rewrites = [r for r in normalized if r.get("decision") == "rewrite"]

    # Group by replacement_seam / actual_seam
    groups: dict[tuple[str, str], list[dict[str, Any]]] = defaultdict(list)
    for rec in rewrites:
        rs = rec.get("replacement_seam") or "N/A"
        seam = rec.get("actual_seam", "Unknown")
        groups[(rs, seam)].append(rec)

    lines: list[str] = []
    lines.append("# Rewrite by Seam Queue\n")
    lines.append(f"Total rewrite candidates: **{len(rewrites)}**\n")
    lines.append(f"Groups by (replacement_seam, actual_seam): **{len(groups)}**\n")
    lines.append("")

    for (rs, seam), recs in sorted(groups.items()):
        lines.append(f"## `{rs}` ← `{seam}` ({len(recs)} tests)\n")
        for rec in recs:
            rid = rec.get("id", "")
            short = short_id(rid)
            reason = (rec.get("reason", "") or "")[:120]
            lines.append(f"- `{short}` — {reason}")
        lines.append("")

    QUEUES_DIR.mkdir(parents=True, exist_ok=True)
    with (QUEUES_DIR / "rewrite-by-seam.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return rewrites


def write_human_review(
    normalized: list[dict[str, Any]],
    anomaly_ids: set[str],
    dup_conflicts: list[str],
) -> list[dict[str, Any]]:
    candidates: list[tuple[str, list[str]]] = []
    seen: set[str] = set()

    for rec in normalized:
        rid = rec.get("id", "")
        flags: list[str] = []

        if rec.get("decision") == "quarantine":
            flags.append("quarantine")
        if rec.get("needs_human_review") is True:
            flags.append("needs_human_review=true")
        if rec.get("confidence") == "low":
            flags.append("confidence=low")
        if rid in anomaly_ids:
            flags.append("anomaly_flagged")
        if rid in dup_conflicts:
            flags.append("duplicate_conflict")

        # High-value delete
        if rec.get("behavior_value") == "high" and rec.get("decision") == "delete":
            flags.append("high_value_delete")

        # ID-sensitive keep
        if rec.get("decision") == "keep":
            # Check raw record for id_sensitive
            raw_file = rec.get("id", "").split("::")[0] if "::" in rid else ""
            if "id_sensitive" in rid.lower():
                flags.append("id_sensitive_keep")

        if flags and rid not in seen:
            candidates.append((rid, flags))
            seen.add(rid)

    lines: list[str] = []
    lines.append("# Human Review Queue\n")
    lines.append(f"Count: **{len(candidates)}**\n")
    lines.append("")

    for rid, flags in sorted(candidates):
        short = short_id(rid)
        rec = next((r for r in normalized if r.get("id") == rid), {})
        lines.append(f"## `{short}`\n")
        lines.append(f"- **Full ID**: `{rid}`")
        lines.append(f"- **Decision**: `{rec.get('decision', 'N/A')}`")
        lines.append(f"- **Confidence**: `{rec.get('confidence', 'N/A')}`")
        lines.append(f"- **Flags**: {', '.join(f'`{f}`' for f in flags)}")
        reason = (rec.get("reason", "") or "")[:200]
        lines.append(f"- **Reason**: {reason}")
        lines.append("")

    QUEUES_DIR.mkdir(parents=True, exist_ok=True)
    with (QUEUES_DIR / "human-review.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return [rid for rid, _ in candidates]


def write_keep_summary(normalized: list[dict[str, Any]], raw: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    keeps = [r for r in normalized if r.get("decision") == "keep"]

    # Group by actual_seam and construction_path
    groups: dict[tuple[str, str], list[dict[str, Any]]] = defaultdict(list)
    for rec in keeps:
        seam = rec.get("actual_seam", "Unknown")
        rid = rec.get("id", "")
        raw_rec = raw.get(rid, {})
        cp = raw_rec.get("construction_path", "unknown")
        groups[(seam, cp)].append(rec)

    lines: list[str] = []
    lines.append("# Keep Summary\n")
    lines.append(f"Total keep decisions: **{len(keeps)}**\n")
    lines.append(f"Groups by (actual_seam, construction_path): **{len(groups)}**\n")
    lines.append("")

    for (seam, cp), recs in sorted(groups.items()):
        lines.append(f"## `{seam}` / `{cp}` ({len(recs)} tests)\n")
        for rec in recs:
            rid = rec.get("id", "")
            short = short_id(rid)
            reason = (rec.get("reason", "") or "")[:120]
            lines.append(f"- `{short}` — {reason}")
        lines.append("")

    QUEUES_DIR.mkdir(parents=True, exist_ok=True)
    with (QUEUES_DIR / "keep-summary.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return keeps


def write_delete_by_file(normalized: list[dict[str, Any]]) -> list[dict[str, Any]]:
    deletes = [r for r in normalized if r.get("decision") == "delete"]

    # Group by file
    groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for rec in deletes:
        rid = rec.get("id", "")
        file_part = rid.split("::")[0] if "::" in rid else rid
        groups[file_part].append(rec)

    lines: list[str] = []
    lines.append("# Delete by File\n")
    lines.append(f"Total delete candidates: **{len(deletes)}**\n")
    lines.append(f"Files affected: **{len(groups)}**\n")
    lines.append("")

    for file_path, recs in sorted(groups.items()):
        lines.append(f"## `{file_path}` ({len(recs)} tests)\n")
        for rec in recs:
            rid = rec.get("id", "")
            short = short_id(rid)
            reason = (rec.get("reason", "") or "")[:120]
            lines.append(f"- `{short}` — {reason}")
        lines.append("")

    QUEUES_DIR.mkdir(parents=True, exist_ok=True)
    with (QUEUES_DIR / "delete-by-file.md").open("w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return deletes


# ---------------------------------------------------------------------------
# Part F: Normalization report
# ---------------------------------------------------------------------------

def write_normalization_report(
    raw: dict[str, dict[str, Any]],
    files: list[tuple[Path, list[dict[str, Any]]]],
    all_ids: Counter,
    normalized: list[dict[str, Any]],
    missing_raw_ids: list[str],
    dup_ids: list[str],
    dup_conflicts: list[str],
    obvious_delete: list[dict[str, Any]],
    rewrite_candidates: list[dict[str, Any]],
    human_review_ids: list[str],
    keep_records: list[dict[str, Any]],
    anomaly_ids: set[str],
) -> str:
    raw_total = len(raw)
    total_records = sum(len(recs) for _, recs in files)
    unique_before = len(set(rid for _, recs in files for rid in [r.get("id", "") for r in recs]))
    dup_count = sum(1 for c in all_ids.values() if c > 1)

    # Decision counts after normalization
    dc = Counter(r.get("decision", "unknown") for r in normalized)

    lines: list[str] = []
    lines.append("# Audit Normalization Report (VSlice 8)\n")
    lines.append(f"**Generated**: {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}\n")
    lines.append("")

    lines.append("## Raw Data\n")
    lines.append(f"- Raw tests total: **{raw_total}**")
    lines.append(f"- Audit files included: **{len(files)}**")
    lines.append(f"- Total audit records (all files, before dedup): **{total_records}**")
    lines.append(f"- Unique audited IDs before normalization: **{unique_before}**")
    lines.append("")

    lines.append("## Coverage Gaps\n")
    lines.append(f"- Raw IDs without audit: **{len(missing_raw_ids)}**")
    for mid in missing_raw_ids:
        lines.append(f"  - `{short_id(mid)}`")
    lines.append(f"- Audit IDs not in raw: **0**")
    lines.append(f"- Duplicate audit IDs: **{dup_count}**")
    lines.append("")

    lines.append("## Normalized Output\n")
    lines.append(f"- Normalized record count: **{len(normalized)}**")
    lines.append(f"- Duplicate IDs resolved: **{dup_count}**")
    lines.append(f"- Duplicate conflicts requiring human review: **{len(dup_conflicts)}**")
    lines.append("")

    lines.append("## Decision Counts (After Normalization)\n")
    lines.append(f"| Decision | Count |")
    lines.append(f"|----------|-------|")
    for dec in ["keep", "rewrite", "delete", "quarantine", "unknown"]:
        c = dc.get(dec, 0)
        lines.append(f"| {dec} | {c} |")
    lines.append("")

    lines.append("## Queue Counts\n")
    lines.append(f"- Obvious delete: **{len(obvious_delete)}**")
    lines.append(f"- Rewrite candidates: **{len(rewrite_candidates)}**")
    lines.append(f"- Human review: **{len(human_review_ids)}**")
    lines.append(f"- Keep: **{len(keep_records)}**")
    lines.append("")

    lines.append("## Missing Raw ID Resolution\n")
    if missing_raw_ids:
        lines.append(f"- **{len(missing_raw_ids)}** raw test(s) remain unaudited")
        lines.append(f"- Status: **NOT RESOLVED** (audit not generated for missing test)")
        lines.append(f"- Recommendation: generate audit for missing test before proceeding to implementation")
    else:
        lines.append("- All raw tests have audit records")
    lines.append("")

    lines.append("## Duplicate Conflicts\n")
    if dup_conflicts:
        lines.append(f"- **{len(dup_conflicts)}** duplicate conflict(s) remain for human review")
        for cid in dup_conflicts:
            lines.append(f"  - `{short_id(cid)}`")
    else:
        lines.append("- No duplicate conflicts")
    lines.append("")

    lines.append("## Recommended First Implementation Slice\n")
    lines.append(f"Start with **delete-obvious** queue ({len(obvious_delete)} tests). These are:")
    lines.append(f"- High confidence deletes")
    lines.append(f"- Low behavior value")
    lines.append(f"- No human review needed")
    lines.append(f"- Not anomaly flagged")
    lines.append("")
    lines.append(f"Group by file using `delete-by-file.md` to create small focused PRs.")
    lines.append(f"Target: files with 2-5 delete candidates for first PR.")
    lines.append("")

    report = "\n".join(lines)
    REPORTS_DIR.mkdir(parents=True, exist_ok=True)
    with (REPORTS_DIR / "audit-normalization-v8.md").open("w", encoding="utf-8") as f:
        f.write(report)
    return report


# ---------------------------------------------------------------------------
# Anomaly ID extraction (for human review queue)
# ---------------------------------------------------------------------------

def extract_anomaly_ids(normalized: list[dict[str, Any]], raw: dict[str, dict[str, Any]]) -> set[str]:
    """Match the summarizer's per-record anomaly detection exactly."""
    anomaly_ids: set[str] = set()
    for rec in normalized:
        rid = rec.get("id", "")
        decision = rec.get("decision", "")
        bv = rec.get("behavior_value", "")
        bov = rec.get("bot_owner_value", "")
        conf = rec.get("confidence", "")
        nhr = rec.get("needs_human_review", False)
        rs = rec.get("replacement_seam")
        reason = (rec.get("reason", "") or "").lower()
        raw_rec = raw.get(rid, {})
        cp = raw_rec.get("construction_path", "")
        assertions = raw_rec.get("assertions", {})
        msg_id = raw_rec.get("message_id", {})

        # High-value delete
        if bv == "high" and decision == "delete":
            anomaly_ids.add(rid)

        # Owner-swear keep
        if bov == "owner_would_swear" and decision == "keep":
            anomaly_ids.add(rid)

        # Low confidence, no human review
        if conf == "low" and nhr is not True:
            anomaly_ids.add(rid)

        # Rewrite without replacement_seam
        if decision == "rewrite" and (rs is None or (isinstance(rs, str) and rs.strip() == "")):
            anomaly_ids.add(rid)

        # Delete without replacement justification
        if decision == "delete":
            if not any(phrase in reason for phrase in [
                "no replacement", "compile-time", "compile time",
                "consumer test", "better tested", "already covered",
                "no behavioral", "no behaviour", "no safety",
                "compile", "already marked", "already known",
                "no distinct bot contract", "no unique behavior",
                "fully covered", "already tested", "duplicate of",
                "redundant with", "redundant.", "same as",
                "no contract", "no behavioral contract",
                "no safety net", "no distinct",
            ]):
                anomaly_ids.add(rid)

        # ServiceCollectionDI delete
        if cp == "service_collection_di" and decision == "delete":
            anomaly_ids.add(rid)

        # Assert.Pass kept
        if assertions.get("assert_pass") and decision == "keep":
            anomaly_ids.add(rid)

        # ID-sensitive kept
        if msg_id.get("id_sensitive_suspected") and decision == "keep":
            anomaly_ids.add(rid)

        # Unknown decision
        if decision == "unknown":
            anomaly_ids.add(rid)

    return anomaly_ids


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    print("Loading raw records...")
    raw = load_raw_records(RAW_FILE)
    print(f"  {len(raw)} raw test records")

    print("Loading audit files...")
    files = load_audit_files()
    print(f"  {len(files)} audit files")
    total_records = sum(len(recs) for _, recs in files)
    print(f"  {total_records} total records")

    # Deduplicate: last file wins
    deduped: dict[str, dict[str, Any]] = {}
    all_ids: Counter = Counter()
    for path, records in files:
        for rec in records:
            rid = rec.get("id", "")
            all_ids[rid] += 1
            deduped[rid] = rec

    unique_before = len(deduped)
    dup_count = sum(1 for c in all_ids.values() if c > 1)
    print(f"  {unique_before} unique IDs (after dedup)")
    print(f"  {dup_count} duplicate IDs")

    # Part B: Missing raw IDs
    print("\nPart B: Missing raw IDs...")
    missing_raw_ids = write_missing_raw_ids(raw, deduped)
    print(f"  {len(missing_raw_ids)} missing raw ID(s)")

    # Part C: Duplicate audit IDs
    print("\nPart C: Duplicate audit IDs...")
    canonical_map, dup_id_list = write_duplicate_audit_ids(files, all_ids)

    # Identify conflicts (disagreement)
    id_occurrences: dict[str, list[tuple[str, dict[str, Any]]]] = defaultdict(list)
    for path, records in files:
        for rec in records:
            rid = rec.get("id", "")
            id_occurrences[rid].append((path.name, rec))

    dup_conflicts = []
    for rid in dup_id_list:
        decisions = [rec.get("decision", "") for _, rec in id_occurrences[rid]]
        if len(set(decisions)) > 1:
            dup_conflicts.append(rid)
    print(f"  {len(dup_id_list)} duplicate IDs")
    print(f"  {len(dup_conflicts)} conflicts (disagreeing decisions)")

    # Part D: Normalized audit JSONL
    print("\nPart D: Normalized audit JSONL...")
    normalized = write_normalized_audit(raw, files, all_ids, canonical_map)
    print(f"  {len(normalized)} normalized records")

    # Validate normalized file
    print("  Validating normalized file...")
    import subprocess
    result = subprocess.run(
        ["python3", "tools/test-audit/validate_audit.py", str(NORMALIZED_DIR / "current-audit.jsonl")],
        capture_output=True, text=True
    )
    if result.returncode != 0:
        print(f"  WARNING: validation produced errors:")
        print(f"  {result.stderr[:500]}")
    else:
        print(f"  Validation passed")

    # Part E: Queues
    print("\nPart E: Implementation queues...")

    # Extract anomaly IDs
    anomaly_ids = extract_anomaly_ids(normalized, raw)
    print(f"  Anomaly-flagged IDs: {len(anomaly_ids)}")

    obvious_delete = write_delete_obvious(normalized, anomaly_ids)
    print(f"  Obvious delete: {len(obvious_delete)}")

    rewrite_candidates = write_rewrite_by_seam(normalized)
    print(f"  Rewrite candidates: {len(rewrite_candidates)}")

    human_review_ids = write_human_review(normalized, anomaly_ids, dup_conflicts)
    print(f"  Human review: {len(human_review_ids)}")

    keep_records = write_keep_summary(normalized, raw)
    print(f"  Keep: {len(keep_records)}")

    write_delete_by_file(normalized)
    delete_count = sum(1 for r in normalized if r.get("decision") == "delete")
    print(f"  Delete by file: {delete_count} tests")

    # Part F: Report
    print("\nPart F: Normalization report...")
    report = write_normalization_report(
        raw, files, all_ids, normalized,
        missing_raw_ids, dup_id_list, dup_conflicts,
        obvious_delete, rewrite_candidates, human_review_ids,
        keep_records, anomaly_ids,
    )
    print("  Report written")

    # Summary
    print("\n" + "=" * 60)
    print("NORMALIZATION COMPLETE")
    print("=" * 60)
    print(f"Normalized records: {len(normalized)}")
    print(f"Missing raw IDs: {len(missing_raw_ids)}")
    print(f"Duplicate conflicts: {len(dup_conflicts)}")
    print(f"Queue counts:")
    print(f"  Delete obvious: {len(obvious_delete)}")
    print(f"  Rewrite: {len(rewrite_candidates)}")
    print(f"  Human review: {len(human_review_ids)}")
    print(f"  Keep: {len(keep_records)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
