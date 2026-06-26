# Тестирование в ClubDoorman

## 🚀 Быстрый старт

### Основной запуск (исключая демо-тесты)
```bash
# С .runsettings (рекомендуется)
dotnet test ClubDoorman.Test --settings ClubDoorman.Test/.runsettings --verbosity normal

# Или явно исключая демо
dotnet test ClubDoorman.Test --filter="Category!=demo&Category!=Demo" --verbosity normal
```

### Демо-тесты (примеры использования TestKit)
```bash
# Только демо-тесты
dotnet test ClubDoorman.Test --filter="Category=demo" --verbosity normal
```

### Запуск конкретного теста
```bash
dotnet test ClubDoorman.Test --filter "MessageHandlerDeletionTest_TraceProblem" --verbosity normal
```

### Запуск тестов с покрытием
```bash
dotnet test ClubDoorman.Test --collect:"XPlat Code Coverage"
```

> 💡 **Демо-тесты исключены по умолчанию** через `.runsettings`, так как они предназначены для демонстрации возможностей TestKit и могут быть нестабильными.
> 
> 📖 **Больше команд:** `scripts/test-commands.md`

## 📚 Документация

- **[Lessons Learned](LESSONS_LEARNED.md)** - Уроки и рекомендации по тестированию
- **[Testing Architecture](TESTING_ARCHITECTURE.md)** - Архитектура и паттерны тестирования

## 🔧 Ключевые компоненты

### Правило для новых тестов
Новые тесты должны идти через ближайший seam, а не через самый широкий helper:

- `MessageHandler` проверяет тонкую маршрутизацию и orchestration, не branch coverage всего pipeline.
- Pipeline steps проверяются отдельно на своих контрактах.
- Command behavior проверяется через `CommandRouter` / command handlers.
- Ban/delete/report side effects проверяются через соответствующие service seams или `FakeTelegramClient`.
- Для сценариев, где важен `MessageId`, используйте `MessageEnvelope` + `FakeTelegramClient.RegisterMessageEnvelope` + `WasMessageDeleted`.

Legacy construction paths не использовать в новых тестах без причины:

- `MessageHandlerTestFactory` — legacy helper для существующих тестов под миграцией.
- `FakeServicesFactory` — legacy broad integration helper.
- `MessageHandlerBuilder` — transitional helper.
- `TestKitAutoFixture.CreateMessageHandler` — hidden construction path.
- `MessageBuilder.WithMessageId` не задает реальный `Message.MessageId`; для id-sensitive сценариев используйте `MessageEnvelope`.

### MessageHandlerTestFactory
Legacy-фабрика для существующих тестов. Не используйте как default path для новых тестов:
```csharp
var factory = new MessageHandlerTestFactory();
var handler = factory.CreateMessageHandlerWithFake(fakeClient);
```

### FakeTelegramClient
Эмуляция Telegram Bot API:
```csharp
var fakeClient = factory.FakeTelegramClient;
fakeClient.RegisterMessageEnvelope(envelope);
var message = fakeClient.CreateMessageFromEnvelope(envelope);
```

### MessageEnvelope
Централизованное представление сообщения:
```csharp
var envelope = new MessageEnvelope
{
    MessageId = 456,
    ChatId = 123,
    UserId = 789,
    Text = "Test message"
};
```

## 📋 Чек-лист для новых тестов

### ✅ Перед написанием
- [ ] Используется централизованная фабрика
- [ ] Все моки созданы как свойства с инициализацией
- [ ] Используется MessageEnvelope для создания сообщений
- [ ] Есть интерфейс для упрощения тестирования

### ✅ При написании
- [ ] Регистрируется envelope в FakeTelegramClient
- [ ] Создается сообщение через CreateMessageFromEnvelope
- [ ] Проверяется результат через WasMessageDeleted
- [ ] Настроены все необходимые моки

### ✅ После написания
- [ ] Тест проходит локально
- [ ] Тест не влияет на другие тесты
- [ ] Код покрыт ассертами

## 🎯 Пример хорошего теста

```csharp
[Test]
public async Task MessageHandler_SpamMessage_DeletesAndSendsWarning()
{
    // Arrange
    var factory = new MessageHandlerTestFactory();
    var fakeClient = factory.FakeTelegramClient;
    var handler = factory.CreateMessageHandlerWithFake(fakeClient);
    
    var envelope = new MessageEnvelope
    {
        MessageId = 456,
        ChatId = 123,
        UserId = 789,
        Text = "SPAM MESSAGE"
    };
    
    fakeClient.RegisterMessageEnvelope(envelope);
    var message = fakeClient.CreateMessageFromEnvelope(envelope);
    
    // Настройка моков
    factory.ModerationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
        .ReturnsAsync(new ModerationResult { Action = ModerationAction.Delete });
    
    // Act
    await handler.HandleAsync(message);
    
    // Assert
    Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
}
```

## 🚨 Частые ошибки

### ❌ Моки создаются заново каждый раз
```csharp
// Плохо
public Mock<IService> ServiceMock => new Mock<IService>();

// Хорошо
public Mock<IService> ServiceMock { get; } = new();
```

### ❌ Смешивание реальных сервисов и моков
```csharp
// Плохо: в фабрике реальный сервис, в тесте проверяется мок
return new MessageHandler(..., realService, ...);
MessageServiceMock.Verify(...); // Не сработает!

// Хорошо: везде используется мок
return new MessageHandler(..., MessageServiceMock.Object, ...);
MessageServiceMock.Verify(...); // Работает!
```

## 📊 Метрики

### Статистика тестов
```bash
# Количество тестов
dotnet test --list-tests

# Время выполнения
dotnet test --logger "console;verbosity=detailed"

# Покрытие кода
dotnet test --collect:"XPlat Code Coverage"
```

## 🔍 Отладка

### Добавление логов
```csharp
Console.WriteLine($"DEBUG: Processing message {envelope.MessageId}");
Console.WriteLine($"DEBUG: Result: {result}");
```

### Проверка моков
```csharp
MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
    It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
    It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
```

## 📞 Поддержка

При возникновении проблем с тестами:

1. Проверьте документацию в `LESSONS_LEARNED.md`
2. Изучите примеры в `TESTING_ARCHITECTURE.md`
3. Добавьте отладочные логи для понимания проблемы
4. Убедитесь, что все моки настроены правильно

---

**Помните:** Хороший тест - это не только проверка функциональности, но и документирование поведения системы.
