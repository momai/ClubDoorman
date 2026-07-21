# 🎯 Обновленная инструкция по разработке ClubDoorman

## Что это за бот?
Это антиспам-бот для Telegram - как охранник на входе в клуб. Он проверяет всех новых участников и их сообщения, чтобы отсеять спамеров.

## 🏗️ Как устроен код (актуальная архитектура)

### 1. Главные "отделы" бота:
- **Worker.cs** - главный "менеджер" бота, который запускает все процессы
- **Services/Handlers/MessageHandler.cs** - главный "рецепционист", который встречает все сообщения (1503 строки - в процессе рефакторинга!)
- **Services/** - разные "специалисты" (проверка спама, капча, баны и т.д.)
- **Services/Commands/** - обработчики команд (как /spam, /ham, /start)
- **Models/** - "формы" для данных
- **Infrastructure/** - техническая "инфраструктура"

### 2. Как работает рефакторинг:
Твой друг разносит большой код по маленьким файлам - это как разбирать большой шкаф на полочки. Сейчас MessageHandler.cs весит 1503 строки - это слишком много!

### 3. Новая архитектура с модулями:
Каждый тип сервисов теперь имеет свой модуль регистрации:
- `ConfigurationModule` - конфигурация
- `CommandsModule` - команды
- `MessagingModule` - сообщения и уведомления
- `AIModule` - AI сервисы
- `UserManagementModule` - управление пользователями
- `ModerationModule` - модерация
- `CaptchaModule` - капча
- `StatisticsModule` - статистика
- `TelegramModule` - Telegram API
- `HandlersModule` - обработчики
- И другие...

## 🚀 Как добавлять новые функции БЕЗ мусора

### Правило №1: Следуй модульной архитектуре
Вместо того чтобы писать код прямо в MessageHandler.cs, создавай новые сервисы в соответствующих модулях:

```csharp
// ❌ ПЛОХО - добавлять в MessageHandler
public async Task HandleNewFeature(Message message) {
    // 100 строк кода прямо здесь
}

// ✅ ХОРОШО - создать отдельный сервис
public interface INewFeatureService {
    Task ProcessFeature(Message message);
}

public class NewFeatureService : INewFeatureService {
    // Вся логика здесь
}
```

### Правило №2: Используй интерфейсы
Всегда создавай интерфейс перед классом:

```csharp
// Сначала интерфейс
public interface IMyNewService {
    Task DoSomething();
}

// Потом реализация
public class MyNewService : IMyNewService {
    public async Task DoSomething() {
        // код
    }
}
```

### Правило №3: Создавай модуль регистрации
Новые сервисы нужно "записать в штат" через модуль:

```csharp
// Создай новый модуль или добавь в существующий
public static class MyNewModule
{
    public static IServiceCollection AddMyNewServices(this IServiceCollection services)
    {
        services.AddScoped<IMyNewService, MyNewService>();
        return services;
    }
}
```

### Правило №4: Регистрируй в Program.cs
Добавь вызов модуля в Program.cs:

```csharp
// В Program.cs, в ConfigureServices
services.AddMyNewServices();
```

### Правило №5: Используй через ServiceProvider
В MessageHandler.cs используй ServiceProvider для получения сервисов:

```csharp
public class MessageHandler : IUpdateHandler, IMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    
    public MessageHandler(IServiceProvider serviceProvider, /* другие зависимости */)
    {
        _serviceProvider = serviceProvider;
    }
    
    private async Task HandleNewFeature(Message message)
    {
        var newFeatureService = _serviceProvider.GetRequiredService<IMyNewService>();
        await newFeatureService.DoSomething();
    }
}
```

## 📁 Где что размещать

### Новые проверки сообщений:
```
Services/
├── MyNewFilter/
│   ├── IMyNewFilter.cs
│   ├── MyNewFilter.cs
│   └── MyNewModule.cs
```

### Новые команды:
```
Services/Commands/
├── MyNewCommandHandler.cs
├── IMyNewCommandHandler.cs
└── Добавить в CommandsModule.cs
```

### Новые модели данных:
```
Models/
├── MyNewModel.cs
```

### Уведомления:
```
Services/Messaging/
├── MyNewNotificationService.cs
└── Добавить в MessagingModule.cs
```

### AI сервисы:
```
Services/AI/
├── IMyNewAIService.cs
├── MyNewAIService.cs
└── Добавить в AIModule.cs
```

### Управление пользователями:
```
Services/UserManagement/
├── IMyNewUserService.cs
├── MyNewUserService.cs
└── Добавить в UserManagementModule.cs
```

## 🎉 Пример правильного добавления функции

Допустим, мы хотим добавить проверку длины сообщений:

### 1. Создаем интерфейс:
```csharp
// Services/TextProcessing/IMessageLengthService.cs
public interface IMessageLengthService
{
    Task<bool> IsMessageTooLong(Message message);
}
```

### 2. Создаем реализацию:
```csharp
// Services/TextProcessing/MessageLengthService.cs
public class MessageLengthService : IMessageLengthService
{
    public async Task<bool> IsMessageTooLong(Message message)
    {
        // Логика проверки
        return message.Text?.Length > 1000;
    }
}
```

### 3. Добавляем в модуль:
```csharp
// Services/TextProcessing/TextProcessingModule.cs
public static IServiceCollection AddTextProcessingServices(this IServiceCollection services)
{
    services.AddScoped<IMessageLengthService, MessageLengthService>();
    return services;
}
```

### 4. Используем в MessageHandler:
```csharp
// В MessageHandler.cs
private async Task HandleUserMessageAsync(Message message, bool isSilentMode, CancellationToken cancellationToken)
{
    var messageLengthService = _serviceProvider.GetRequiredService<IMessageLengthService>();
    
    if (await messageLengthService.IsMessageTooLong(message))
    {
        await DeleteAndReportMessage(message, "Сообщение слишком длинное", isSilentMode, cancellationToken);
        return;
    }
    
    // Остальная логика...
}
```

## 📚 Основные принципы для тебя:

1. **Всегда создавай интерфейс** - это основа архитектуры
2. **Используй модули** - группируй связанные сервисы
3. **Регистрируй в Program.cs** - через вызов модуля
4. **Используй ServiceProvider** - не засоряй конструктор MessageHandler
5. **Размещай файлы правильно** - следуй структуре папок

## 🚫 Чего НЕ делать:

❌ **Не пиши код прямо в MessageHandler.cs** - он уже слишком большой
❌ **Не добавляй параметры в конструктор MessageHandler** - он уже слишком длинный
❌ **Не создавай статические методы** - используй DI
❌ **Не забывай про интерфейсы** - это основа архитектуры
❌ **Не регистрируй сервисы напрямую в Program.cs** - используй модули

## 🔄 Как продолжать рефакторинг:

Твой друг правильно делает - он разносит большой код по маленьким файлам. Когда будешь добавлять новые функции:

1. **Следуй его примеру** - создавай отдельные сервисы
2. **Не трогай большие файлы** - добавляй только вызовы
3. **Используй интерфейсы** - это основа архитектуры
4. **Тестируй отдельно** - каждый сервис можно тестировать независимо
5. **Группируй в модули** - используй модульную архитектуру

## 🏗️ Текущая структура модулей:

- **ConfigurationModule** - `IAppConfig`, `AppConfig`
- **CommandsModule** - `ICommandProcessingService`, `StartCommandHandler`, `SuspiciousCommandHandler`
- **MessagingModule** - `IMessageService`, `INotificationService`, `IChatLinkFormatter`
- **AIModule** - `IAiChecks`, `ISpamHamClassifier`, `IMimicryClassifier`
- **UserManagementModule** - `IUserManager`, `IUserCleanupService`, `IUserIndex` (ранее упоминался устаревший IUserJoinService)
- **ModerationModule** - `IModerationService`
- **CaptchaModule** - `ICaptchaService`
- **StatisticsModule** - `IStatisticsService`, `GlobalStatsManager`
- **TelegramModule** - `ITelegramBotClientWrapper`
- **HandlersModule** - `IUpdateHandler`, `IMessageHandler`, `IUpdateDispatcher`

## 🎯 Итог:

Теперь ты знаешь, как добавлять новые функции правильно в соответствии с текущей архитектурой! 

**Ключевые изменения от старой инструкции:**
- Используй **модульную архитектуру** вместо прямых регистраций
- **ServiceProvider** вместо конструктора для новых зависимостей
- **Группировка сервисов** по функциональности в модули
- **Четкая структура папок** для каждого типа сервисов

Следуй этим принципам, и твой код будет чистым, тестируемым и легко поддерживаемым! 🚀
