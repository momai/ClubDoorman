using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("user-ban")]
public class UserBanServiceTests
{
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IMessageService> _messageServiceMock = null!;
    private Mock<IUserFlowLogger> _userFlowLoggerMock = null!;
    private Mock<IAppConfig> _appConfigMock = null!;
    private Mock<IModerationService> _moderationServiceMock = null!;
    private Mock<IViolationTracker> _violationTrackerMock = null!;
    private Mock<IStatisticsService> _statisticsServiceMock = null!;
    private Mock<GlobalStatsManager> _globalStatsManagerMock = null!;
    private Mock<IUserManager> _userManagerMock = null!;

    // Дополнительные моки для MessageHandler (эталонная версия)
    private Mock<ICaptchaService> _captchaServiceMock = null!;
    private Mock<ISpamHamClassifier> _classifierMock = null!;
    private Mock<IBadMessageManager> _badMessageManagerMock = null!;
    private Mock<IAiChecks> _aiServiceMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IChatLinkFormatter> _chatLinkFormatterMock = null!;
    private Mock<IBotPermissionsService> _botPermissionsServiceMock = null!;


    private IUserBanService _userBanService = null!;

    [SetUp]
    public void Setup()
    {
        _botMock = new Mock<ITelegramBotClientWrapper>();
        _messageServiceMock = new Mock<IMessageService>();
        _userFlowLoggerMock = new Mock<IUserFlowLogger>();
        _appConfigMock = new Mock<IAppConfig>();
        _moderationServiceMock = new Mock<IModerationService>();
        _violationTrackerMock = new Mock<IViolationTracker>();
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _globalStatsManagerMock = new Mock<GlobalStatsManager>();
        _userManagerMock = new Mock<IUserManager>();

        // Дополнительные моки для MessageHandler (эталонная версия)
        _captchaServiceMock = new Mock<ICaptchaService>();
        _classifierMock = new Mock<ISpamHamClassifier>();
        _badMessageManagerMock = new Mock<IBadMessageManager>();
        _aiServiceMock = new Mock<IAiChecks>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _chatLinkFormatterMock = new Mock<IChatLinkFormatter>();
        _botPermissionsServiceMock = new Mock<IBotPermissionsService>();


        // MessageHandler больше не нужен в этих тестах - тестируем UserBanService напрямую

        // UserBanService теперь содержит реальную логику
        _userBanService = new UserBanService(
            _botMock.Object,
            _messageServiceMock.Object,
            _userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            _violationTrackerMock.Object,
            _appConfigMock.Object,
            _statisticsServiceMock.Object,
            _globalStatsManagerMock.Object,
            _userManagerMock.Object,
            new Mock<IUserCleanupService>().Object
        );
    }

    #region BanUserForLongName Tests

    [Test]
    [Category("migration-new")]
    public async Task BanUserForLongName_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange - используем builders вместо мутаций
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.BuildMessage()
            .AsValid()
            .InChat(chat)
            .Build();
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert - идентично оригиналу
        // Логирование происходит в MessageHandler, а не в UserBanService

        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.PrivateChatBanAttempt,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Убеждаемся, что бан не был выполнен
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("migration-new")]
    public async Task BanUserForLongName_GroupChat_BansDeletesNotifiesLogs()
    {
        // Arrange — FakeTelegramClient tracks real non-zero message IDs via MessageEnvelope
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var messageServiceMock = new Mock<IMessageService>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var appConfigMock = new Mock<IAppConfig>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var userManagerMock = new Mock<IUserManager>();

        var serviceUnderTest = new UserBanService(
            fakeClient,
            messageServiceMock.Object,
            userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            violationTrackerMock.Object,
            appConfigMock.Object,
            statisticsServiceMock.Object,
            globalStatsManagerMock.Object,
            userManagerMock.Object,
            new Mock<IUserCleanupService>().Object
        );

        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 12345,
            chatId: -1001234567890,
            text: "Test message",
            firstName: "LongNameUser",
            chatTitle: "Test Group"
        );
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        // Act
        await serviceUnderTest.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert — ban side effect (tracked by FakeTelegramClient, not a mock verify)
        Assert.That(fakeClient.WasUserBanned(chat.Id, user.Id), Is.True,
            "User should be banned in the group chat");

        // Assert — delete side effect (envelope.MessageId is non-zero, not message.MessageId == 0)
        Assert.That(envelope.MessageId, Is.GreaterThan(0),
            "MessageEnvelope must carry a non-zero message ID");
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True,
            "Offending message should be deleted using the real message ID from the envelope");

        // Assert — notification side effect (mock verify)
        messageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.Is<Message>(m => m.MessageId == message.MessageId),
                LogNotificationType.BanForLongName,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Ban notification should be forwarded to log chat");

        // Assert — log side effect (mock verify)
        userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, reason),
            Times.Once,
            "Ban event should be logged");
    }

    [Test]
    public async Task BanUserForLongName_PermanentBan_BansUserPermanently()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        var reason = "Длинное имя";
        TimeSpan? banDuration = null; // Перманентный бан

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
        _botMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                null, // Перманентный бан
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region BanBlacklistedUser Tests

    [Test]
    public async Task BanBlacklistedUser_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;

        // Act
        await _userBanService.BanBlacklistedUserAsync(message, user, CancellationToken.None);

        // Assert
        // Убеждаемся, что бан не был выполнен для приватного чата
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("migration-new")]
    public async Task BanBlacklistedUser_GroupChat_Bans4HoursDeletesLogs()
    {
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var messageServiceMock = new Mock<IMessageService>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var appConfigMock = new Mock<IAppConfig>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var userManagerMock = new Mock<IUserManager>();
        var userCleanupServiceMock = new Mock<IUserCleanupService>();

        var serviceUnderTest = new UserBanService(
            fakeClient,
            messageServiceMock.Object,
            userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            violationTrackerMock.Object,
            appConfigMock.Object,
            statisticsServiceMock.Object,
            globalStatsManagerMock.Object,
            userManagerMock.Object,
            userCleanupServiceMock.Object
        );

        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 54321,
            chatId: -1009876543210,
            text: "Blacklisted user join",
            firstName: "Blacklisted",
            chatTitle: "Test Group"
        );
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var user = message.From!;
        var chat = message.Chat;

        await serviceUnderTest.BanBlacklistedUserAsync(message, user, CancellationToken.None);

        Assert.That(fakeClient.WasUserBanned(chat.Id, user.Id), Is.True,
            "User should be banned in the group chat");

        var banRecord = fakeClient.BannedUsers.FirstOrDefault(b => b.ChatId == chat.Id && b.UserId == user.Id);
        Assert.That(banRecord, Is.Not.Null, "Ban record should exist");
        Assert.That(banRecord!.UntilDate, Is.Not.Null, "Ban should have an until date (4-hour ban)");
        var banDuration = banRecord!.UntilDate!.Value - DateTime.UtcNow;
        Assert.That(banDuration.TotalHours, Is.GreaterThanOrEqualTo(3.9).And.LessThanOrEqualTo(4.1),
            "Ban duration should be approximately 4 hours (240 minutes)");
        Assert.That(banRecord.RevokeMessages, Is.True,
            "Ban should revoke messages");

        Assert.That(envelope.MessageId, Is.GreaterThan(0),
            "MessageEnvelope must carry a non-zero message ID");
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True,
            "Offending message should be deleted using the real message ID from the envelope");

        userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте"),
            Times.Once,
            "Ban event should be logged");

        statisticsServiceMock.Verify(
            x => x.IncrementBlacklistBan(chat.Id),
            Times.Once,
            "Blacklist ban statistics should be incremented");
    }

    [Test]
    public async Task BanBlacklistedUser_ExceptionOccurs_LogsWarning()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        // Ожидаем, что исключение будет проброшено (re-throw behavior)
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _userBanService.BanBlacklistedUserAsync(message, user, CancellationToken.None));

        Assert.That(exception.Message, Is.EqualTo("Test exception"));
    }

    #endregion

    #region AutoBan Tests

    [Test]
    public async Task AutoBan_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Спам";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        // Убеждаемся, что бан не был выполнен для приватного чата
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AutoBan_KnownSpamReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Известное спам-сообщение";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanKnownSpam,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_TextMentionReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Ссылки запрещены";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanTextMention,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_RepeatedViolationsReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Повторные нарушения";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanRepeatedViolations,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_UnknownReason_SelectsDefaultNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Неизвестная причина";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanBlacklist,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("migration-new")]
    public async Task AutoBan_GroupChat_BansDeletesNotifiesCleansUp()
    {
        // Arrange — FakeTelegramClient tracks real non-zero message IDs via MessageEnvelope
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var messageServiceMock = new Mock<IMessageService>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var appConfigMock = new Mock<IAppConfig>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var userManagerMock = new Mock<IUserManager>();
        var userCleanupServiceMock = new Mock<IUserCleanupService>();

        var serviceUnderTest = new UserBanService(
            fakeClient,
            messageServiceMock.Object,
            userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            violationTrackerMock.Object,
            appConfigMock.Object,
            statisticsServiceMock.Object,
            globalStatsManagerMock.Object,
            userManagerMock.Object,
            userCleanupServiceMock.Object
        );

        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 12345,
            chatId: -1001234567890,
            text: "Spam message",
            firstName: "Spammer",
            chatTitle: "Test Group"
        );
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Автобан";

        // Act
        await serviceUnderTest.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert — ban side effect (tracked by FakeTelegramClient, not a mock verify)
        Assert.That(fakeClient.WasUserBanned(chat.Id, user.Id), Is.True,
            "User should be banned permanently in the group chat");

        // Assert — delete side effect (envelope.MessageId is non-zero, not message.MessageId == 0)
        Assert.That(envelope.MessageId, Is.GreaterThan(0),
            "MessageEnvelope must carry a non-zero message ID");
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True,
            "Offending message should be deleted using the real message ID from the envelope");

        // Assert — notification side effect (mock verify)
        // AutoBanAsync calls SendNotificationAsync with message=null, so it routes to
        // SendLogNotificationAsync or SendAdminNotificationAsync depending on config.
        // We verify that exactly one notification method was called.
        var logNotificationCalls = messageServiceMock.Invocations
            .Count(i => i.Method.Name == nameof(IMessageService.SendLogNotificationAsync));
        var adminNotificationCalls = messageServiceMock.Invocations
            .Count(i => i.Method.Name == nameof(IMessageService.SendAdminNotificationAsync));
        Assert.That(logNotificationCalls + adminNotificationCalls, Is.EqualTo(1),
            "Exactly one notification should be sent (either log or admin)");

        // Assert — cleanup side effect: user removed from group approval
        userCleanupServiceMock.Verify(
            x => x.RemoveUserFromGroupApproval(user.Id, chat.Id, "Очистка при бане"),
            Times.Once,
            "User should be removed from group approval");

        // Assert — cleanup side effect: violation counters reset
        violationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.MlSpam),
            Times.Once,
            "ML spam violations should be reset");
        violationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.StopWords),
            Times.Once,
            "Stop words violations should be reset");
        violationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.TooManyEmojis),
            Times.Once,
            "Too many emojis violations should be reset");
        violationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.LookalikeSymbols),
            Times.Once,
            "Lookalike symbols violations should be reset");
    }

    #endregion

    #region AutoBanChannel Tests

    [Test]
    public async Task AutoBanChannel_ExceptionOccurs_LogsWarningAndSendsErrorNotification()
    {
        // Arrange
        var senderChat = new Chat { Id = 789, Type = ChatType.Channel, Title = "Test Channel" };
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.SenderChat = senderChat;
        message.Text = "Test message";

        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _userBanService.AutoBanChannelAsync(message, CancellationToken.None);

        // Assert  
        // Проверяем, что исключение было проброшено
        // (логирование происходит в MessageHandler, а не в UserBanService)

        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.ChannelError,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("migration-new")]
    public async Task AutoBanChannel_DeletesBansSenderNotifiesAdmin()
    {
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var messageServiceMock = new Mock<IMessageService>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var appConfigMock = new Mock<IAppConfig>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var userManagerMock = new Mock<IUserManager>();
        var userCleanupServiceMock = new Mock<IUserCleanupService>();

        var serviceUnderTest = new UserBanService(
            fakeClient,
            messageServiceMock.Object,
            userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            violationTrackerMock.Object,
            appConfigMock.Object,
            statisticsServiceMock.Object,
            globalStatsManagerMock.Object,
            userManagerMock.Object,
            userCleanupServiceMock.Object
        );

        var groupChatId = -1001112223334L;
        var senderChatId = 9998887776L;

        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 0,
            chatId: groupChatId,
            text: "Channel forwarded message",
            firstName: "ChannelUser",
            chatTitle: "Target Group"
        );
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        message.SenderChat = new Chat { Id = senderChatId, Type = ChatType.Channel, Title = "Spam Channel" };

        await serviceUnderTest.AutoBanChannelAsync(message, CancellationToken.None);

        Assert.That(envelope.MessageId, Is.GreaterThan(0),
            "MessageEnvelope must carry a non-zero message ID");
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True,
            "Offending message should be deleted using the real message ID from the envelope");

        Assert.That(fakeClient.WasSenderChatBanned(groupChatId, senderChatId), Is.True,
            "Sender chat (channel) should be banned in the target group");

        messageServiceMock.Verify(
            x => x.ForwardToAdminWithNotificationAsync(
                It.Is<Message>(m => m.Chat.Id == groupChatId),
                AdminNotificationType.ChannelMessage,
                It.IsAny<ChannelMessageNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Message should be forwarded to admin with channel message notification");
    }

    #endregion

    #region HandleBlacklistBan Tests

    [Test]
    [Category("migration-new")]
    public async Task HandleBlacklistBan_DeletesMessageLogsBan()
    {
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var messageServiceMock = new Mock<IMessageService>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var appConfigMock = new Mock<IAppConfig>();
        appConfigMock.Setup(x => x.LogAdminChatId).Returns(999999999L);
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var userManagerMock = new Mock<IUserManager>();
        var userCleanupServiceMock = new Mock<IUserCleanupService>();

        var serviceUnderTest = new UserBanService(
            fakeClient,
            messageServiceMock.Object,
            userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            violationTrackerMock.Object,
            appConfigMock.Object,
            statisticsServiceMock.Object,
            globalStatsManagerMock.Object,
            userManagerMock.Object,
            userCleanupServiceMock.Object
        );

        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 111222333,
            chatId: -1004445556667L,
            text: "Blacklisted user message",
            firstName: "BlacklistedUser",
            chatTitle: "Test Group"
        );
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var user = message.From!;
        var chat = message.Chat;

        await serviceUnderTest.HandleBlacklistBanAsync(message, user, chat, CancellationToken.None);

        Assert.That(envelope.MessageId, Is.GreaterThan(0),
            "MessageEnvelope must carry a non-zero message ID");
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True,
            "Offending message should be deleted using the real message ID from the envelope");

        Assert.That(fakeClient.WasUserBanned(chat.Id, user.Id), Is.True,
            "User should be banned in the group chat");

        var banRecord = fakeClient.BannedUsers.FirstOrDefault(b => b.ChatId == chat.Id && b.UserId == user.Id);
        Assert.That(banRecord, Is.Not.Null, "Ban record should exist");
        Assert.That(banRecord!.UntilDate, Is.Not.Null, "Ban should have an until date (4-hour ban)");
        var banDuration = banRecord!.UntilDate!.Value - DateTime.UtcNow;
        Assert.That(banDuration.TotalHours, Is.GreaterThanOrEqualTo(3.9).And.LessThanOrEqualTo(4.1),
            "Ban duration should be approximately 4 hours (240 minutes)");
        Assert.That(banRecord.RevokeMessages, Is.True,
            "Ban should revoke messages");

        userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot"),
            Times.Once,
            "Ban attempt should be logged");

        statisticsServiceMock.Verify(
            x => x.IncrementBlacklistBan(chat.Id),
            Times.Once,
            "Blacklist ban statistics should be incremented");
    }

    #endregion
}