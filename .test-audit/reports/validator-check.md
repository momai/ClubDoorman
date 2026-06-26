# Validator Check

## Commands run

```bash
python3 tools/test-audit/validate_audit.py .test-audit/audit/example.audit.jsonl
python3 tools/test-audit/validate_audit.py .test-audit/audit/*.audit.jsonl
python3 tools/test-audit/validate_audit.py --require-complete .test-audit/audit/*.audit.jsonl
python3 -m json.tool tools/test-audit/audit.schema.json >/tmp/opencode/audit.schema.checked.json
```

## Example file validation result

```text
validated_files=1 audited_records=3 unique_audited_ids=3 raw_ids=847 warnings=0 errors=0
```

Glob input produced the same result:

```text
validated_files=1 audited_records=3 unique_audited_ids=3 raw_ids=847 warnings=0 errors=0
```

Strict completeness mode failed as expected for the partial example file:

```text
ERROR: require-complete: missing raw ids: 844
validated_files=1 audited_records=3 unique_audited_ids=3 raw_ids=847 warnings=0 errors=1
```

## Raw ID lookup

Raw ID lookup works. The validator loaded `.test-audit/raw/tests.raw.jsonl`, counted 847 raw IDs, and accepted the 3 example audit IDs as present in the raw inventory.

## Warnings produced

No warnings were produced by the example audit file.

The validator emits a warning, not a failure, when `bot_owner_value == owner_would_swear` and `decision == keep`.

## Known limitations

- Validation is implemented with the Python standard library instead of a JSON Schema engine.
- `tools/test-audit/audit.schema.json` is the documented schema contract; `validate_audit.py` mirrors the schema manually and adds cross-record/raw-ID rules.
- Completeness is optional. Without `--require-complete`, partial audit files are valid.
- Duplicate IDs are always rejected inside one audit file. Duplicates across multiple input files are rejected only by `--require-complete`.
