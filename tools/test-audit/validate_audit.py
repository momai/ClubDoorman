#!/usr/bin/env python3
"""
validate_audit.py - validate local LLM test audit JSONL output.

Usage:
    python3 tools/test-audit/validate_audit.py .test-audit/audit/example.audit.jsonl
    python3 tools/test-audit/validate_audit.py .test-audit/audit/*.audit.jsonl
    python3 tools/test-audit/validate_audit.py --require-complete .test-audit/audit/*.audit.jsonl

This script does not run an LLM, classify tests, or modify test files.
"""

from __future__ import annotations

import argparse
import glob
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parent.parent.parent
RAW_FILE = REPO_ROOT / ".test-audit" / "raw" / "tests.raw.jsonl"
SCHEMA_FILE = REPO_ROOT / "tools" / "test-audit" / "audit.schema.json"

EXPECTED_SCHEMA_VERSION = "test-audit-v1"

DECLARED_AREA = {
    "MessageHandler",
    "MessagePipeline",
    "PipelineStep",
    "UserBanService",
    "ModerationFacade",
    "CommandRouter",
    "MessageService",
    "NotificationService",
    "FakeTelegramClient",
    "DI",
    "Pure",
    "Integration",
    "TestInfrastructure",
    "Unknown",
}

ACTUAL_SEAM = DECLARED_AREA | {"Multiple"}

ASSERTION_TYPE = {
    "strong_behavior",
    "state_assertion",
    "fake_client_tracking",
    "envelope_tracking",
    "mock_choreography",
    "does_not_throw",
    "no_meaningful_assertion",
    "compile_smoke",
    "snapshot_or_golden",
    "logs",
    "unknown",
}

VALUE_LEVEL = {"high", "medium", "low", "unknown"}
BOT_OWNER_VALUE = {"owner_would_thank_us", "owner_would_swear", "unclear"}
DECISION = {"keep", "rewrite", "delete", "quarantine", "unknown"}
CONFIDENCE = {"high", "medium", "low"}

REQUIRED_FIELDS = {
    "schema_version",
    "id",
    "declared_area",
    "actual_seam",
    "test_intent",
    "assertion_type",
    "behavior_value",
    "maintenance_risk",
    "bot_owner_value",
    "decision",
    "replacement_needed",
    "replacement_seam",
    "reason",
    "confidence",
    "needs_human_review",
}

STRING_FIELDS = {
    "schema_version",
    "id",
    "declared_area",
    "actual_seam",
    "test_intent",
    "assertion_type",
    "behavior_value",
    "maintenance_risk",
    "bot_owner_value",
    "decision",
    "reason",
    "confidence",
}


def load_raw_ids(path: Path) -> set[str]:
    ids: set[str] = set()
    duplicates: list[str] = []

    with path.open("r", encoding="utf-8") as f:
        for lineno, raw_line in enumerate(f, 1):
            line = raw_line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError as e:
                raise ValueError(f"raw inventory line {lineno}: invalid JSON: {e}") from e

            raw_id = obj.get("id")
            if not isinstance(raw_id, str) or not raw_id:
                raise ValueError(f"raw inventory line {lineno}: missing non-empty string id")
            if raw_id in ids:
                duplicates.append(raw_id)
            ids.add(raw_id)

    if duplicates:
        dupes = ", ".join(sorted(set(duplicates))[:5])
        raise ValueError(f"raw inventory contains duplicate ids: {dupes}")
    return ids


def expand_inputs(patterns: list[str]) -> list[Path]:
    paths: list[Path] = []
    for pattern in patterns:
        matches = glob.glob(pattern)
        if matches:
            paths.extend(Path(m) for m in matches)
        else:
            paths.append(Path(pattern))
    return sorted(paths)


def loc(path: Path, lineno: int, message: str) -> str:
    return f"{path}:{lineno}: {message}"


def validate_shape(path: Path, lineno: int, obj: Any) -> list[str]:
    errors: list[str] = []

    if not isinstance(obj, dict):
        return [loc(path, lineno, "line must be a JSON object")]

    keys = set(obj.keys())
    missing = sorted(REQUIRED_FIELDS - keys)
    extra = sorted(keys - REQUIRED_FIELDS)
    if missing:
        errors.append(loc(path, lineno, f"missing required fields: {', '.join(missing)}"))
    if extra:
        errors.append(loc(path, lineno, f"unexpected fields: {', '.join(extra)}"))

    for field in sorted(STRING_FIELDS & keys):
        if not isinstance(obj[field], str) or obj[field] == "":
            errors.append(loc(path, lineno, f"{field} must be a non-empty string"))

    if "replacement_needed" in obj and not isinstance(obj["replacement_needed"], bool):
        errors.append(loc(path, lineno, "replacement_needed must be a boolean"))
    if "needs_human_review" in obj and not isinstance(obj["needs_human_review"], bool):
        errors.append(loc(path, lineno, "needs_human_review must be a boolean"))

    if "replacement_seam" in obj and not (
        isinstance(obj["replacement_seam"], str) or obj["replacement_seam"] is None
    ):
        errors.append(loc(path, lineno, "replacement_seam must be a string or null"))

    enum_checks = {
        "declared_area": DECLARED_AREA,
        "actual_seam": ACTUAL_SEAM,
        "assertion_type": ASSERTION_TYPE,
        "behavior_value": VALUE_LEVEL,
        "maintenance_risk": VALUE_LEVEL,
        "bot_owner_value": BOT_OWNER_VALUE,
        "decision": DECISION,
        "confidence": CONFIDENCE,
    }
    for field, allowed in enum_checks.items():
        if field in obj and isinstance(obj[field], str) and obj[field] not in allowed:
            errors.append(loc(path, lineno, f"{field} has invalid value {obj[field]!r}"))

    if obj.get("schema_version") != EXPECTED_SCHEMA_VERSION:
        errors.append(loc(path, lineno, f"schema_version must be {EXPECTED_SCHEMA_VERSION!r}"))

    return errors


def validate_business_rules(path: Path, lineno: int, obj: dict[str, Any], raw_ids: set[str]) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []

    audit_id = obj.get("id")
    if isinstance(audit_id, str) and audit_id not in raw_ids:
        errors.append(loc(path, lineno, f"id is not present in raw inventory: {audit_id}"))

    decision = obj.get("decision")
    if decision == "rewrite":
        if obj.get("replacement_needed") is not True:
            errors.append(loc(path, lineno, "decision=rewrite requires replacement_needed=true"))
        replacement_seam = obj.get("replacement_seam")
        if not isinstance(replacement_seam, str) or replacement_seam.strip() == "":
            errors.append(loc(path, lineno, "decision=rewrite requires non-empty replacement_seam"))

    if obj.get("confidence") == "low" and obj.get("needs_human_review") is not True:
        errors.append(loc(path, lineno, "confidence=low requires needs_human_review=true"))

    if obj.get("behavior_value") == "high" and decision == "delete" and obj.get("needs_human_review") is not True:
        errors.append(loc(path, lineno, "behavior_value=high and decision=delete requires needs_human_review=true"))

    if obj.get("bot_owner_value") == "owner_would_swear" and decision == "keep":
        warnings.append(loc(path, lineno, "bot_owner_value=owner_would_swear with decision=keep"))

    return errors, warnings


def validate_file(path: Path, raw_ids: set[str]) -> tuple[list[str], list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []
    ids: list[str] = []

    if not path.is_file():
        return [f"{path}: file not found"], warnings, ids

    with path.open("r", encoding="utf-8") as f:
        for lineno, raw_line in enumerate(f, 1):
            line = raw_line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError as e:
                errors.append(loc(path, lineno, f"invalid JSON: {e}"))
                continue

            shape_errors = validate_shape(path, lineno, obj)
            errors.extend(shape_errors)
            if isinstance(obj, dict):
                if isinstance(obj.get("id"), str):
                    ids.append(obj["id"])
                rule_errors, rule_warnings = validate_business_rules(path, lineno, obj, raw_ids)
                errors.extend(rule_errors)
                warnings.extend(rule_warnings)

    counts = Counter(ids)
    for audit_id, count in sorted(counts.items()):
        if count > 1:
            errors.append(f"{path}: duplicate id inside file ({count}x): {audit_id}")

    return errors, warnings, ids


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate test audit JSONL files.")
    parser.add_argument("audit_files", nargs="+", help="Audit JSONL files or glob patterns")
    parser.add_argument(
        "--require-complete",
        action="store_true",
        help="Fail unless the combined input covers every raw id exactly once",
    )
    args = parser.parse_args()

    if not SCHEMA_FILE.is_file():
        print(f"ERROR: schema file not found: {SCHEMA_FILE}", file=sys.stderr)
        return 2
    if not RAW_FILE.is_file():
        print(f"ERROR: raw inventory file not found: {RAW_FILE}", file=sys.stderr)
        return 2

    try:
        raw_ids = load_raw_ids(RAW_FILE)
    except ValueError as e:
        print(f"ERROR: {e}", file=sys.stderr)
        return 2

    audit_paths = expand_inputs(args.audit_files)
    errors: list[str] = []
    warnings: list[str] = []
    all_ids: list[str] = []

    for path in audit_paths:
        file_errors, file_warnings, file_ids = validate_file(path, raw_ids)
        errors.extend(file_errors)
        warnings.extend(file_warnings)
        all_ids.extend(file_ids)

    if args.require_complete:
        counts = Counter(all_ids)
        missing = sorted(raw_ids - set(all_ids))
        duplicate = sorted(audit_id for audit_id, count in counts.items() if count > 1)
        if missing:
            errors.append(f"require-complete: missing raw ids: {len(missing)}")
        if duplicate:
            errors.append(f"require-complete: duplicate audited ids across inputs: {len(duplicate)}")

    for warning in warnings:
        print(f"WARNING: {warning}", file=sys.stderr)
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)

    unique_ids = len(set(all_ids))
    print(
        f"validated_files={len(audit_paths)} audited_records={len(all_ids)} "
        f"unique_audited_ids={unique_ids} raw_ids={len(raw_ids)} "
        f"warnings={len(warnings)} errors={len(errors)}"
    )

    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
