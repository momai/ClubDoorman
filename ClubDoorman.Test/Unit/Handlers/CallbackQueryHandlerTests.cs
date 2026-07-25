using ClubDoorman.Services.Violation;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Logging; // for IModerationEventPublisher
using ClubDoorman.Services.UserBan;
using NUnit.Framework;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Moq;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.AdminOps;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:callback")]
public class CallbackQueryHandlerTests
{
    private CallbackQueryHandler _handler = null!;
    private Mock<ITelegramBotClientWrapper> _mockBot = null!;
    private Mock<ICaptchaService> _mockCaptchaService = null!;
    private Mock<IUserManager> _mockUserManager = null!;
    private Mock<IBadMessageManager> _mockBadMessageManager = null!;
    private Mock<IStatisticsService> _mockStatisticsService = null!;
    private Mock<IAiChecks> _mockAiChecks = null!;
    private Mock<IModerationService> _mockModerationService = null!;
    private Mock<IMessageService> _mockMessageService = null!;
    private Mock<ILogger<CallbackQueryHandler>> _mockLogger = null!;
    private Mock<ILogger<ViolationTracker>> _mockViolationTrackerLogger = null!;
    private Mock<IAppConfig> _mockAppConfig = null!;
    private Mock<IUserBanService> _mockUserBanService = null!;
    private Mock<ILogChatService> _mockLogChatService = null!;
    private Mock<IAdminActionStore> _mockAdminActionStore = null!;
    private Mock<ClubDoorman.Services.Logging.IGoldenMasterRecorder> _mockRecorder = null!;
    private Mock<IModerationEventPublisher> _mockEvents = null!;
    private Mock<IAdminCallbackDispatcher> _mockAdminCallbackDispatcher = null!;

    [SetUp]
    public void Setup()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockUserManager = new Mock<IUserManager>();
        _mockBadMessageManager = new Mock<IBadMessageManager>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockAiChecks = new Mock<IAiChecks>();
        _mockModerationService = new Mock<IModerationService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<CallbackQueryHandler>>();
        _mockViolationTrackerLogger = new Mock<ILogger<ViolationTracker>>();
        _mockAppConfig = new Mock<IAppConfig>();
        _mockUserBanService = new Mock<IUserBanService>();
        _mockLogChatService = new Mock<ILogChatService>();
        _mockAdminActionStore = new Mock<IAdminActionStore>();
    _mockRecorder = new Mock<ClubDoorman.Services.Logging.IGoldenMasterRecorder>();
        _mockEvents = new Mock<IModerationEventPublisher>();
        _mockAdminCallbackDispatcher = new Mock<IAdminCallbackDispatcher>();
        _mockAdminCallbackDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<CallbackQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminCallbackResult.NotHandled());

    // Default config values for tests
    const long adminChatId = 555555; // test admin chat id distinct from other test chats
    _mockAppConfig.SetupGet(x => x.AdminChatId).Returns(adminChatId);
    _mockAppConfig.SetupGet(x => x.LogAdminChatId).Returns(-20002);

        _handler = new CallbackQueryHandler(
            _mockBot.Object,
            _mockCaptchaService.Object,
            _mockStatisticsService.Object,
            _mockMessageService.Object,
            new ViolationTracker(_mockViolationTrackerLogger.Object, _mockAppConfig.Object),
            _mockUserBanService.Object,
            _mockAdminCallbackDispatcher.Object,
            _mockLogger.Object,
            _mockRecorder.Object,
            _mockEvents.Object,
            _mockAppConfig.Object
        );
    }

    [Test]
    public void CanHandle_WithCallbackQuery_ReturnsTrue()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "test_id",
                Data = "test_data"
            }
        };

        // Act
        var result = _handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_WithoutCallbackQuery_ReturnsFalse()
    {
        // Arrange
        var update = new Update
        {
            Message = new Message { Text = "test" }
        };

        // Act
        var result = _handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HandleAsync_WithEmptyCallbackData_LogsWarningAndReturns()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "test_id",
                Data = "",
                From = new User { Id = 123, FirstName = "Test", Username = "testuser" },
                Message = new Message { Chat = new Chat { Id = 123 } }
            }
        };

        // Act
        await _handler.HandleAsync(update);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Пустой callback data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_WithException_LogsErrorAndAnswersCallback()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "test_id",
                Data = "cap_123_1",
                From = new User { Id = 123, FirstName = "Test", Username = "testuser" },
                Message = new Message { Chat = new Chat { Id = 123 } }
            }
        };

        _mockCaptchaService
            .Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
            .Returns("test_key");

        _mockCaptchaService
            .Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
            .Returns(new Models.CaptchaInfo(
                ChatId: 123,
                ChatTitle: "Test Chat",
                Timestamp: DateTime.UtcNow,
                User: new User { Id = 123 },
                CorrectAnswer: 1,
                Cts: new CancellationTokenSource(),
                UserJoinedMessage: null
            ));

        _mockCaptchaService
            .Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _handler.HandleAsync(update);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ошибка при обработке callback")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockBot.Verify(
            x => x.AnswerCallbackQuery("test_id", "Произошла ошибка", It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_WithAdminChatId_CallsHandleAdminCallback()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "test_id",
                Data = "admin_action",
                From = new User { Id = 123, FirstName = "Test", Username = "testuser" },
                Message = new Message { Chat = new Chat { Id =  _mockAppConfig.Object.AdminChatId } }
            }
        };

        // Act
        await _handler.HandleAsync(update);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Обрабатываем админский callback")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_WithRegularChatId_CallsHandleCaptchaCallback()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "test_id",
                Data = "captcha_answer",
                From = new User { Id = 123, FirstName = "Test", Username = "testuser" },
                Message = new Message { Chat = new Chat { Id = 456 } }
            }
        };

        // Act
        await _handler.HandleAsync(update);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Обрабатываем капча callback")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_UnknownAdminPrefix_AnswersCallbackOnce()
    {
        _mockAdminCallbackDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<CallbackQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminCallbackResult.NotHandled());
        var update = CreateAdminUpdate("unknown_42");

        await _handler.HandleAsync(update);

        _mockBot.Verify(x => x.AnswerCallbackQuery(
            "callback-id",
            "Неизвестное действие",
            true,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_MalformedAdminCallback_AnswersCallbackOnce()
    {
        _mockAdminCallbackDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<CallbackQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminCallbackResult.Invalid());
        var update = CreateAdminUpdate("approve_not-a-user");

        await _handler.HandleAsync(update);

        _mockBot.Verify(x => x.AnswerCallbackQuery(
            "callback-id",
            "Некорректное действие",
            true,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_AdminCancellation_PropagatesWithoutAnsweringCallback()
    {
        _mockAdminCallbackDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<CallbackQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var update = CreateAdminUpdate("approve_42");

        Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(update));

        _mockBot.Verify(x => x.AnswerCallbackQuery(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private Update CreateAdminUpdate(string data)
    {
        return new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "callback-id",
                Data = data,
                From = new User { Id = 123, FirstName = "Admin" },
                Message = new Message { Chat = new Chat { Id = _mockAppConfig.Object.AdminChatId } }
            }
        };
    }

}
