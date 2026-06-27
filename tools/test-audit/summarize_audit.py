#!/usr/bin/env python3
"""
summarize_audit.py — aggregate audit JSONL files and produce a human-readable report.

Reads:
  .test-audit/raw/tests.raw.jsonl
  .test-audit/audit/*.audit*.jsonl  (excludes *.invalid.jsonl and example.audit.jsonl)

Produces:
  .test-audit/reports/audit-summary.md

Usage:
    python3 tools/test-audit/summarize_audit.py
    python3 tools/test-audit/summarize_audit.py --include-example
    python3 tools/test-audit/summarize_audit.py --audit-glob ".test-audit/audit/*.audit*.jsonl"
"""

from __future__ import annotations

import argparse
import glob
import json
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
RAW_FILE = REPO_ROOT / ".test-audit" / "raw" / "tests.raw.jsonl"
AUDIT_DIR = REPO_ROOT / ".test-audit" / "audit"
REPORT_DIR = REPO_ROOT / ".test-audit" / "reports"
REPORT_FILE = REPORT_DIR / "audit-summary.md"

DEFAULT_GLOB = str(AUDIT_DIR / "*.audit*.jsonl")


# ---------------------------------------------------------------------------
# Loading
# ---------------------------------------------------------------------------

def load_raw_records(path: Path) -> dict[str, dict[str, Any]]:
    """Load raw test records keyed by id."""
    records: dict[str, dict[str, Any]] = {}
    with path.open("r", encoding="utf-8") as f:
        for lineno, raw_line in enumerate(f, 1):
            line = raw_line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError as e:
                print(f"WARNING: raw line {lineno}: invalid JSON: {e}", file=sys.stderr)
                continue
            rid = obj.get("id")
            if isinstance(rid, str) and rid:
                records[rid] = obj
    return records


def should_skip_file(path: Path, include_example: bool) -> bool:
    """Return True if this audit file should be skipped."""
    name = path.name
    if name.endswith(".invalid.jsonl"):
        return True
    if not include_example and name.startswith("example."):
        return True
    return False


def load_audit_files(
    glob_pattern: str, include_example: bool
) -> list[tuple[Path, list[dict[str, Any]]]]:
    """Load audit files, returning list of (path, records)."""
    matches = sorted(glob.glob(glob_pattern))
    result: list[tuple[Path, list[dict[str, Any]]]] = []
    for m in matches:
        p = Path(m)
        if not p.is_file():
            continue
        if should_skip_file(p, include_example):
            continue
        records: list[dict[str, Any]] = []
        with p.open("r", encoding="utf-8") as f:
            for lineno, raw_line in enumerate(f, 1):
                line = raw_line.strip()
                if not line:
                    continue
                try:
                    obj = json.loads(line)
                except json.JSONDecodeError as e:
                    print(
                        f"WARNING: {p}:{lineno}: invalid JSON: {e}",
                        file=sys.stderr,
                    )
                    continue
                if isinstance(obj, dict) and obj.get("id"):
                    records.append(obj)
        if records:
            result.append((p, records))
    return result


def deduplicate(
    files: list[tuple[Path, list[dict[str, Any]]]]
) -> tuple[dict[str, dict[str, Any]], Counter, list[tuple[str, str, str]]]:
    """
    Deduplicate audit records by id.  Later files overwrite earlier ones
    (sorted by path, so .v2 wins over .v1).

    Returns:
      deduped:  id -> record (winner)
      dup_info: id -> count (all occurrences)
      dup_details: list of (id, file, decision) for every occurrence
    """
    all_ids: Counter = Counter()
    deduped: dict[str, dict[str, Any]] = {}
    dup_details: list[tuple[str, str, str]] = []

    for path, records in files:
        for rec in records:
            rid = rec.get("id", "")
            all_ids[rid] += 1
            deduped[rid] = rec
            dup_details.append((rid, path.name, rec.get("decision", "")))

    return deduped, all_ids, dup_details


# ---------------------------------------------------------------------------
# Analysis helpers
# ---------------------------------------------------------------------------

def pct(n: int, total: int) -> str:
    if total == 0:
        return "0.0%"
    return f"{n / total * 100:.1f}%"


def decision_counts(records: list[dict[str, Any]]) -> Counter:
    return Counter(r.get("decision", "unknown") for r in records)


def dominant_decision(counter: Counter) -> tuple[str, str]:
    if not counter:
        return ("N/A", "0.0%")
    total = sum(counter.values())
    top = counter.most_common(1)[0]
    return (top[0], pct(top[1], total))


# ---------------------------------------------------------------------------
# Anomaly detection
# ---------------------------------------------------------------------------

def detect_anomalies(
    deduped: dict[str, dict[str, Any]],
    raw: dict[str, dict[str, Any]],
    files: list[tuple[Path, list[dict[str, Any]]]],
) -> list[str]:
    anomalies: list[str] = []

    # Per-batch >80% same decision
    for path, records in files:
        dc = decision_counts(records)
        total = sum(dc.values())
        if total == 0:
            continue
        top = dc.most_common(1)[0]
        ratio = top[1] / total
        if ratio > 0.8:
            anomalies.append(
                f"**Batch homogeneity**: {path.name} — "
                f"{top[1]}/{total} ({ratio * 100:.0f}%) are `{top[0]}`"
            )

    # Per-record anomalies
    for rid, rec in sorted(deduped.items()):
        decision = rec.get("decision", "")
        bv = rec.get("behavior_value", "")
        bov = rec.get("bot_owner_value", "")
        conf = rec.get("confidence", "")
        nhr = rec.get("needs_human_review", False)
        rs = rec.get("replacement_seam")
        reason = rec.get("reason", "") or ""
        raw_rec = raw.get(rid, {})
        cp = raw_rec.get("construction_path", "")
        assertions = raw_rec.get("assertions", {})
        msg_id = raw_rec.get("message_id", {})

        if bv == "high" and decision == "delete":
            anomalies.append(
                f"**High-value delete**: `{rid}` — "
                f"behavior_value=high but decision=delete"
            )

        if bov == "owner_would_swear" and decision == "keep":
            anomalies.append(
                f"**Owner-swear keep**: `{rid}` — "
                f"bot_owner_value=owner_would_swear but decision=keep"
            )

        if conf == "low" and nhr is not True:
            anomalies.append(
                f"**Low confidence, no human review**: `{rid}` — "
                f"confidence=low but needs_human_review=false"
            )

        if decision == "rewrite" and (rs is None or (isinstance(rs, str) and rs.strip() == "")):
            anomalies.append(
                f"**Rewrite without replacement_seam**: `{rid}` — "
                f"decision=rewrite but replacement_seam is null/empty"
            )

        if decision == "delete" and "replacement" not in reason.lower() and "no replacement" not in reason.lower():
            # Check if reason explains why no replacement is needed
            if not any(phrase in reason.lower() for phrase in [
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
                anomalies.append(
                    f"**Delete without replacement justification**: `{rid}` — "
                    f"decision=delete but reason doesn't explain why no replacement is needed"
                )

        if cp == "service_collection_di" and decision == "delete":
            anomalies.append(
                f"**ServiceCollectionDI delete**: `{rid}` — "
                f"construction_path=service_collection_di but decision=delete"
            )

        if assertions.get("assert_pass") and decision == "keep":
            anomalies.append(
                f"**Assert.Pass kept**: `{rid}` — "
                f"assertion smell assert_pass but decision=keep"
            )

        if msg_id.get("id_sensitive_suspected") and decision == "keep":
            anomalies.append(
                f"**ID-sensitive kept**: `{rid}` — "
                f"message_id.id_sensitive_suspected=true but decision=keep"
            )

        if decision == "unknown":
            anomalies.append(
                f"**Unknown decision**: `{rid}`"
            )

    # Quarantine count
    quarantine_ids = [rid for rid, rec in deduped.items() if rec.get("decision") == "quarantine"]
    if quarantine_ids:
        anomalies.append(
            f"**Quarantine count**: {len(quarantine_ids)} record(s) marked quarantine"
        )

    return anomalies


# ---------------------------------------------------------------------------
# Candidate queues
# ---------------------------------------------------------------------------

def high_conf_delete(deduped: dict[str, dict[str, Any]]) -> list[tuple[str, str]]:
    """(id, reason) for high-confidence delete with low behavior value."""
    candidates = []
    for rid, rec in sorted(deduped.items()):
        if (
            rec.get("decision") == "delete"
            and rec.get("confidence") == "high"
            and rec.get("behavior_value") == "low"
        ):
            candidates.append((rid, rec.get("reason", "")[:120]))
    return candidates


def rewrite_candidates(deduped: dict[str, dict[str, Any]]) -> list[tuple[str, str, str]]:
    """(id, replacement_seam, reason) for rewrite decisions."""
    candidates = []
    for rid, rec in sorted(deduped.items()):
        if rec.get("decision") == "rewrite":
            candidates.append((
                rid,
                rec.get("replacement_seam") or "N/A",
                rec.get("reason", "")[:120],
            ))
    return candidates


def human_review_candidates(
    deduped: dict[str, dict[str, Any]],
    anomalies: list[str],
) -> list[tuple[str, str]]:
    """(id, flag) for records needing human review."""
    anomaly_ids: set[str] = set()
    for a in anomalies:
        match = re.search(r"`([^`]+)`", a)
        if match:
            anomaly_ids.add(match.group(1))

    candidates: list[tuple[str, str]] = []
    seen: set[str] = set()
    for rid, rec in sorted(deduped.items()):
        flags: list[str] = []
        if rec.get("needs_human_review"):
            flags.append("needs_human_review=true")
        if rec.get("confidence") == "low":
            flags.append("confidence=low")
        if rec.get("decision") == "quarantine":
            flags.append("quarantine")
        if rid in anomaly_ids:
            flags.append("anomaly_flagged")
        if flags:
            if rid not in seen:
                candidates.append((rid, ", ".join(flags)))
                seen.add(rid)
    return candidates


def consolidation_candidates(
    deduped: dict[str, dict[str, Any]],
    raw: dict[str, dict[str, Any]],
) -> list[list[tuple[str, str]]]:
    """
    Group by (file, actual_seam, decision) then check name similarity.
    Returns list of groups, each group is list of (id, short_name).
    Only groups with 2+ members are returned.
    """
    groups: dict[tuple[str, str, str], list[tuple[str, str]]] = defaultdict(list)
    for rid, rec in deduped.items():
        raw_rec = raw.get(rid, {})
        file_name = raw_rec.get("file", rec.get("id", "").split("::")[0].split("/")[-1] if "::" in rec.get("id", "") else rec.get("id", ""))
        seam = rec.get("actual_seam", "")
        decision = rec.get("decision", "")
        test_name = raw_rec.get("test_name", rid.split("::")[-1] if "::" in rid else rid)
        groups[(file_name, seam, decision)].append((rid, test_name))

    result: list[list[tuple[str, str]]] = []
    for key, members in sorted(groups.items()):
        if len(members) < 2:
            continue
        # Check name similarity: share a common prefix (first 3 words)
        prefixes: list[str] = []
        for _, name in members:
            words = re.split(r"[_\s]+", name)[:3]
            prefixes.append(" ".join(words))
        unique_prefixes = set(prefixes)
        if len(unique_prefixes) < len(members):
            result.append(members)
        elif len(members) >= 3:
            result.append(members)
    return result


# ---------------------------------------------------------------------------
# Report generation
# ---------------------------------------------------------------------------

def generate_report(
    raw: dict[str, dict[str, Any]],
    files: list[tuple[Path, list[dict[str, Any]]]],
    deduped: dict[str, dict[str, Any]],
    all_ids: Counter,
    anomalies: list[str],
) -> str:
    lines: list[str] = []
    a = lines.append

    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    a(f"# Audit Summary Report\n")
    a(f"**Generated**: {now}\n")
    a(f"**Audit files included**: {len(files)}\n")
    a("")

    # File list
    a("Included files:")
    for path, records in files:
        a(f"- `{path.name}` ({len(records)} records)")
    a("")

    # ---- 1. Coverage ----
    a("## 1. Coverage\n")
    raw_total = len(raw)
    total_records = sum(len(recs) for _, recs in files)
    unique_ids = len(deduped)
    dup_count = sum(1 for c in all_ids.values() if c > 1)
    audited_raw_ids = set(rid for rid in deduped if rid in raw)
    raw_ids_without_audit = set(rid for rid in raw if rid not in deduped)
    audit_ids_not_in_raw = set(rid for rid in deduped if rid not in raw)
    pct_audited = pct(len(audited_raw_ids), raw_total)

    a(f"| Metric | Value |")
    a(f"|--------|-------|")
    a(f"| Raw tests total | {raw_total} |")
    a(f"| Audited records total (all files) | {total_records} |")
    a(f"| Unique audited IDs (after dedup) | {unique_ids} |")
    a(f"| Percent of raw audited | {pct_audited} |")
    a(f"| Raw IDs without audit (raw test has no audit record) | {len(raw_ids_without_audit)} |")
    a(f"| Audit IDs not in raw (audit record references non-existent raw test) | {len(audit_ids_not_in_raw)} |")
    a(f"| Duplicate audit IDs (same ID appears in multiple audit files) | {dup_count} |")
    a("")

    if len(raw_ids_without_audit):
        a("Raw IDs without audit (raw test has no audit record):")
        for rid in sorted(raw_ids_without_audit):
            a(f"- `{rid}`")
        a("")

    if len(audit_ids_not_in_raw):
        a("Audit IDs not in raw (audit record references non-existent raw test):")
        for rid in sorted(audit_ids_not_in_raw):
            a(f"- `{rid}`")
        a("")

    if dup_count:
        a("Duplicate audit IDs (same ID appears in multiple audit files):")
        for rid, count in sorted(all_ids.items()):
            if count > 1:
                a(f"- `{rid}`: {count}x")
        a("")

    # ---- 2. Decision counts ----
    a("## 2. Decision Counts\n")
    dc = Counter(r.get("decision", "unknown") for r in deduped.values())
    total_decided = sum(dc.values())
    a(f"| Decision | Count | Percentage |")
    a(f"|----------|-------|------------|")
    for dec in ["keep", "rewrite", "delete", "quarantine", "unknown"]:
        c = dc.get(dec, 0)
        a(f"| {dec} | {c} | {pct(c, total_decided)} |")
    a("")

    # ---- 3. Decision counts by batch ----
    a("## 3. Decision Counts by Batch\n")
    for path, records in files:
        bdc = decision_counts(records)
        btotal = sum(bdc.values())
        dom_dec, dom_pct = dominant_decision(bdc)
        a(f"### `{path.name}` ({btotal} records)\n")
        a(f"Dominant decision: `{dom_dec}` ({dom_pct})\n")
        a(f"| Decision | Count |")
        a(f"|----------|-------|")
        for dec in ["keep", "rewrite", "delete", "quarantine", "unknown"]:
            c = bdc.get(dec, 0)
            if c:
                a(f"| {dec} | {c} |")
        a("")

    # ---- 4. Decision counts by construction_path ----
    a("## 4. Decision Counts by Construction Path\n")
    cp_groups: dict[str, Counter] = defaultdict(Counter)
    for rid, rec in deduped.items():
        raw_rec = raw.get(rid, {})
        cp = raw_rec.get("construction_path", "unknown")
        cp_groups[cp][rec.get("decision", "unknown")] += 1

    a(f"| Construction Path | {' | '.join(['keep', 'rewrite', 'delete', 'quarantine', 'unknown'])} | Total |")
    a(f"|-------------------|{' | '.join(['-----' for _ in range(5)])}|-------|")
    for cp in sorted(cp_groups.keys()):
        counter = cp_groups[cp]
        total = sum(counter.values())
        cols = [str(counter.get(d, 0)) for d in ["keep", "rewrite", "delete", "quarantine", "unknown"]]
        a(f"| {cp} | {' | '.join(cols)} | {total} |")
    a("")

    # ---- 5. Decision counts by actual_seam ----
    a("## 5. Decision Counts by Actual Seam\n")
    seam_groups: dict[str, Counter] = defaultdict(Counter)
    for rid, rec in deduped.items():
        seam = rec.get("actual_seam", "Unknown")
        seam_groups[seam][rec.get("decision", "unknown")] += 1

    a(f"| Actual Seam | {' | '.join(['keep', 'rewrite', 'delete', 'quarantine', 'unknown'])} | Total |")
    a(f"|-------------|{' | '.join(['-----' for _ in range(5)])}|-------|")
    for seam in sorted(seam_groups.keys()):
        counter = seam_groups[seam]
        total = sum(counter.values())
        cols = [str(counter.get(d, 0)) for d in ["keep", "rewrite", "delete", "quarantine", "unknown"]]
        a(f"| {seam} | {' | '.join(cols)} | {total} |")
    a("")

    # ---- 6. Suspicious / Anomaly Flags ----
    a("## 6. Suspicious / Anomaly Flags\n")
    if anomalies:
        a(f"**{len(anomalies)} anomaly flag(s) detected.**\n")
        for anomaly in anomalies:
            a(f"- {anomaly}")
        a("")
    else:
        a("No anomalies detected.\n")

    # ---- 7. Top Candidate Queues ----
    a("## 7. Top Candidate Queues\n")

    # 7a. High-confidence delete
    hc_delete = high_conf_delete(deduped)
    a(f"### High-Confidence Delete Candidates ({len(hc_delete)})\n")
    a("`decision=delete`, `confidence=high`, `behavior_value=low`\n")
    if hc_delete:
        a("| ID | Reason (truncated) |")
        a("|----|-------------------|")
        for rid, reason in hc_delete:
            short = rid.split("::")[-1] if "::" in rid else rid
            a(f"| `{short}` | {reason} |")
        a("")
    else:
        a("None.\n")

    # 7b. Rewrite candidates
    rewrites = rewrite_candidates(deduped)
    a(f"### Rewrite Candidates ({len(rewrites)})\n")
    if rewrites:
        a("| ID | Replacement Seam | Reason (truncated) |")
        a("|----|-----------------|-------------------|")
        for rid, seam, reason in rewrites:
            short = rid.split("::")[-1] if "::" in rid else rid
            a(f"| `{short}` | {seam} | {reason} |")
        a("")
    else:
        a("None.\n")

    # 7c. Human review candidates
    hr = human_review_candidates(deduped, anomalies)
    a(f"### Human Review Candidates ({len(hr)})\n")
    a("`needs_human_review=true` OR `confidence=low` OR `quarantine` OR anomaly flagged\n")
    if hr:
        a("| ID | Flags |")
        a("|----|-------|")
        for rid, flags in hr:
            short = rid.split("::")[-1] if "::" in rid else rid
            a(f"| `{short}` | {flags} |")
        a("")
    else:
        a("None.\n")

    # 7d. Consolidation candidates
    consol = consolidation_candidates(deduped, raw)
    a(f"### Possible Duplicate / Consolidation Candidates ({len(consol)} groups)\n")
    if consol:
        for i, group in enumerate(consol, 1):
            a(f"**Group {i}** ({len(group)} tests):\n")
            for rid, name in group:
                short = rid.split("::")[-1] if "::" in rid else rid
                a(f"- `{short}`")
            a("")
    else:
        a("No consolidation candidates found.\n")

    # ---- 8. Known Limitations ----
    a("## 8. Known Limitations\n")
    a("1. **Audit is advisory.** Decisions produced by LLM audit are recommendations, not mandates. They should be treated as input to human review, not as automated directives.")
    a("2. **No code changes from aggregate counts.** Aggregate decision counts alone are insufficient justification for removing or rewriting tests. Each decision requires individual slice-level review.")
    a("3. **Delete/rewrite decisions need slice review.** Before acting on any delete or rewrite decision, the test must be reviewed in context: understand what behavior it covers, what seam it belongs to, and whether a replacement is truly unnecessary.")
    a("4. **Coverage is partial.** Only COVERAGE_NUM of COVERAGE_TOTAL raw tests have been audited (COVERAGE_PCT). Decisions about unaudited tests should not be inferred from audited ones.")
    a("5. **Anomaly flags are heuristics.** The anomaly detection rules are conservative patterns. A flagged record may be perfectly valid — the flag means 'look at this one.'")
    a("6. **Deduplication strategy.** When the same test ID appears in multiple audit files, the last file (sorted by path) wins. This means `.v2` files override `.v1` files. The report counts all occurrences but analysis uses the deduplicated set.")
    a("")

    text = "\n".join(lines)
    text = text.replace("COVERAGE_NUM", str(len(audited_raw_ids)))
    text = text.replace("COVERAGE_TOTAL", str(raw_total))
    text = text.replace("COVERAGE_PCT", pct_audited)
    return text


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        description="Aggregate audit JSONL files and produce a summary report."
    )
    parser.add_argument(
        "--include-example",
        action="store_true",
        help="Include example.audit.jsonl in the aggregation",
    )
    parser.add_argument(
        "--audit-glob",
        default=DEFAULT_GLOB,
        help="Glob pattern for audit files (default: .test-audit/audit/*.audit*.jsonl)",
    )
    args = parser.parse_args()

    # Load raw records
    if not RAW_FILE.is_file():
        print(f"ERROR: raw inventory not found: {RAW_FILE}", file=sys.stderr)
        return 2

    raw = load_raw_records(RAW_FILE)
    print(f"Loaded {len(raw)} raw test records")

    # Load audit files
    files = load_audit_files(args.audit_glob, args.include_example)
    if not files:
        print("ERROR: no audit files matched the glob pattern", file=sys.stderr)
        return 1

    print(f"Loaded {len(files)} audit files:")
    for path, records in files:
        print(f"  {path.name}: {len(records)} records")

    # Deduplicate
    deduped, all_ids, _ = deduplicate(files)
    total_records = sum(len(recs) for _, recs in files)
    unique_ids = len(deduped)
    dup_count = sum(1 for c in all_ids.values() if c > 1)
    print(f"Total audit records: {total_records}")
    print(f"Unique IDs (after dedup): {unique_ids}")
    print(f"Duplicate IDs: {dup_count}")

    # Detect anomalies
    anomalies = detect_anomalies(deduped, raw, files)
    print(f"Anomalies detected: {len(anomalies)}")

    # Generate report
    report = generate_report(raw, files, deduped, all_ids, anomalies)

    # Write report
    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    with REPORT_FILE.open("w", encoding="utf-8") as f:
        f.write(report)

    print(f"\nReport written to: {REPORT_FILE}")

    # Print summary to stdout
    dc = Counter(r.get("decision", "unknown") for r in deduped.values())
    total_decided = sum(dc.values())
    print(f"\nDecision counts:")
    for dec in ["keep", "rewrite", "delete", "quarantine", "unknown"]:
        c = dc.get(dec, 0)
        print(f"  {dec}: {c} ({pct(c, total_decided)})")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
