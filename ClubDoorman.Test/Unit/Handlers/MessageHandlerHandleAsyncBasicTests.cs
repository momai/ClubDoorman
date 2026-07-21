using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Базовые тесты для метода HandleAsync в MessageHandler
/// <tags>unit, handlers, handle-async, golden-master</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("handle-async")]
[Category("golden-master")]
public class MessageHandlerHandleAsyncBasicTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();

        // Настройка базовых моков для предотвращения NullReferenceException
        _factory.WithModerationFacadeMock(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test"));
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test name"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });

        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(false); // Пользователь НЕ одобрен для запуска AI анализа
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
        });

        // Настройка AiCascadeService mock для AI анализа
        _factory.AiCascadeServiceMock.Setup(x => x.PerformAiProfileAnalysisAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // false означает, что профиль не подозрительный

        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Тест для HandleAsync с валидным текстовым сообщением
    /// Проверяет, что метод обрабатывает обычное текстовое сообщение и вызывает модерацию
    /// <tags>golden-master, production, handle-async, valid-message, moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidTextMessage_CallsModerationAndLogs()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var update = new Update { Message = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем вызовы модерации - используем конкретный объект
        _factory.ModerationFacadeMock.Verify(
            x => x.CheckMessageAsync(message),
            Times.Once,
            "Должна вызваться проверка сообщения");

        // CheckUserNameAsync вызывается только для новых участников, не для обычных сообщений
        _factory.ModerationFacadeMock.Verify(
            x => x.CheckUserNameAsync(It.IsAny<User>()),
            Times.Never,
            "CheckUserNameAsync не должен вызываться для обычных сообщений");

        // Проверяем логирование - используем реальные сообщения из логов
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageHandler получил сообщение")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться получение сообщения");
    }

    /// <summary>
    /// Тест для HandleAsync с отредактированным сообщением
    /// Проверяет, что метод обрабатывает отредактированное сообщение
    /// <tags>golden-master, production, handle-async, edited-message</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_EditedMessage_CallsModerationAndLogs()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем вызовы модерации - используем конкретный объект
        _factory.ModerationFacadeMock.Verify(
            x => x.CheckMessageAsync(message),
            Times.Once,
            "Должна вызваться проверка отредактированного сообщения");

        _factory.ModerationFacadeMock.Verify(
            x => x.CheckUserNameAsync(It.IsAny<User>()),
            Times.Never,
            "CheckUserNameAsync не должен вызываться для обычных сообщений");

        // Проверяем логирование - используем реальные сообщения из логов
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageHandler получил сообщение")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться получение отредактированного сообщения");
    }

    /// <summary>
    /// Тест для HandleAsync с конкретными проверками для убийства мутантов
    /// Проверяет конкретные значения и состояния объектов
    /// <tags>golden-master, production, handle-async, mutation-killing</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidMessage_VerifiesSpecificCalls()
    {
        // Arrange: Создаем конкретные объекты для точных проверок
        var user = new User { Id = 12345, FirstName = "Test", Username = "testuser" };
        var chat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup, Title = "Test Chat" };
        var message = new Message
        {
            Date = DateTime.UtcNow,
            From = user,
            Chat = chat,
            Text = "Hello world"
        };
        var update = new Update { Message = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем конкретные вызовы с точными параметрами
        _factory.ModerationFacadeMock.Verify(
            x => x.CheckMessageAsync(It.Is<Message>(m =>
                m.Text == "Hello world" &&
                m.From!.Id == 12345)),
            Times.Once,
            "Должна вызваться проверка сообщения с точными параметрами");

        // Проверяем, что пользователь проверяется по блэклисту
        _factory.UserManagerMock.Verify(
            x => x.InBanlist(12345),
            Times.AtLeastOnce(),
            "Проверка пользователя по блэклисту должна вызываться (pipeline+legacy могут давать 2 вызова на этапе миграции)");

        // Проверяем, что AI анализ запускается через AiCascadeService
        _factory.AiCascadeServiceMock.Verify(
            x => x.PerformAiProfileAnalysisAsync(It.Is<Message>(m => m.Text == "Hello world" && m.From!.Id == 12345), It.Is<User>(u => u.Id == 12345), It.Is<Chat>(c => c.Id == -1001234567890), It.IsAny<CancellationToken>()),
            Times.Once,
            "Должен запуститься AI анализ профиля через AiCascadeService");
    }
}