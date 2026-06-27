using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Times = Moq.Times;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Notifications;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для метода SendSuspiciousMessageWithButtons в MessageHandler
/// <tags>unit, handlers, suspicious-message, admin-notifications, buttons</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("suspicious-message")]
public class MessageHandlerSendSuspiciousMessageTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;
    private NotificationService CreateNotificationService()
    {
        // Admin chat id default
        _factory.WithAppConfigSetup(c => c.Setup(x => x.AdminChatId).Returns(12345L));
        var logChatService = new Mock<ILogChatService>();
        return new NotificationService(
            new Mock<ILogger<NotificationService>>().Object,
            _factory.MessageServiceMock.Object,
            _factory.AppConfigMock.Object,
            _factory.BotMock.Object,
            logChatService.Object);
    }

    // Хелпер для создания ChatId
    private static ChatId Chat(long id) => new ChatId(id);

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _messageHandler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithValidData_SendsMessageWithButtons()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;

        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), chat.Id, message.MessageId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), ParseMode.Html, It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 12345L } });
        });

            _factory.WithMessageServiceSetup(mock =>
            {
                var appConfig = new Moq.Mock<ClubDoorman.Services.Core.Configuration.IAppConfig>();
                appConfig.SetupGet(x => x.SuspiciousToApprovedMessageCount).Returns(3);
                var templates = new MessageTemplates(appConfig.Object);
                mock.Setup(x => x.GetTemplates()).Returns(templates);
            });

        var service = CreateNotificationService();
        await service.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        _factory.BotMock.Verify(
            x => x.ForwardMessage(Chat(12345L), chat.Id, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once, "Сообщение должно быть переслано в админ-чат");
        _factory.BotMock.Verify(
            x => x.SendMessage(Chat(12345L), It.IsAny<string>(), ParseMode.Html, It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()),
            Times.Once, "Уведомление с кнопками должно быть отправлено");
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithSilentMode_AddsSilentModePrefix()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = true;

        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), chat.Id, message.MessageId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            mock.Setup(x => x.SendMessage(Chat(12345L), It.IsAny<string>(), ParseMode.Html, It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 12345L } });
        });
        _factory.WithMessageServiceSetup(mock =>
        {
            var appConfig = new Moq.Mock<ClubDoorman.Services.Core.Configuration.IAppConfig>();
            appConfig.SetupGet(x => x.SuspiciousToApprovedMessageCount).Returns(3);
            var templates = new MessageTemplates(appConfig.Object);
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });
        var service = CreateNotificationService();
        await service.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.Is<string>(text => text.Contains("🔇 **Тихий режим**")),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()),
            Times.Once, "Сообщение должно содержать префикс тихого режима");
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WhenForwardFails_SendsMessageWithoutForward()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), chat.Id, message.MessageId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("protected content"));
            mock.Setup(x => x.SendMessage(Chat(12345L), It.IsAny<string>(), ParseMode.Html, null, It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 12345L } });
        });
        _factory.WithMessageServiceSetup(mock =>
        {
            var appConfig = new Moq.Mock<ClubDoorman.Services.Core.Configuration.IAppConfig>();
            appConfig.SetupGet(x => x.SuspiciousToApprovedMessageCount).Returns(3);
            var templates = new MessageTemplates(appConfig.Object);
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });
        var service = CreateNotificationService();
        await service.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                null,
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()),
            Times.Once, "Сообщение должно быть отправлено без пересылки");
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WhenBotThrowsException_SendsFallbackMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), chat.Id, message.MessageId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));
        });
        _factory.WithMessageServiceSetup(mock =>
        {
            var appConfig = new Moq.Mock<ClubDoorman.Services.Core.Configuration.IAppConfig>();
            appConfig.SetupGet(x => x.SuspiciousToApprovedMessageCount).Returns(3);
            var templates = new MessageTemplates(appConfig.Object);
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });
        var service = CreateNotificationService();
        await service.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);
        _factory.MessageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, data, It.IsAny<CancellationToken>()),
            Times.Once, "Должно быть отправлено fallback уведомление");
    }
}
