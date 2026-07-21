# Root Final Cleanup Proposal — Slice 4

Generated: 2026-06-25

## 1. Move to `scripts/`

| Source | Target | Reason |
|---|---|---|
| `analyze-tests.py` | `scripts/analyze-tests.py` | Python dev tool, TRX test result analyzer (variant 1) |
| `analyze_trx.py` | `scripts/analyze_trx.py` | Python dev tool, TRX test result analyzer (variant 2, LLM-focused) |
| `check_code_diff.py` | `scripts/check_code_diff.py` | Python dev tool, method content diff utility |
| `show_full_diff.py` | `scripts/show_full_diff.py` | Python dev tool, full method extraction + diff |
| `check_diff.sh` | `scripts/check_diff.sh` | Shell dev tool, one-off method comparison |
| `test_check.sh` | `scripts/test_check.sh` | Shell dev tool, build + test runner |
| `test_dx_tool.py` | `scripts/test_dx_tool.py` | Python test for `scripts/test_factory_generator` DX Tool |
| `dotnet-install.sh` | `scripts/dotnet-install.sh` | Official .NET installer, 1888 lines |

**Count: 8 files**

### Required reference updates:
- `.github/copilot-instructions.md` — change `dotnet-install.sh` → `scripts/dotnet-install.sh` (lines 30, 31, 180)
- `.github/copilot-instructions.md` — change `check_diff.sh` → `scripts/check_diff.sh` (line 212)
- `docs/DEVELOPER_ONBOARDING.md` — change `dotnet-install.sh` → `scripts/dotnet-install.sh` (lines 14, 86)
- `docs/archive/implementation-summary.md` — change `./dotnet-install.sh` → `../scripts/dotnet-install.sh` (line 148)

## 2. Keep in root

None. All remaining root files are either cleanup artifacts or dev tools.

## 3. Move to `docs/archive/cleanup-2026-06/`

| Source | Target | Reason |
|---|---|---|
| `CLEANUP_INVENTORY.md` | `docs/archive/cleanup-2026-06/CLEANUP_INVENTORY.md` | This inventory — historical record of cleanup process |
| `ROOT_DECLUTTER_PROPOSAL.md` | `docs/archive/cleanup-2026-06/ROOT_DECLUTTER_PROPOSAL.md` | This proposal — historical record of cleanup process |

**Count: 2 files**

## 4. Delete

| File | Reason |
|---|---|
| `test_bot.sh.example` | Low-value example script (20 lines), historical artifact from GitLeaks fix. Already documented in `docs/GITLEAKS_FIX.md`. |
| `run_with_real_token.sh.example` | 2 lines, placeholder-only. Historical artifact from GitLeaks fix. |

**Count: 2 files**

### Required baseline update:
- `.gitleaks.baseline.json` — after deletion, run `gitleaks detect --source . --config .gitleaks.toml --report-format json --report-path .gitleaks.baseline.json` to remove stale fingerprints referencing these files.

## 5. Needs human decision

None. All items classified.

## 6. Verification commands

```bash
# 1. Verify root file count decreased
git ls-files | awk -F/ 'NF==1 {print}' | wc -l
# Expected: 20 (down from 30 after slices 1-3: 19 keep + 11 leave-for-later → 19 keep + 3 scripts moved)

# 2. Verify no production code changed
git diff --stat ClubDoorman/ ClubDoorman.Test/ ClubDoorman.Baseline/
# Expected: (empty — no changes)

# 3. Verify moved files exist at new paths
ls scripts/analyze-tests.py
ls scripts/analyze_trx.py
ls scripts/check_code_diff.py
ls scripts/show_full_diff.py
ls scripts/check_diff.sh
ls scripts/test_check.sh
ls scripts/test_dx_tool.py
ls scripts/dotnet-install.sh

# 4. Verify archive files exist
ls docs/archive/cleanup-2026-06/CLEANUP_INVENTORY.md
ls docs/archive/cleanup-2026-06/ROOT_DECLUTTER_PROPOSAL.md

# 5. Verify deleted files are gone
test -f test_bot.sh.example && echo "FAIL" || echo "OK"
test -f run_with_real_token.sh.example && echo "FAIL" || echo "OK"

# 6. Verify reference updates
git grep -n "scripts/dotnet-install.sh" -- .github/copilot-instructions.md docs/DEVELOPER_ONBOARDING.md
git grep -n "scripts/check_diff.sh" -- .github/copilot-instructions.md

# 7. Verify no broken references to old paths
git grep -n "\./dotnet-install\.sh\|check_diff\.sh" -- '*.md' '*.sh' '*.json'
# Expected: only in moved/copied files (docs/archive/...) and docs/GITLEAKS_FIX.md (historical)

# 8. Verify tests still pass
dotnet test ClubDoorman.sln --no-restore --verbosity minimal

# 9. Update gitleaks baseline
gitleaks detect --source . --config .gitleaks.toml --report-format json --report-path .gitleaks.baseline.json
```

## 7. Files that will NOT change

- `graphify-out/` — untouched
- `ClubDoorman/.understand-anything/` — untouched
- `ClubDoorman/`, `ClubDoorman.Test/`, `ClubDoorman.Baseline/` — no production code changes
- `docs/` — only archive directory created, existing docs untouched except reference updates
- `plans/` — untouched
- `.github/` — only `copilot-instructions.md` reference updates

## 8. Summary

| Category | Count |
|---|---|
| Move to `scripts/` | 8 |
| Keep in root | 0 |
| Move to `docs/archive/cleanup-2026-06/` | 2 |
| Delete | 2 |
| **Total classified** | **12** |

After moves/deletions, root tracked files: **20** (down from 30 after slices 1-3).
