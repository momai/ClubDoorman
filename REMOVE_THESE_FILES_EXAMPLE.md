# 🗑️ Файлы для удаления при упрощении архитектуры

## Фасады (лишние слои абстракции)

### Features/Moderation/
- `01-ModerationFeature.cs` - 23 строки, только регистрация DI
- `02-ModerationFacade.cs` - 121 строка, только прокси-вызовы

### Features/UserJoin/
- `01-UserJoinFeature.cs` - 23 строки, только регистрация DI  
- `02-UserJoinFacade.cs` - 62 строки, только прокси-вызовы

## Что заменяет эти файлы

### Вместо фасадов используем прямые сервисы:
```csharp
// Было:
services.AddModerationFeature(); // регистрирует IModerationFacade
services.AddUserJoinFeature();   // регистрирует IUserJoinFacade

// Стало:
services.AddSingleton<IModerationPolicy, ModerationPolicy>();
services.AddSingleton<IUserJoinPolicy, UserJoinPolicy>();
```

### В MessageHandler:
```csharp
// Было:
private readonly IModerationFacade _moderationFacade;
private readonly IUserJoinFacade _userJoinFacade;

// Стало:
private readonly IModerationPolicy _moderationPolicy;
private readonly IUserJoinPolicy _userJoinPolicy;
```

## Результат упрощения

### Убрали:
- ❌ 4 файла фасадов (229 строк кода)
- ❌ Лишние слои абстракции
- ❌ Прокси-вызовы без ценности

### Получили:
- ✅ Прямые вызовы сервисов
- ✅ Меньше файлов и кода
- ✅ Простую архитектуру

## Команды для удаления

```bash
# Удаляем фасады
rm ClubDoorman/Features/Moderation/01-ModerationFeature.cs
rm ClubDoorman/Features/Moderation/02-ModerationFacade.cs
rm ClubDoorman/Features/UserJoin/01-UserJoinFeature.cs
rm ClubDoorman/Features/UserJoin/02-UserJoinFacade.cs

# Удаляем пустые папки Features если они станут пустыми
rmdir ClubDoorman/Features/Moderation
rmdir ClubDoorman/Features/UserJoin
rmdir ClubDoorman/Features
```

## Альтернативный подход

Если папка Features содержит другие важные файлы, можно оставить только нужные:

### Features/Moderation/ (оставить только)
- `03-Policies.cs` - 900 строк, реальная логика
- `04-Services.cs` - 80 строк, вспомогательные сервисы
- `05-Contracts.cs` - 243 строки, интерфейсы

### Features/UserJoin/ (оставить только)
- `03-Policies.cs` - 172 строки, реальная логика
- `04-Services.cs` - если есть
- `05-Contracts.cs` - если есть
