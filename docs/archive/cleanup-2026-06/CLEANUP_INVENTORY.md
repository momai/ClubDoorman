# Inventory Report — cleanup/repo-hygiene

Generated: 2026-06-25
Baseline tracked files: **692**

---

## 1. Tracked file counts by area

| Area | Count |
|---|---|
| `ClubDoorman/` (main source) | 228 |
| `ClubDoorman.Test/` (tests) | 239 |
| `ClubDoorman.Baseline/` (golden baseline tool + data) | 59 |
| `docs/` + `docs/scenarios/` | 21 |
| `scripts/` | 37 |
| `plans/` (refactor plans + worklogs) | 17 |
| `.github/` (CI/CD) | 8 |
| `.cursor/rules/` | 6 |
| `.refactor-mcp/` (MCP metrics + tool-call-log) | 9 |
| `.understand-anything/` | 3 |
| Root-level tracked files | 65 |
| **Total** | **692** |

---

## 2. Generated junk — candidates for removal

| Category | Count | Files |
|---|---|---|
| `__pycache__/` + `.pyc` | 15 | `scripts/test_factory_generator/__pycache__/*` |
| `.backup` files | 2 | `ClubDoorman.Test/TestData.cs.backup`, `ClubDoorman.Test/TestData/TestDataFactory.Generated.cs.backup` |
| Zone.Identifier streams (macOS/Windows artifact) | 4 | `Схема работы.drawio*` |
| `.drawio` + `.drawio.png` (diagrams, tracked but not source) | 2 | `Схема работы.drawio`, `Схема работы.drawio.png` |
| `test_bot.sh.example` (example, tracked) | 1 | root |
| `test_check.sh` (small test script, root) | 1 | root |
| `dotnet-install.sh` (installer, tracked) | 1 | root |
| **Total** | **30** | |

---

## 3. Docs / worklog / report candidates (not in `docs/`)

These are `.md`, `.txt`, `.json` files at root or scattered that look like
worklogs, implementation summaries, or analysis artifacts:

| File | Type |
|---|---|
| `IMPLEMENTATION_SUMMARY.md` | Implementation summary |
| `TESTING_IMPLEMENTATION_SUMMARY.md` | Testing implementation summary |
| `PRODUCTION_CHANGES_DOCUMENTATION.md` | Production changes doc |
| `WORKLOG_CHANNEL_EFFECTS_MIGRATION.md` | Worklog |
| `WORKLOG_MESSAGE_PIPELINE.md` | Worklog |
| `SUSPICIOUS_USERS_TODO.md` | Todo list |
| `AI_PROFILE_CHECKS.md` | Checklist |
| `FOLDER_INVITE_BAN.md` | Feature doc (could stay or move to `docs/`) |
| `CHANGELOG.md` | Changelog (keep at root) |
| `README.md` | Main readme (keep at root) |
| `SUSPICIOUS_USERS_SETUP.md` | Setup guide (could move to `docs/`) |
| `remaining_services_analysis.txt` | Analysis artifact |
| `test-analysis.json` | Analysis artifact |
| `cd-index-short.json` | Index artifact |
| `System.NullReferenceException` | **Accidental file** (0 bytes, named after exception) |

---

## 4. `.gitignore` status

Current `.gitignore` already covers `bin/`, `obj/`, `*.user`, `*.suo`, etc.
But the following are **tracked** despite being generated/junk:

- `__pycache__/` files (15 files) — should be gitignored
- `.backup` files — should be gitignored
- `Zone.Identifier` — should be gitignored
- `dotnet-install.sh` — this is a downloaded installer, debatable
- `test_bot.sh.example`, `test_check.sh` — small scripts, debatable

---

## 5. `.cursor/` and `.refactor-mcp/` and `.understand-anything/`

These are tool-specific directories tracked in git:

| Dir | Files | Notes |
|---|---|---|
| `.cursor/rules/` | 6 (3 `.mdc`) | Cursor IDE rules — probably shouldn't be tracked |
| `.refactor-mcp/` | 9 (metrics JSON + tool-call-log) | MCP metrics — generated, shouldn't be tracked |
| `.understand-anything/` | 3 (Python + JSON) | Understand tool artifacts — shouldn't be tracked |

---

## 6. `plans/` directory

17 files across two plan groups:
- `plans/current/refactor-2025/` — 12 files (5 plan.md + 5 worklog.md + 2 overview/checklist)
- `plans/refactoring_master/` — 5 files (architectural analysis, master plan, folder structure, safety measures, testkit analysis)

These are historical planning artifacts. Consider archiving or removing if superseded.

---

## 7. Suggested actions (summary)

### Remove (junk / generated):
- All `__pycache__/` files (15)
- `.backup` files (2)
- `Zone.Identifier` files (4)
- `System.NullReferenceException` (1 — accidental file)
- `.refactor-mcp/metrics/` and `.refactor-mcp/tool-call-log-*` (9)

### Move to `docs/` (scattered docs):
- `WORKLOG_*.md` (2)
- `IMPLEMENTATION_SUMMARY.md`
- `TESTING_IMPLEMENTATION_SUMMARY.md`
- `PRODUCTION_CHANGES_DOCUMENTATION.md`
- `SUSPICIOUS_USERS_SETUP.md`
- `remaining_services_analysis.txt`
- `test-analysis.json`
- `cd-index-short.json`

### Keep at root:
- `README.md`, `CHANGELOG.md`
- `docker-compose.yml`, `Dockerfile`, `.dockerignore`, `.editorconfig`
- `.gitignore`, `.env.sample`, `.env copy.sample`
- `ClubDoorman.sln`, `stryker-config.json`
- `Doxyfile`

### Debatable:
- `plans/` — keep or archive?
- `docs/scenarios/` `.feature.cs` files — generated from `.feature`?
- `ClubDoorman.Baseline/` — is it a tool or part of the project?
- `test-bot.sh.example`, `dotnet-install.sh` — keep or remove?
- `.cursor/rules/` — keep or gitignore?

---

## Verification

- `git status` — clean (new branch, no uncommitted changes)
- `git ls-files | wc -l` — **692** (baseline saved)
- `dotnet test ClubDoorman.sln --no-restore --verbosity minimal` — **941 passed, 0 failed, 12 skipped**
