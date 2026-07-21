# Developer Onboarding (Spampyre / ClubDoorman Fork)

Цель: быстро поднять локальную среду разработки, следовать стандартам форматирования и не ломать пайплайны.

## 1. Предпосылки
- Linux/macOS (Windows WSL2 ок)
- .NET 9 SDK (обязательно)
- Git, Bash
- (опц.) Docker
- (опц.) gitleaks для проверки секретов

## 2. Установка .NET 9
```bash
./scripts/dotnet-install.sh --channel 9.0 --version latest
export PATH="$HOME/.dotnet:$PATH"
```
Проверьте: `dotnet --version` → 9.x

## 3. Клонирование и restore
```bash
git clone git@github.com:momai/ClubDoorman.git spampyre
cd spampyre
export PATH="$HOME/.dotnet:$PATH"
dotnet restore
```

## 4. Быстрый build + тесты
```bash
dotnet build --configuration Release --no-restore
dotnet test ClubDoorman.Test --filter "Category!=real-api&Category!=BDD&Category!=disabled&Category!=demo" --no-restore
```

## 5. Переменные окружения (минимум)
Создайте `.env` или export:
```
DOORMAN_BOT_API=https://api.telegram.org
DOORMAN_ADMIN_CHAT=123456789
```
(это безопасные значения для тестов — бот реально не запустится)

## 6. Git hooks (форматирование + тесты)
Хуки лежат в `.githooks/`:
- `pre-commit`: автоформат временно отключен. Включить для конкретного коммита: `PRECOMMIT_FORMAT=1 git commit -m "msg"`
- `pre-push`: быстрые тесты (фильтр категорий)

Включить:
```bash
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit .githooks/pre-push
```
Пропустить (не рекомендуется): `--no-verify`

## 7. Стиль и форматирование
Используем стандартный `dotnet format`. Сейчас он НЕ запускается автоматически в pre-commit — гоняйте вручную перед крупным PR или периодически:
```bash
dotnet format
```

## 8. Архитектура (кратко)
- Worker Service (.NET) – точка входа `Program.cs`, `Worker.cs`
- Основные подсистемы: Moderation, Captcha, UserJoin, UserBan, Effects pipeline
- Channel moderation: все side-effects через эффект билдер + `IEffectBus`

## 9. Что не ломать
- Не менять тексты русских логов модерации без причины
- Не включать real-api тесты в обычном цикле (`Category=real-api`)
- Не добавлять секреты в репозиторий

## 10. Типовой цикл
```bash
# 1. Обновиться
git pull --rebase
# 2. Изменения
<edit>
# 3. Формат (ручной)
dotnet format
# 4. Тесты
./scripts/run_tests_without_demos.sh
# 5. Commit (hook проверит формат)
# 6. Push (hook прогонит быстрые тесты)
```

## 11. Troubleshooting
| Проблема | Решение |
|----------|---------|
| NETSDK1045 / не видит .NET 9 | Установить SDK через `scripts/dotnet-install.sh` и PATH |
| Долгий restore | Первый раз до ~70s — норма |
| Падают real-api тесты | Исключить фильтром или задать валидные ключи |
| Форматирование валится в pre-commit | (Если включили PRECOMMIT_FORMAT=1) Запустите `dotnet format`, добавьте изменения |

## 12. Создание веток
Всегда от `next` или указанной интеграционной ветки (например, `next-lab`).
```bash
git checkout next
git pull --rebase
git checkout -b feature/<short-name>
```

## 13. PR чеклист (локально)
- [ ] Build без ошибок
- [ ] Быстрые тесты зелёные
- [ ] Нет случайных изменений строк логов
- [ ] Нет лишних файлов / отладочных артефактов
- [ ] README не трогали без причины

## 14. Effects: расширение
Если добавляете новые side-effects — держите их ближе к соответствующему сервису; не смешивайте решение (decision) и исполнение (effect).

## 15. Контакты
Вопросы: @momai (Telegram)

---
Минимум онбординга — максимум пользы. Удачной охоты на спам.

### Дополнительно: локальный запуск golden workflow
Для проверки golden baseline перед пушем можно локально прогнать GitHub Actions через `act`:
```bash
scripts/run_act_golden.sh
```
Требуется установленный `act`.

### Миграционная заметка (Golden Master hardening Q3 2025)
С недавних PR (#137/#138/#139):
- Удалены временные `SemanticsOverrides` — теперь все покрытые ранние ветки сами эмитят semantics.
- Хэндлеры больше не создаются через устаревшие конструкторы: требуется явная передача `IGoldenMasterRecorder` (используйте `NullGoldenMasterRecorder` в тестах только если осознанно не хотите запись).
- Добавлены ранние RuleCode (Phase1): `PrivateSkip`, `NewMembers`, `LeftMemberCleanup`, `ChannelMessage`.
- Добавлены системные/fast-path RuleCode (Phase2): `SystemNoUser`, `BotMessage`, `CaptchaPending`, `AlreadyApproved`, `ClubMemberSkip`, `AiProfileRestricted`, `ModeratedGeneric`.
При добавлении новой логики: сначала тест (semantics/golden), затем код, затем регенерация baseline.
