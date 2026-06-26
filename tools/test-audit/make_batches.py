#!/usr/bin/env python3
"""
make_batches.py — batch raw test inventory into small JSON files for local LLM audit.

Reads .test-audit/raw/tests.raw.jsonl, prioritizes suspicious tests,
extracts source excerpts, and writes batch-NNNN.json files under .test-audit/batches/.

Does NOT run an LLM. Does NOT classify tests. Does NOT modify any test files.
"""

import json
import os
import sys
from collections import Counter
from pathlib import Path

# ── Configuration ──────────────────────────────────────────────────────────────

RAW_FILE = Path(".test-audit/raw/tests.raw.jsonl")
BATCH_DIR = Path(".test-audit/batches")
SUMMARY_FILE = BATCH_DIR / "batch-summary.md"
TEST_PROJECT_ROOT = Path("ClubDoorman.Test")

MIN_TESTS_PER_BATCH = 8
MAX_TESTS_PER_BATCH = 12
MAX_SOURCE_CHARS_PER_BATCH = 30_000
EXCERPT_RADIUS_LINES = 30  # lines before/after test start line

# Smells that indicate high cleanup value
PRIORITY_SMELLS = {
    "assert_pass",
    "does_not_throw",
    "weak_not_null",
    "weak_not_empty",
    "mock_verify",
    "uses_message_handler_test_factory",
    "uses_fake_services_factory",
    "uses_testkit_message_handler_builder",
    "uses_testkit_autofixture",
    "uses_direct_new_message_handler",
    "uses_with_message_id",
    "reads_message_message_id",
}

# Construction paths that indicate high cleanup value
PRIORITY_CONSTRUCTION_PATHS = {
    "legacy_message_handler_test_factory",
    "fake_services_factory",
    "testkit_message_handler_builder",
    "testkit_autofixture",
    "direct_new_message_handler",
    "custom_local_world",
    "service_collection_di",
}


def compute_priority_score(record: dict) -> int:
    """Higher score = higher priority for earlier batches."""
    score = 0

    # Smells from top-level smells array
    smells = record.get("smells", [])
    for smell in smells:
        if smell in PRIORITY_SMELLS:
            score += 10

    # Assertion smells
    assertions = record.get("assertions", {})
    for key in ("assert_pass", "does_not_throw", "weak_not_null", "weak_not_empty", "mock_verify"):
        if assertions.get(key):
            score += 10

    # Infra smells
    infra = record.get("infra", {})
    for key in (
        "uses_message_handler_test_factory",
        "uses_fake_services_factory",
        "uses_testkit_message_handler_builder",
        "uses_testkit_autofixture",
        "uses_direct_new_message_handler",
    ):
        if infra.get(key):
            score += 10

    # Message ID smells
    msg_id = record.get("message_id", {})
    for key in ("uses_with_message_id", "reads_message_message_id"):
        if msg_id.get(key):
            score += 10

    # Construction path priority
    cp = record.get("construction_path", "")
    if cp in PRIORITY_CONSTRUCTION_PATHS:
        score += 20

    return score


def extract_source_excerpt(file_rel: str, line: int) -> str:
    """Extract ~40-60 lines around the test start line from source."""
    source_path = TEST_PROJECT_ROOT / file_rel
    if not source_path.is_file():
        return f"# SOURCE FILE NOT FOUND: {source_path}"

    try:
        with open(source_path, "r", encoding="utf-8", errors="replace") as f:
            all_lines = f.readlines()
    except Exception as e:
        return f"# ERROR READING SOURCE: {source_path}: {e}"

    total = len(all_lines)
    start = max(0, line - 1 - EXCERPT_RADIUS_LINES)
    end = min(total, line - 1 + EXCERPT_RADIUS_LINES + 1)

    excerpt_lines = all_lines[start:end]
    prefix = f"# File: {source_path}\n# Lines {start + 1}-{end} (test starts at line {line})\n"
    return prefix + "".join(excerpt_lines)


def load_raw_records(path: Path) -> list[dict]:
    """Load and validate raw test records."""
    records = []
    with open(path, "r", encoding="utf-8") as f:
        for lineno, raw_line in enumerate(f, 1):
            raw_line = raw_line.strip()
            if not raw_line:
                continue
            try:
                rec = json.loads(raw_line)
                rec["_source_lineno"] = lineno
                records.append(rec)
            except json.JSONDecodeError as e:
                print(f"WARNING: skipping malformed line {lineno}: {e}", file=sys.stderr)
    return records


def sort_records(records: list[dict]) -> list[dict]:
    """Sort records so highest-priority (most suspicious) come first."""
    scored = [(compute_priority_score(r), idx, r) for idx, r in enumerate(records)]
    # Sort by score descending, then original index for stability
    scored.sort(key=lambda t: (-t[0], t[1]))
    return [t[2] for t in scored]


def build_batch(batch_no: int, test_records: list[dict]) -> dict:
    """Build a single batch JSON object."""
    tests = []
    for rec in test_records:
        file_rel = rec.get("file", "")
        line = rec.get("line", 0)
        excerpt = extract_source_excerpt(file_rel, line)
        tests.append({
            "raw": rec,
            "source_excerpt": excerpt,
        })

    return {
        "schema_version": "test-audit-batch-v1",
        "batch_no": batch_no,
        "purpose": "local LLM audit input",
        "instructions": "Return JSONL only. One audit object per input test. No markdown. No code edits.",
        "tests": tests,
    }


def split_into_batches(records: list[dict]) -> list[list[dict]]:
    """Split sorted records into batches respecting size constraints."""
    batches = []
    current_batch = []
    current_chars = 0

    for rec in records:
        file_rel = rec.get("file", "")
        line = rec.get("line", 0)
        excerpt = extract_source_excerpt(file_rel, line)
        excerpt_chars = len(excerpt) + len(json.dumps(rec))

        # Check if adding this record would exceed limits
        if (
            len(current_batch) >= MAX_TESTS_PER_BATCH
            or (current_batch and current_chars + excerpt_chars > MAX_SOURCE_CHARS_PER_BATCH)
        ):
            batches.append(current_batch)
            current_batch = []
            current_chars = 0
            excerpt_chars = len(excerpt) + len(json.dumps(rec))

        current_batch.append(rec)
        current_chars += excerpt_chars

    if current_batch:
        batches.append(current_batch)

    return batches


def write_batches(batches: list[list[dict]], batch_dir: Path) -> list[dict]:
    """Write batch files and collect stats."""
    batch_dir.mkdir(parents=True, exist_ok=True)

    stats = []
    for idx, batch_records in enumerate(batches, 1):
        batch_no = idx
        batch_data = build_batch(batch_no, batch_records)
        filename = f"batch-{batch_no:04d}.json"
        filepath = batch_dir / filename

        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(batch_data, f, ensure_ascii=False, indent=2)

        # Collect stats
        files_in_batch = set(r.get("file", "") for r in batch_records)
        all_smells = set()
        for r in batch_records:
            for s in r.get("smells", []):
                all_smells.add(s)
            for k, v in r.get("assertions", {}).items():
                if v and k in PRIORITY_SMELLS:
                    all_smells.add(k)
            for k, v in r.get("infra", {}).items():
                if v and k in PRIORITY_SMELLS:
                    all_smells.add(k)
            for k, v in r.get("message_id", {}).items():
                if v and k in PRIORITY_SMELLS:
                    all_smells.add(k)

        total_chars = sum(
            len(build_batch(batch_no, batch_records)["tests"][0]["source_excerpt"])
            for _ in batch_records
        )
        # Recalculate properly
        batch_obj = build_batch(batch_no, batch_records)
        total_chars = sum(len(t["source_excerpt"]) for t in batch_obj["tests"])

        stats.append({
            "batch_no": batch_no,
            "filename": filename,
            "test_count": len(batch_records),
            "files": sorted(files_in_batch),
            "smells": sorted(all_smells),
            "source_chars": total_chars,
        })

    return stats


def write_summary(stats: list[dict], total_records: int, unique_id_count: int, summary_path: Path):
    """Write batch-summary.md."""
    summary_path.parent.mkdir(parents=True, exist_ok=True)

    total_batched = sum(s["test_count"] for s in stats)
    avg_tests = total_batched / len(stats) if stats else 0
    total_chars = sum(s["source_chars"] for s in stats)
    batch_sizes = [s["test_count"] for s in stats]
    min_batch = min(batch_sizes) if batch_sizes else 0
    max_batch = max(batch_sizes) if batch_sizes else 0

    lines = [
        "# Batch Summary",
        "",
        f"- **Total raw records read:** {total_records}",
        f"- **Total batches created:** {len(stats)}",
        f"- **Total records batched:** {total_batched}",
        f"- **Unique IDs batched:** {unique_id_count}",
        f"- **Duplicate IDs:** 0 (validated before batching)",
        f"- **Average tests per batch:** {avg_tests:.1f}",
        f"- **Batch size range:** {min_batch}-{max_batch}",
        f"- **Total source excerpt chars:** {total_chars:,}",
        "",
        "## Batches",
        "",
        "| Batch | Tests | Source Chars | Top Files | Top Smells |",
        "|-------|-------|-------------|-----------|------------|",
    ]

    for s in stats:
        top_files = ", ".join(s["files"][:5])
        top_smells = ", ".join(s["smells"][:5]) if s["smells"] else "(none)"
        lines.append(
            f"| {s['filename']} | {s['test_count']} | {s['source_chars']:,} | {top_files} | {top_smells} |"
        )

    lines.extend([
        "",
        "## Known Limitations",
        "",
        "- Source excerpts use a fixed line radius (±30 lines). Method boundaries are not parsed.",
        "- If a test method spans more than 60 lines, the excerpt may be truncated.",
        "- Source files that no longer exist get a placeholder excerpt.",
        "- Priority scoring is heuristic; not all suspicious tests may be in early batches.",
        "- Records with `construction_path: unknown` are not prioritized.",
    ])

    with open(summary_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


def main():
    if not RAW_FILE.exists():
        print(f"ERROR: raw file not found: {RAW_FILE}", file=sys.stderr)
        sys.exit(1)

    # Clean old batch files
    if BATCH_DIR.exists():
        for old_file in BATCH_DIR.glob("batch-*.json"):
            old_file.unlink()
        print(f"Cleaned old batch files from {BATCH_DIR}")

    print(f"Loading raw records from {RAW_FILE}...")
    records = load_raw_records(RAW_FILE)
    print(f"Loaded {len(records)} records.")

    # Validate: no duplicate IDs
    all_ids = [r.get("id", "") for r in records]
    id_counts = Counter(all_ids)
    duplicate_ids = {k: v for k, v in id_counts.items() if v > 1}
    if duplicate_ids:
        print(f"ERROR: {len(duplicate_ids)} duplicate IDs in raw inventory:", file=sys.stderr)
        for k, v in duplicate_ids.items():
            print(f"  DUPE ({v}x): {k}", file=sys.stderr)
        sys.exit(1)

    unique_id_count = len(set(all_ids))
    print(f"Unique IDs: {unique_id_count}")

    print("Sorting by priority...")
    sorted_records = sort_records(records)

    print("Splitting into batches...")
    batches = split_into_batches(sorted_records)
    print(f"Created {len(batches)} batches.")

    print(f"Writing batches to {BATCH_DIR}...")
    stats = write_batches(batches, BATCH_DIR)

    print(f"Writing summary to {SUMMARY_FILE}...")
    write_summary(stats, len(records), unique_id_count, SUMMARY_FILE)

    total_batched = sum(s["test_count"] for s in stats)
    print(f"Done. {total_batched} records in {len(batches)} batches.")


if __name__ == "__main__":
    main()
