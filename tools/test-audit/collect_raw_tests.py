#!/usr/bin/env python3
"""
collect_raw_tests.py — raw test inventory scanner for ClubDoorman.Test.

Scans .cs files in the test project, extracts test methods via regex,
detects infrastructure dependencies, assertion styles, MessageId patterns,
and construction paths.  Writes JSONL + summary.

Usage:
    python3 tools/test-audit/collect_raw_tests.py
"""

import json
import os
import re
import sys
from collections import Counter
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent.parent  # tools/test-audit -> repo
TEST_DIR = REPO_ROOT / "ClubDoorman.Test"
RAW_DIR = REPO_ROOT / ".test-audit" / "raw"
JSONL_OUT = RAW_DIR / "tests.raw.jsonl"
DOTNET_TESTS_OUT = RAW_DIR / "dotnet-tests.txt"
SUMMARY_OUT = RAW_DIR / "raw-inventory-summary.md"

# ---------------------------------------------------------------------------
# Regex patterns — test discovery
# ---------------------------------------------------------------------------

# Match [Test] / [TestCase(...)] / [Theory] attributes (possibly with other attrs)
TEST_ATTR_RE = re.compile(
    r"\[\s*(Test|TestCase|TestCaseSource|Theory)\s*(?:\([^)]*\))?\s*\]",
    re.IGNORECASE,
)

# Match real C# method declarations.
# Requires an access modifier (public/protected/internal/private).
# Group 1: method name.
# This pattern will NOT match control-flow statements (for, foreach, if, etc.)
# because they lack an access modifier and valid return type.
METHOD_RE = re.compile(
    r"^\s*"
    r"(?:public|protected|internal|private)\s+"  # access modifier (required)
    r"(?:static\s+)?"                             # optional static
    r"(?:async\s+)?"                              # optional async
    r"(?:"
        r"(?:Task\s*<[^>]*>|ValueTask\s*<[^>]*>)\s*"  # generic return type
        r"|"
        r"(?:Task|ValueTask|void|bool|int|string|long|double|float|decimal|byte|short|uint|ulong|ushort|sbyte|char|object|dynamic)\s+"  # simple return type
        r"|"
        r"[A-Z]\w*\s+"  # custom type (starts with uppercase)
    r")"
    r"(\w+)"  # group 1: method name
    r"(?:\s*<\w+(?:,\s*\w+)*)?"  # optional generic parameters
    r"\s*\([^)]*\)"              # parameter list
    r"(?:\s*where\s+\w+\s*:\s*\w+(?:\s*,\s*\w+)*)*"  # optional where clauses
    r"\s*\{",                     # opening brace
    re.MULTILINE,
)

# Match [SetUp], [OneTimeSetUp], [TearDown], [OneTimeTearDown]
SETUP_ATTR_RE = re.compile(
    r"\[\s*(SetUp|OneTimeSetUp|TearDown|OneTimeTearDown)\s*\]",
    re.IGNORECASE,
)

# Match [Category("...")]
CATEGORY_ATTR_RE = re.compile(
    r'\[\s*Category\s*\(\s*"([^"]+)"\s*\)\s*\]',
    re.IGNORECASE,
)

# Match [TestFixture]
TEST_FIXTURE_RE = re.compile(
    r"\[\s*TestFixture\s*\]",
    re.IGNORECASE,
)

# ---------------------------------------------------------------------------
# Reserved C# keywords that must never be treated as test method names
# ---------------------------------------------------------------------------
RESERVED_KEYWORDS = frozenset({
    "if", "for", "foreach", "while", "switch", "using", "lock",
    "catch", "finally", "return", "await", "try", "throw", "new",
    "delete", "else", "do", "break", "continue", "case", "default",
    "in", "is", "as", "out", "ref", "yield", "checked", "unchecked",
    "unsafe", "fixed", "stackalloc", "sizeof", "typeof", "nameof",
})

# Match class declarations
CLASS_RE = re.compile(
    r"^\s*(?:public|internal|partial)?\s*class\s+(\w+)",
    re.MULTILINE,
)

# Match namespace
NAMESPACE_RE = re.compile(
    r"^\s*namespace\s+(\S+?);\s*$",
    re.MULTILINE,
)

# ---------------------------------------------------------------------------
# Infrastructure detection patterns
# ---------------------------------------------------------------------------
INFRA_PATTERNS = {
    "uses_message_handler_test_factory": [
        r"MessageHandlerTestFactory",
    ],
    "uses_fake_services_factory": [
        r"FakeServicesFactory",
    ],
    "uses_testkit_message_handler_builder": [
        r"MessageHandlerBuilder",
        r"CreateMessageHandlerBuilder",
    ],
    "uses_testkit_autofixture": [
        r"TestKitAutoFixture",
        r"AutoFixture",
        r"IFixture",
        r"\.CreateAnonymous\(",
        r"\.Build\(",
    ],
    "uses_direct_new_message_handler": [
        r"\bnew\s+MessageHandler\s*\(",
    ],
    "uses_fake_telegram_client": [
        r"FakeTelegramClient",
    ],
    "uses_message_envelope": [
        r"MessageEnvelope",
    ],
    "uses_tk_static_helpers": [
        r"\bTK\.",
    ],
    "uses_service_collection_di": [
        r"ServiceCollection",
        r"AddSingleton\(",
        r"AddScoped\(",
        r"AddTransient\(",
    ],
    "uses_real_service_provider": [
        r"BuildServiceProvider",
        r"GetService\(",
        r"GetRequiredService\(",
        r"IServiceProvider",
    ],
    "uses_app_config_test_factory": [
        r"AppConfigTestFactory",
    ],
    "uses_golden_master_recorder": [
        r"GoldenMasterRecorder",
        r"GoldenMaster",
    ],
    "uses_temp_files_or_baselines": [
        r"Path\.GetTemp",
        r"File\.Read",
        r"File\.Write",
        r"baseline",
        r"Baseline",
    ],
    "uses_real_env_or_api": [
        r"Environment\.GetEnvironmentVariable",
        r"\.env",
        r"TelegramBotClient",
        r"OPENAI",
        r"ANTHROPIC",
    ],
    "uses_moq": [
        r"\bMock\s*<",
        r"\bnew\s+Mock\b",
        r"\bIt\.Is",
    ],
}

# ---------------------------------------------------------------------------
# Assertion detection patterns
# ---------------------------------------------------------------------------
ASSERTION_PATTERNS = {
    "assert_pass": [
        r"Assert\.Pass\b",
    ],
    "does_not_throw": [
        r"Assert\.DoesNotThrow\b",
        r"Assert\.DoesNotThrowAsync\b",
    ],
    "weak_not_null": [
        r"\.NotBeNull\(\)",
        r"Is\.Not\.Null\b",
    ],
    "weak_not_empty": [
        r"\.NotBeEmpty\(\)",
        r"Is\.Not\.Empty\b",
        r"Is\.NotEmpty\b",
    ],
    "mock_verify": [
        r"\.Verify\s*\(",
    ],
    "throws": [
        r"Assert\.Throws\b",
        r"Assert\.ThrowsAsync\b",
    ],
    "state_assertion_suspected": [
        r"Assert\.That\b",
        r"Should\(\)\.Be\b",
        r"Should\(\)\.Contain\b",
        r"Assert\.AreEqual\b",
        r"Assert\.AreNotEqual\b",
        r"Assert\.IsTrue\b",
        r"Assert\.IsFalse\b",
        r"Should\(\)\.BeTrue\b",
        r"Should\(\)\.BeFalse\b",
        r"Should\(\)\.BeEquivalentTo\b",
        r"Should\(\)\.HaveCount\b",
        r"Should\(\)\.BeOneOf\b",
        r"Should\(\)\.NotBeNullOrEmpty\b",
        r"Should\(\)\.BeGreaterThan\b",
        r"Should\(\)\.BeLessThan\b",
        r"Should\(\)\.BeLessThanOrEqualTo\b",
        r"Should\(\)\.BeEquivalentTo\b",
    ],
}

# ---------------------------------------------------------------------------
# MessageId detection patterns
# ---------------------------------------------------------------------------
MSGID_PATTERNS = {
    "uses_with_message_id": [
        r"WithMessageId\s*\(",
    ],
    "reads_message_message_id": [
        r"\.Message\.MessageId\b",
        r"message\.MessageId\b",
        r"msg\.MessageId\b",
    ],
    "uses_envelope_message_id": [
        r"envelope\.MessageId\b",
        r"Envelope\.MessageId\b",
    ],
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def find_test_files() -> list[Path]:
    """Find all .cs files in the test project, excluding generated/obj."""
    files = []
    for cs in TEST_DIR.rglob("*.cs"):
        # Skip generated / obj / bin / feature-generated files
        if "obj/" in str(cs) or "bin/" in str(cs):
            continue
        # Skip SpecFlow-generated feature.cs files
        if cs.name.endswith(".feature.cs"):
            continue
        files.append(cs)
    return sorted(files)


def extract_namespace(content: str) -> str:
    """Extract namespace from file content."""
    m = NAMESPACE_RE.search(content)
    return m.group(1) if m else ""


def extract_classes(content: str) -> list[tuple[str, int]]:
    """Extract class names and their line numbers."""
    classes = []
    for m in CLASS_RE.finditer(content):
        line = content[: m.start()].count("\n") + 1
        classes.append((m.group(1), line))
    return classes


def extract_categories_before(pos: int, content: str) -> list[str]:
    """Extract [Category("...")] attributes before position pos."""
    # Look back up to 2000 chars
    start = max(0, pos - 2000)
    text = content[start:pos]
    return CATEGORY_ATTR_RE.findall(text)


def extract_test_attrs_before(pos: int, content: str) -> list[str]:
    """Extract test attribute names before position pos (legacy, kept for setup extraction)."""
    start = max(0, pos - 500)
    text = content[start:pos]
    return TEST_ATTR_RE.findall(text)


def find_method_after_attr(content: str, attr_end: int, max_lines: int = 20) -> re.Match | None:
    """Find a real C# method declaration after a test attribute.

    Scans forward from attr_end up to max_lines for a method declaration.
    Returns None if no valid method found within the window.
    """
    # Get the text window after the attribute
    window = content[attr_end : attr_end + 2000]
    window_lines = window.split("\n")

    # Limit to max_lines
    if len(window_lines) > max_lines:
        window = "\n".join(window_lines[:max_lines])

    m = METHOD_RE.search(window)
    if m and m.group(1) not in RESERVED_KEYWORDS:
        # Return match with absolute position
        abs_match = METHOD_RE.search(content[attr_end:])
        if abs_match:
            return abs_match
    return None


def collect_test_attr_blocks(content: str) -> list[tuple[list[str], int]]:
    """Find test attribute blocks and return (attr_names, position_after_last_attr).

    Groups consecutive test-related attributes into blocks.
    """
    blocks = []
    current_attrs = []
    last_attr_end = 0

    for m in TEST_ATTR_RE.finditer(content):
        attr_name = m.group(1)
        # Check if this attribute is close to the previous one (within 200 chars)
        if current_attrs and m.start() - last_attr_end < 200:
            current_attrs.append(attr_name)
        else:
            if current_attrs:
                blocks.append((current_attrs, last_attr_end))
            current_attrs = [attr_name]
        last_attr_end = m.end()

    if current_attrs:
        blocks.append((current_attrs, last_attr_end))

    return blocks


def extract_method_body(content: str, method_start: int) -> tuple[str, int]:
    """Extract method body text and line count by matching braces.

    Returns (body_text, body_lines).
    """
    # Find the opening brace
    brace_pos = content.find("{", method_start)
    if brace_pos < 0:
        return "", 0

    brace_depth = 0
    body_start = brace_pos
    for i in range(brace_pos, len(content)):
        ch = content[i]
        if ch == "{":
            brace_depth += 1
        elif ch == "}":
            brace_depth -= 1
            if brace_depth == 0:
                body_end = i + 1
                body_text = content[body_start:body_end]
                body_lines = body_text.count("\n") + 1
                return body_text, body_lines

    # If we didn't find matching brace, return what we have
    body_text = content[body_start:body_start + 5000]
    body_lines = body_text.count("\n") + 1
    return body_text, body_lines


def get_declared_area_from_path(file_path: Path) -> str:
    """Derive area from file path."""
    rel = str(file_path.relative_to(TEST_DIR))
    parts = rel.split("/")
    if "Unit/" in rel:
        return "Unit"
    if "Integration/" in rel:
        return "Integration"
    if "StepDefinitions/" in rel:
        return "StepDefinitions"
    if "TestKit/" in rel:
        return "TestKit"
    if "TestInfrastructure/" in rel:
        return "TestInfrastructure"
    if "GoldenMaster/" in rel:
        return "GoldenMaster"
    if "TestData/" in rel:
        return "TestData"
    # Root-level test files
    return "Root"


def detect_infra(body_text: str, setup_text: str) -> dict:
    """Detect infrastructure dependencies in test body + setup."""
    combined = body_text + "\n" + setup_text
    result = {}
    for key, patterns in INFRA_PATTERNS.items():
        result[key] = any(re.search(p, combined, re.IGNORECASE) for p in patterns)
    return result


def detect_assertions(body_text: str) -> dict:
    """Detect assertion patterns in test body."""
    result = {}
    for key, patterns in ASSERTION_PATTERNS.items():
        result[key] = any(re.search(p, body_text) for p in patterns)
    return result


def detect_message_id(body_text: str) -> dict:
    """Detect MessageId-sensitive patterns."""
    result = {}
    for key, patterns in MSGID_PATTERNS.items():
        result[key] = any(re.search(p, body_text) for p in patterns)
    # id_sensitive_suspected: any MessageId pattern detected
    result["id_sensitive_suspected"] = any(result.values())
    return result


def determine_construction_path(infra: dict) -> str:
    """Determine construction path based on detected infrastructure."""
    if infra.get("uses_direct_new_message_handler"):
        return "direct_new_message_handler"
    if infra.get("uses_message_handler_test_factory"):
        return "legacy_message_handler_test_factory"
    if infra.get("uses_fake_services_factory"):
        return "fake_services_factory"
    if infra.get("uses_testkit_message_handler_builder"):
        return "testkit_message_handler_builder"
    if infra.get("uses_testkit_autofixture"):
        return "testkit_autofixture"
    if infra.get("uses_service_collection_di"):
        return "service_collection_di"
    # custom_local_world: many mocks/services but no known factory
    infra_hits = sum(1 for k, v in infra.items() if v)
    if infra_hits >= 3:
        return "custom_local_world"
    if infra_hits == 0:
        return "none_or_pure"
    return "unknown"


def detect_smells(infra: dict, assertions: dict, body_lines: int, test_attrs: list[str]) -> list[str]:
    """Detect test smells."""
    smells = []
    if assertions.get("assert_pass"):
        smells.append("assert_pass")
    if assertions.get("does_not_throw"):
        smells.append("does_not_throw")
    if assertions.get("weak_not_null") and not assertions.get("state_assertion_suspected"):
        smells.append("weak_not_null_only")
    if assertions.get("weak_not_empty") and not assertions.get("state_assertion_suspected"):
        smells.append("weak_not_empty_only")
    if assertions.get("mock_verify"):
        smells.append("mock_verify")
    if infra.get("uses_direct_new_message_handler"):
        smells.append("direct_new_message_handler")
    if infra.get("uses_message_handler_test_factory"):
        smells.append("message_handler_test_factory")
    if infra.get("uses_fake_services_factory"):
        smells.append("fake_services_factory")
    if infra.get("uses_testkit_autofixture"):
        smells.append("testkit_autofixture")
    if infra.get("uses_real_env_or_api"):
        smells.append("real_env_or_api")
    if body_lines > 80:
        smells.append("long_test_body")
    if body_lines < 5:
        smells.append("very_short_test")
    # TestCase without TestCaseSource — multiple test cases in one method
    if "TestCase" in test_attrs and len(test_attrs) > 2:
        smells.append("multiple_test_cases")
    return smells


def extract_setup_text(content: str, class_start: int, test_method_pos: int) -> str:
    """Extract SetUp/OneTimeSetUp methods between class start and test method."""
    setup_text = ""
    segment = content[class_start:test_method_pos]
    # Find [SetUp] or [OneTimeSetUp] methods
    for m in SETUP_ATTR_RE.finditer(segment):
        # Find the method after this attribute
        rest = segment[m.end():]
        method_m = METHOD_RE.search(rest)
        if method_m:
            # Extract text around the setup method
            setup_text += segment[m.end() : m.end() + 2000] + "\n"
    return setup_text


def scan_file(file_path: Path) -> list[dict]:
    """Scan a single file for test methods.

    Uses forward-scanning: finds test attributes first, then looks forward
    for the method declaration. This avoids matching control-flow statements
    inside method bodies.
    """
    content = file_path.read_text(encoding="utf-8", errors="replace")

    # Quick check: does file have any test attributes?
    if not TEST_ATTR_RE.search(content):
        return []

    namespace = extract_namespace(content)
    rel_path = str(file_path.relative_to(TEST_DIR))
    declared_area = get_declared_area_from_path(file_path)

    # Find all classes
    classes = extract_classes(content)

    # Find test attribute blocks, then scan forward for method declarations
    attr_blocks = collect_test_attr_blocks(content)
    tests = []
    seen_positions = set()

    for attr_names, attr_end in attr_blocks:
        # Find method declaration after this attribute block
        method_m = find_method_after_attr(content, attr_end)
        if not method_m:
            continue

        # Absolute position of the method in the file
        method_pos = attr_end + method_m.start()

        # Skip if we already processed this position (e.g., TestCase + Test on same method)
        if method_pos in seen_positions:
            continue
        seen_positions.add(method_pos)

        method_name = method_m.group(1)

        # Reserved keyword guard
        if method_name in RESERVED_KEYWORDS:
            continue

        # Determine which class this method belongs to
        class_name = ""
        class_start = 0
        for cname, cline in classes:
            cpos = content.find(cname, 0)
            if cpos < method_pos:
                class_name = cname
                class_start = cpos
            else:
                break

        line_num = content[:method_pos].count("\n") + 1

        # Get test body text (precise brace matching)
        body_text, body_lines = extract_method_body(content, method_pos)

        # Get setup text
        setup_text = extract_setup_text(content, class_start, method_pos)

        # Categories from attributes (look back from method position)
        categories = extract_categories_before(method_pos, content)

        # Detect infrastructure
        infra = detect_infra(body_text, setup_text)

        # Detect assertions
        assertions = detect_assertions(body_text)

        # Detect MessageId patterns
        message_id = detect_message_id(body_text)

        # Determine construction path
        construction_path = determine_construction_path(infra)

        # Detect smells
        smells = detect_smells(infra, assertions, body_lines, attr_names)

        # Build record
        record = {
            "schema_version": "test-raw-v1",
            "id": f"ClubDoorman.Test/{rel_path}::{class_name}::{method_name}",
            "file": rel_path,
            "namespace": namespace,
            "class": class_name,
            "test_name": method_name,
            "line": line_num,
            "attributes": attr_names,
            "categories": categories,
            "body_lines": body_lines,
            "declared_area_from_path": declared_area,
            "construction_path": construction_path,
            "infra": infra,
            "assertions": assertions,
            "message_id": message_id,
            "smells": smells,
        }
        tests.append(record)

    return tests


def generate_summary(records: list[dict], dotnet_count: int | None = None,
                     duplicate_id_count: int = 0, keyword_fp_count: int = 0) -> str:
    """Generate summary markdown."""
    total = len(records)
    unique_ids = len(set(r["id"] for r in records))

    # Count by construction_path
    path_counts = Counter(r["construction_path"] for r in records)

    # Count by file
    file_counts = Counter(r["file"] for r in records)
    top_files = file_counts.most_common(10)

    # Smell counts
    smell_counts = Counter()
    for r in records:
        for s in r["smells"]:
            smell_counts[s] += 1

    # Infra counts
    infra_counts = Counter()
    for r in records:
        for k, v in r["infra"].items():
            if v:
                infra_counts[k] += 1

    # Area counts
    area_counts = Counter(r["declared_area_from_path"] for r in records)

    # Category counts
    cat_counts = Counter()
    for r in records:
        for c in r["categories"]:
            cat_counts[c] += 1

    lines = [
        "# Raw Test Inventory Summary",
        "",
        f"Generated by `tools/test-audit/collect_raw_tests.py`",
        "",
        "## Counts",
        "",
        f"- **Total raw test methods discovered:** {total}",
        f"- **Unique IDs:** {unique_ids}",
        f"- **Duplicate IDs:** {duplicate_id_count}",
        f"- **Reserved keyword false positives:** {keyword_fp_count}",
        "",
        "### By construction path",
        "",
    ]

    for path, count in path_counts.most_common():
        lines.append(f"- `{path}`: {count}")

    lines.append("")
    lines.append("### By declared area")
    lines.append("")
    for area, count in area_counts.most_common():
        lines.append(f"- `{area}`: {count}")

    lines.append("")
    lines.append("### Top files by test count")
    lines.append("")
    for f, count in top_files:
        lines.append(f"- `{f}`: {count}")

    lines.append("")
    lines.append("### Key smells")
    lines.append("")

    smell_labels = {
        "assert_pass": "Assert.Pass",
        "does_not_throw": "DoesNotThrow",
        "mock_verify": "Mock.Verify",
        "direct_new_message_handler": "direct new MessageHandler",
        "message_handler_test_factory": "MessageHandlerTestFactory",
        "fake_services_factory": "FakeServicesFactory",
        "testkit_autofixture": "TestKitAutoFixture",
        "real_env_or_api": "real env or API",
        "long_test_body": "long test body (>80 lines)",
        "very_short_test": "very short test (<5 lines)",
        "multiple_test_cases": "multiple TestCase attributes",
        "weak_not_null_only": "weak NotBeNull only",
        "weak_not_empty_only": "weak NotBeEmpty only",
    }

    for smell, count in smell_counts.most_common():
        label = smell_labels.get(smell, smell)
        lines.append(f"- `{label}`: {count}")

    lines.append("")
    lines.append("### Key infrastructure usage")
    lines.append("")
    for infra_key, count in infra_counts.most_common():
        lines.append(f"- `{infra_key}`: {count}")

    lines.append("")
    lines.append("### Categories")
    lines.append("")
    for cat, count in cat_counts.most_common():
        lines.append(f"- `{cat}`: {count}")

    lines.append("")
    lines.append("### dotnet test --list-tests comparison")
    lines.append("")
    lines.append("_Populated after running `dotnet test --list-tests`._")
    lines.append("")
    lines.append("## Sample Records")
    lines.append("")
    lines.append("First 10 records from `tests.raw.jsonl`:")
    lines.append("")

    for r in records[:10]:
        lines.append(f"```json")
        lines.append(json.dumps(r, indent=2, ensure_ascii=False))
        lines.append(f"```")
        lines.append("")

    return "\n".join(lines)


def main():
    print(f"Scanning test project: {TEST_DIR}")

    if not TEST_DIR.exists():
        print(f"ERROR: Test directory not found: {TEST_DIR}", file=sys.stderr)
        sys.exit(1)

    # Ensure output dir exists
    RAW_DIR.mkdir(parents=True, exist_ok=True)

    # Find test files
    test_files = find_test_files()
    print(f"Found {len(test_files)} .cs files")

    # Scan all files
    all_records: list[dict] = []
    for f in test_files:
        records = scan_file(f)
        all_records.extend(records)

    print(f"Discovered {len(all_records)} test methods")

    # --- Validation ---
    all_ids = [r["id"] for r in all_records]
    id_counts = Counter(all_ids)
    duplicate_ids = {k: v for k, v in id_counts.items() if v > 1}
    duplicate_id_count = sum(v - 1 for v in duplicate_ids.values())  # extra occurrences

    keyword_fps = [r for r in all_records if r["test_name"] in RESERVED_KEYWORDS]
    keyword_fp_count = len(keyword_fps)

    print(f"Unique IDs: {len(set(all_ids))}")
    print(f"Duplicate IDs: {duplicate_id_count}")
    print(f"Reserved keyword false positives: {keyword_fp_count}")

    if duplicate_id_count > 0:
        print(f"ERROR: {duplicate_id_count} duplicate IDs detected:", file=sys.stderr)
        for k, v in duplicate_ids.items():
            print(f"  DUPE ({v}x): {k}", file=sys.stderr)
        sys.exit(1)

    if keyword_fp_count > 0:
        print(f"ERROR: {keyword_fp_count} reserved keyword test names detected:", file=sys.stderr)
        for r in keyword_fps:
            print(f"  KEYWORD: {r['test_name']} at {r['file']}:{r['line']}", file=sys.stderr)
        sys.exit(1)

    # Write JSONL
    with open(JSONL_OUT, "w", encoding="utf-8") as fh:
        for r in all_records:
            fh.write(json.dumps(r, ensure_ascii=False) + "\n")

    print(f"Wrote {JSONL_OUT}")

    # Generate summary (without dotnet comparison yet)
    summary = generate_summary(all_records, duplicate_id_count=duplicate_id_count, keyword_fp_count=keyword_fp_count)

    # Try to run dotnet list-tests for comparison
    dotnet_count = None
    dotnet_notes = ""
    try:
        import subprocess

        result = subprocess.run(
            ["dotnet", "test", str(REPO_ROOT / "ClubDoorman.Test/ClubDoorman.Test.csproj"), "--no-restore", "--list-tests"],
            capture_output=True,
            text=True,
            timeout=120,
        )
        dotnet_output = result.stdout + result.stderr

        # Write dotnet output
        with open(DOTNET_TESTS_OUT, "w", encoding="utf-8") as fh:
            fh.write(dotnet_output)

        # Count listed tests — look for lines that look like test names
        # NUnit list-tests output: "    FullyQualified.ClassName.TestMethodName"
        test_lines = []
        for line in dotnet_output.split("\n"):
            stripped = line.strip()
            # Skip headers, empty lines, info lines
            if not stripped:
                continue
            if stripped.startswith("The following Tests"):
                continue
            if stripped.startswith("Total"):
                continue
            if stripped.startswith("info:") or stripped.startswith("Build"):
                continue
            if stripped.startswith("NUnit"):
                continue
            if stripped.startswith("Test run"):
                continue
            # Match test name patterns: namespace.Class.Method or namespace.Class.Method(param)
            if re.match(r"^\s*\w[\w.]*\w", stripped):
                test_lines.append(stripped)

        # Also try to extract "Total tests: N" from the output
        total_match = re.search(r"Total tests:\s*(\d+)", dotnet_output)
        if total_match:
            dotnet_count = int(total_match.group(1))
        else:
            dotnet_count = len(test_lines)

        print(f"dotnet test --list-tests: {dotnet_count} tests listed")
        print(f"Wrote {DOTNET_TESTS_OUT}")

        # Update summary with dotnet comparison
        mismatch = abs(len(all_records) - dotnet_count) if dotnet_count else None
        dotnet_section = f"- **Total dotnet listed tests:** {dotnet_count}\n"
        if mismatch is not None:
            dotnet_section += f"- **Mismatch:** {mismatch} (raw: {len(all_records)}, dotnet: {dotnet_count})\n"
            if mismatch > 0:
                ratio = dotnet_count / len(all_records) if len(all_records) > 0 else 0
                if ratio > 1.5:
                    dotnet_section += (
                        "- **Note:** dotnet count is significantly higher. "
                        "This is expected when `[TestCase]` attributes expand into multiple test cases. "
                        "Raw inventory counts methods, not expanded cases.\n"
                    )
                elif ratio < 0.5:
                    dotnet_section += (
                        "- **Note:** dotnet count is significantly lower. "
                        "May indicate parser missed tests or dotnet excluded tests via filters.\n"
                    )
                else:
                    dotnet_section += (
                        "- **Note:** Counts are within reasonable range. "
                        "Differences are expected due to TestCase expansion, disabled tests, or parser limitations.\n"
                    )

        # Insert dotnet section into summary
        summary = summary.replace(
            "_Populated after running `dotnet test --list-tests`._",
            dotnet_section.strip(),
        )

    except FileNotFoundError:
        dotnet_notes = "dotnet not found — skipped list-tests comparison"
        print(f"WARNING: {dotnet_notes}", file=sys.stderr)
    except subprocess.TimeoutExpired:
        dotnet_notes = "dotnet test --list-tests timed out (120s)"
        print(f"WARNING: {dotnet_notes}", file=sys.stderr)
    except Exception as e:
        dotnet_notes = f"dotnet test --list-tests failed: {e}"
        print(f"WARNING: {dotnet_notes}", file=sys.stderr)

    if not dotnet_count:
        summary = summary.replace(
            "_Populated after running `dotnet test --list-tests`._",
            f"- **dotnet test --list-tests:** {dotnet_notes or 'not run'}",
        )

    # Write summary
    with open(SUMMARY_OUT, "w", encoding="utf-8") as fh:
        fh.write(summary)

    print(f"Wrote {SUMMARY_OUT}")
    print(f"\nDone. {len(all_records)} tests inventoried.")


if __name__ == "__main__":
    main()
