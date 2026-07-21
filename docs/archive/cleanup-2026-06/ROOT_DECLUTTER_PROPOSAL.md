# Root Declutter Proposal

Generated: 2026-06-25
Baseline tracked root files: **43**

---

## 1. Keep in root

| File | Reason |
|---|---|
| `README.md` | Main project entry point, must stay at root |
| `CHANGELOG.md` | Standard convention, readers expect it at root |
| `ClubDoorman.sln` | .NET solution file, required at root for `dotnet` CLI |
| `Dockerfile` | Docker build entry point, convention |
| `docker-compose.yml` | Docker compose entry point, convention |
| `docker-compose.yml.example` | Example for self-hosting users |
| `.env.sample` | Environment template for self-hosting |
| `.env copy.sample` | Environment template (weird name, but useful) |
| `.gitignore` | Git config, required at root |
| `.dockerignore` | Docker config, convention |
| `.editorconfig` | Editor config, convention |
| `.actrc` | `act` (nektos/act) config |
| `Doxyfile` | Doxygen config (used by CI) |
| `stryker-config.json` | Stryker mutation testing config |
| `.gitleaks.baseline` | Security tool baseline |
| `.gitleaks.baseline.json` | Security tool baseline |
| `.gitleaks.toml` | Security tool config |
| `Схема работы.drawio` | Architecture diagram source, discoverable |
| `Схема работы.drawio.png` | Architecture diagram PNG, discoverable |

**Count: 19 files**

---

## 2. Move to `docs/`

| Source | Proposed target | Reason | Required link update |
|---|---|---|---|
| `FOLDER_INVITE_BAN.md` | `docs/folder-invite-ban.md` | Feature documentation, belongs with other docs | Add link in README.md `## Документация` |
| `AI_PROFILE_CHECKS.md` | `docs/ai-profile-checks.md` | Feature documentation, belongs with other docs | Add link in README.md `## Документация` |
| `SUSPICIOUS_USERS_SETUP.md` | `docs/suspicious-users-setup.md` | Feature documentation, belongs with other docs | Add link in README.md `## Документация` |
| `ОБНОВЛЕННАЯ_ИНСТРУКЦИЯ_ПО_РАЗРАБОТКЕ.md` | `docs/development-instructions.ru.md` | Developer instructions, large and important doc | Add link in README.md `## Документация` |

**Count: 4 files**

---

## 3. Move to `docs/archive/`

| Source | Proposed target | Reason | Required link update |
|---|---|---|---|
| `IMPLEMENTATION_SUMMARY.md` | `docs/archive/implementation-summary.md` | Historical record of one feature implementation, superseded by current code | Link via `docs/archive/README.md` index |
| `TESTING_IMPLEMENTATION_SUMMARY.md` | `docs/archive/testing-implementation-summary.md` | Historical record of testing implementation, superseded by current code | Link via `docs/archive/README.md` index |
| `PRODUCTION_CHANGES_DOCUMENTATION.md` | `docs/archive/production-changes-documentation.md` | Diff against upstream, historical reference only | Link via `docs/archive/README.md` index |
| `WORKLOG_CHANNEL_EFFECTS_MIGRATION.md` | `docs/archive/worklog-channel-effects-migration.md` | Worklog for completed migration, historical | Link via `docs/archive/README.md` index |
| `WORKLOG_MESSAGE_PIPELINE.md` | `docs/archive/worklog-message-pipeline.md` | Worklog for completed migration, historical | Link via `docs/archive/README.md` index |
| `SUSPICIOUS_USERS_TODO.md` | `docs/archive/suspicious-users-todo.md` | TODO list for feature that is already largely implemented | Link via `docs/archive/README.md` index |
| `test-analysis.json` | `docs/archive/test-analysis.json` | 525KB test analysis output, generated artifact | Link via `docs/archive/README.md` index |
| `cd-index-short.json` | `docs/archive/cd-index-short.json` | Call graph index, generated artifact | Link via `docs/archive/README.md` index |
| `remaining_services_analysis.txt` | `docs/archive/remaining-services-analysis.txt` | Analysis of remaining services, historical | Link via `docs/archive/README.md` index |

**Count: 9 files**

---

## 4. Leave for later

| File | Why not now |
|---|---|
| `analyze-tests.py` | Python dev tool — move to `scripts/` in a separate slice |
| `analyze_trx.py` | Python dev tool — move to `scripts/` in a separate slice |
| `check_code_diff.py` | Python dev tool — move to `scripts/` in a separate slice |
| `show_full_diff.py` | Python dev tool — move to `scripts/` in a separate slice |
| `check_diff.sh` | Shell dev tool — move to `scripts/` in a separate slice |
| `test_check.sh` | Small shell script — move to `scripts/` in a separate slice |
| `test_dx_tool.py` | Python dev tool — move to `scripts/` in a separate slice |
| `dotnet-install.sh` | Official .NET installer, 63KB. Debate: keep for convenience or remove. Not urgent. |
| `test_bot.sh.example` | Example script for bot testing. Small. Could be removed or kept. |
| `run_with_real_token.sh.example` | Example script, 76 bytes. Small, low value. Could be removed. |
| `CLEANUP_INVENTORY.md` | This inventory file. After this proposal is accepted and executed, it should be updated or removed. |

**Count: 11 files**

---

## 5. Summary

| Category | Count |
|---|---|
| Keep in root | 19 |
| Move to `docs/` | 4 |
| Move to `docs/archive/` | 9 |
| Leave for later | 11 |
| **Total classified** | **43** |

After moves, root tracked files: **30** (19 keep + 11 leave for later)

---

## 6. Cross-reference check

No production code (`*.cs`) references any of the moved files.

Internal references among moved files (all within `docs/archive/` after move, so they remain valid):
- `IMPLEMENTATION_SUMMARY.md` references `FOLDER_INVITE_BAN.md` — both are moving, reference stays valid
- `WORKLOG_CHANNEL_EFFECTS_MIGRATION.md` references `WORKLOG_MESSAGE_PIPELINE.md` — both moving to same location

No README.md references any moved file by name.

---

## 7. Minimal README navigation patch after moves

Add the following block after the existing "🔗 Ссылки" section (after line ~217) and before "🙏 Благодарности" (line ~218):

```markdown
## 📚 Документация

- [Архитектура](docs/ARCHITECTURE.md)
- [Тестирование](docs/TESTING.md)
- [Инструкция по разработке](docs/development-instructions.ru.md)
- [Сценарии](docs/scenarios/)

### Функции

- [Бан за вход через папки](docs/folder-invite-ban.md)
- [AI проверки профилей](docs/ai-profile-checks.md)
- [Система подозрительных пользователей — настройка](docs/suspicious-users-setup.md)

### Архив

- [Рабочие журналы и исторические документы](docs/archive/README.md)
```

This is a minimal addition. It does not rewrite existing sections.

---

## 8. `docs/archive/README.md` index (proposed content)

Create `docs/archive/README.md` with:

```markdown
# Архив документов

Исторические рабочие журналы, реализации и анализ-артефакты.

## Рабочие журналы

- [Migration: Channel Effects](worklog-channel-effects-migration.md)
- [Migration: Message Pipeline](worklog-message-pipeline.md)

## Реализации

- [Бан за вход через папки](implementation-summary.md)
- [TDD тестирование](testing-implementation-summary.md)
- [Изменения продакшн кода](production-changes-documentation.md)

## Артефакты анализа

- [TODO: Suspicious Users](suspicious-users-todo.md)
- [Тест-анализ](test-analysis.json)
- [Call graph index](cd-index-short.json)
- [Остальные сервисы](remaining-services-analysis.txt)
```

---

## 9. Verification plan

After executing the moves:

```bash
# 1. Verify root file count decreased
git ls-files | awk -F/ 'NF==1 {print}' | wc -l
# Expected: 30 (down from 43)

# 2. Verify no production code changed
git diff --stat ClubDoorman/ ClubDoorman.Test/ ClubDoorman.Baseline/
# Expected: (empty — no changes)

# 3. Verify no broken references to old paths
git grep -n "<old moved filename>" -- '*.md' '*.cs' '*.json' '*.yml' '*.sh'
# Expected: only CLEANUP_INVENTORY.md and self-references within moved files

# 4. Verify moved files exist at new paths
ls docs/folder-invite-ban.md
ls docs/ai-profile-checks.md
ls docs/suspicious-users-setup.md
ls docs/development-instructions.ru.md
ls docs/archive/implementation-summary.md
ls docs/archive/testing-implementation-summary.md
ls docs/archive/production-changes-documentation.md
ls docs/archive/worklog-channel-effects-migration.md
ls docs/archive/worklog-message-pipeline.md
ls docs/archive/suspicious-users-todo.md
ls docs/archive/test-analysis.json
ls docs/archive/cd-index-short.json
ls docs/archive/remaining-services-analysis.txt
ls docs/archive/README.md

# 5. Verify tests still pass
dotnet test ClubDoorman.sln --no-restore --verbosity minimal
```
