using ClubDoorman.Services.UserBan;
using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Times = Moq.Times;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.AdminOps;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для метода HandleSayCommandAsync в MessageHandler
/// <tags>unit, handlers, say-command, admin-commands, messaging</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("say-command")]
public class MessageHandlerHandleSayCommandTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();

        // Настраиваем AppConfig для разрешения команды /say из тестового чата
        _factory.AppConfigMock.Setup(x => x.AdminChatId).Returns(-1001234567890); // ID тестового чата
        _factory.AppConfigMock.Setup(x => x.LogAdminChatId).Returns(-1001234567890);

        // Настраиваем CommandRouter для обработки /say команд через SayCommandHandler
        var sayCommandHandler = new SayCommandHandler(
            _factory.BotMock.Object,
            _factory.MessageServiceMock.Object,
            _factory.AppConfigMock.Object,
            NullLogger<SayCommandHandler>.Instance
        );

        _factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.Is<Message>(m => m.Text != null && m.Text.StartsWith("/say")),
            It.IsAny<CancellationToken>()))
            .Returns<Message, CancellationToken>(async (message, ct) =>
            {
                await sayCommandHandler.HandleAsync(message, ct);
                return true; // Возвращаем true, что команда обработана
            });

        // Добавляем отдельный setup для null или не-/say сообщений
        _factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.Is<Message>(m => m.Text == null || !m.Text.StartsWith("/say")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Команда не обработана

        _messageHandler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInsufficientParts_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say @username"; // Недостаточно частей

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Формат: /say")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о неправильном формате"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithUsername_AttemptsToFindUserAndSendMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!"; // используем userId вместо username

        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Success,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Сообщение отправлено")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено уведомление об успешной отправке"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithUserId_AttemptsToSendMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!";

        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(It.Is<ChatId>(c => c.Identifier == 12345L), "Привет!", ParseMode.Markdown, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено сообщение пользователю"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInvalidUsername_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say @nonexistentuser Привет!";

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось найти пользователя")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о ненайденном пользователе"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInvalidUserId_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say invalid_id Привет!";

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось найти пользователя")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о ненайденном пользователе"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WhenBotThrowsException_SendsErrorMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!";
        var expectedException = new InvalidOperationException("Test exception");

        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);
        });

        // Act
        await _messageHandler.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось доставить сообщение")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение об ошибке доставки"
        );
    }

 }