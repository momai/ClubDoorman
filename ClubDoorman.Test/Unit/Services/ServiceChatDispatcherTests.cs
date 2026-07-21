using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("services")]
[Category("dispatcher")]
public class ServiceChatDispatcherTests
{
    private ServiceChatDispatcherTestFactory _factory;
    private ServiceChatDispatcher _dispatcher;

    [SetUp]
    public void Setup()
    {
        _factory = new ServiceChatDispatcherTestFactory();
        _dispatcher = _factory.CreateServiceChatDispatcher();
    }

    [Test]
    [Category("admin-chat")]
    public async Task SendToAdminChatAsync_ValidNotification_SendsMessage()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToAdminChatAsync(notification);

        // Assert
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("log-chat")]
    public async Task SendToLogChatAsync_ValidNotification_SendsMessage()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToLogChatAsync(notification);

        // Assert
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SuspiciousMessageData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SuspiciousUserData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateSuspiciousUserData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_AutoBanData_ReturnsFalse()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateAutoBanData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_ErrorData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateErrorData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SimpleNotificationData_ReturnsFalse()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("error-handling")]
    public async Task SendToAdminChatAsync_BotClientThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Bot API error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.SendToAdminChatAsync(notification));

        Assert.That(exception.Message, Is.EqualTo("Bot API error"));

        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("error-handling")]
    public async Task SendToLogChatAsync_BotClientThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Bot API error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.SendToLogChatAsync(notification));

        Assert.That(exception.Message, Is.EqualTo("Bot API error"));

        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("edge-cases")]
    public void ShouldSendToAdminChat_NullNotification_ReturnsFalse()
    {
        // Act
        var result = _dispatcher.ShouldSendToAdminChat(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullBotClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServiceChatDispatcher(null!, _factory.LoggerMock.Object, _factory.AppConfigMock.Object));

        Assert.That(exception.ParamName, Is.EqualTo("bot"));
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServiceChatDispatcher(_factory.BotClientMock.Object, null!, _factory.AppConfigMock.Object));

        Assert.That(exception.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_AiDetectData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateAiDetectData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("integration")]
    public async Task SendToAdminChatAsync_AllNotificationTypes_HandledCorrectly()
    {
        // Arrange
        var notifications = new NotificationData[]
        {
            ServiceChatDispatcherTestFactory.CreateTestNotificationData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousUserData(),
            ServiceChatDispatcherTestFactory.CreateAiProfileAnalysisData(),
            ServiceChatDispatcherTestFactory.CreateAiDetectData(),
            ServiceChatDispatcherTestFactory.CreateAutoBanData(),
            ServiceChatDispatcherTestFactory.CreateErrorData()
        };

        // Act & Assert
        foreach (var notification in notifications)
        {
            await _dispatcher.SendToAdminChatAsync(notification);
        }

        // Проверяем, что все уведомления были обработаны
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Exactly(notifications.Length));
    }

    [Test]
    [Category("integration")]
    public async Task SendToLogChatAsync_AllNotificationTypes_HandledCorrectly()
    {
        // Arrange
        var notifications = new NotificationData[]
        {
            ServiceChatDispatcherTestFactory.CreateTestNotificationData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousUserData(),
            ServiceChatDispatcherTestFactory.CreateAiProfileAnalysisData(),
            ServiceChatDispatcherTestFactory.CreateAiDetectData(),
            ServiceChatDispatcherTestFactory.CreateAutoBanData(),
            ServiceChatDispatcherTestFactory.CreateErrorData()
        };

        // Act & Assert
        foreach (var notification in notifications)
        {
            await _dispatcher.SendToLogChatAsync(notification);
        }

        // Проверяем, что все уведомления были обработаны
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Exactly(notifications.Length));
    }
}