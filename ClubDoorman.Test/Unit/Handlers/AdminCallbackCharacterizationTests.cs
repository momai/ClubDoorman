using ClubDoorman.Handlers;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Violation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("fast")]
public class AdminCallbackCharacterizationTests
{
    private const long AdminChatId = -1000;

    private Mock<ITelegramBotClientWrapper> _bot = null!;
    private Mock<IUserManager> _userManager = null!;
    private Mock<IBadMessageManager> _badMessageManager = null!;
    private Mock<IAiChecks> _aiChecks = null!;
    private Mock<IModerationService> _moderationService = null!;
    private Mock<IMessageService> _messageService = null!;
    private Mock<IUserBanService> _userBanService = null!;
    private Mock<ILogChatService> _logChatService = null!;
    private Mock<IAdminActionStore> _adminActionStore = null!;
    private CallbackQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _bot = new Mock<ITelegramBotClientWrapper>();
        _userManager = new Mock<IUserManager>();
        _badMessageManager = new Mock<IBadMessageManager>();
        _aiChecks = new Mock<IAiChecks>();
        _moderationService = new Mock<IModerationService>();
        _messageService = new Mock<IMessageService>();
        _userBanService = new Mock<IUserBanService>();
        _logChatService = new Mock<ILogChatService>();
        _adminActionStore = new Mock<IAdminActionStore>();

        var appConfig = new Mock<IAppConfig>();
        appConfig.SetupGet(x => x.AdminChatId).Returns(AdminChatId);
        appConfig.SetupGet(x => x.LogAdminChatId).Returns(-1001);

        var adminDispatcher = new AdminCallbackDispatcher(
            [
                new ApproveUserCallbackHandler(_userManager.Object, _bot.Object, new NullLogger<ApproveUserCallbackHandler>()),
                new BanUserCallbackHandler(_badMessageManager.Object, _userBanService.Object, _bot.Object, new NullLogger<BanUserCallbackHandler>()),
                new LogBanCallbackHandler(_logChatService.Object, _bot.Object, new NullLogger<LogBanCallbackHandler>()),
                new BanProfileCallbackHandler(_adminActionStore.Object, _userBanService.Object, _bot.Object, appConfig.Object, new NullLogger<BanProfileCallbackHandler>()),
                new AiOkCallbackHandler(_aiChecks.Object, _bot.Object, _messageService.Object, new NullLogger<AiOkCallbackHandler>()),
                new SuspiciousUserCallbackHandler(_moderationService.Object, _bot.Object, new NullLogger<SuspiciousUserCallbackHandler>()),
                new NoopCallbackHandler(_bot.Object)
            ],
            new NullLogger<AdminCallbackDispatcher>());

        _handler = new CallbackQueryHandler(
            _bot.Object,
            Mock.Of<ICaptchaService>(),
            Mock.Of<IStatisticsService>(),
            _messageService.Object,
            Mock.Of<IViolationTracker>(),
            _userBanService.Object,
            adminDispatcher,
            new NullLogger<CallbackQueryHandler>(),
            NullGoldenMasterRecorder.Instance,
            Mock.Of<IModerationEventPublisher>(),
            appConfig.Object);
    }

    [Test]
    public async Task Approve_ApprovesUserAndEditsMessage()
    {
        await HandleAsync("approve_42");

        _userManager.Verify(x => x.Approve(42, null), Times.Once);
        VerifyEditedMessageContains("Одобрен администратором Admin");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task Ban_BansUserAndEditsMessage()
    {
        await HandleAsync("ban_-2000_43");

        _userBanService.Verify(x => x.BanUserAsync(
            It.Is<Chat>(chat => chat.Id == -2000),
            It.Is<User>(user => user.Id == 43),
            BanTypeEnum.ManualBan,
            "Ручной бан",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        VerifyEditedMessageContains("Забанен администратором Admin");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task LogBan_DelegatesToLogChatAndEditsMessage()
    {
        await HandleAsync("logban_-2001_44");

        _logChatService.Verify(x => x.HandleLogBanAsync(-2001, 44, "Admin", It.IsAny<CancellationToken>()), Times.Once);
        VerifyEditedMessageContains("Пользователь очищен из всех списков");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task BanProfile_UsesStoredReviewState()
    {
        _adminActionStore
            .Setup(x => x.TakeProfileReview("opaque-token"))
            .Returns(new ProfileReviewActionState(-2002, 45, 123));

        await HandleAsync("banprofile_opaque-token");

        _userBanService.Verify(x => x.BanUserAsync(
            It.Is<Chat>(chat => chat.Id == -2002),
            It.Is<User>(user => user.Id == 45),
            BanTypeEnum.ProfileBan,
            "Бан по профилю",
            123,
            -2002,
            It.IsAny<CancellationToken>()), Times.Once);
        _bot.Verify(x => x.ForwardMessage(AdminChatId, -2002, 123, It.IsAny<CancellationToken>()), Times.Once);
        VerifyEditedMessageContains("Сообщение НЕ добавлено в автобан");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task BanProfileWithMissingState_ShowsExpiredAlertWithoutBanning()
    {
        await HandleAsync("banprofile_missing-token");

        _userBanService.VerifyNoOtherCalls();
        _bot.Verify(x => x.AnswerCallbackQuery(
            "callback-id",
            "Действие устарело или уже выполнено",
            true,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AiOkOld_MarksUserSafeWithoutUnrestricting()
    {
        await HandleAsync("aiOk_46");

        _aiChecks.Verify(x => x.MarkUserOkay(46), Times.Once);
        _bot.Verify(x => x.RestrictChatMember(
            It.IsAny<ChatId>(),
            It.IsAny<long>(),
            It.IsAny<ChatPermissions>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        VerifyEditedMessageContains("AI проверки отключены");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task AiOkNew_MarksUserSafeAndUnrestricts()
    {
        await HandleAsync("aiOk_-2003_47");

        _aiChecks.Verify(x => x.MarkUserOkay(47), Times.Once);
        _bot.Verify(x => x.RestrictChatMember(
            -2003,
            47,
            It.Is<ChatPermissions>(permissions => permissions.CanSendMessages),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        VerifyEditedMessageContains("ограничения сняты");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task SuspiciousApprove_UnrestrictsAndApprovesUser()
    {
        _moderationService.Setup(x => x.UnrestrictAndApproveUserAsync(48, -2004)).ReturnsAsync(true);

        await HandleAsync("suspicious_approve_48_-2004");

        _moderationService.Verify(x => x.UnrestrictAndApproveUserAsync(48, -2004), Times.Once);
        VerifyEditedMessageContains("Разблокирован и одобрен");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task SuspiciousBan_BansAndDeletesOriginalMessage()
    {
        _moderationService.Setup(x => x.BanAndCleanupUserAsync(49, -2005, null)).ReturnsAsync(true);

        await HandleAsync("suspicious_ban_49_-2005_322");

        _moderationService.Verify(x => x.BanAndCleanupUserAsync(49, -2005, null), Times.Once);
        _bot.Verify(x => x.DeleteMessage(-2005, 322, It.IsAny<CancellationToken>()), Times.Once);
        VerifyEditedMessageContains("Забанен и очищен");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task SuspiciousBanWithoutMessageId_BansWithoutDeletingOriginalMessage()
    {
        _moderationService.Setup(x => x.BanAndCleanupUserAsync(51, -2007, null)).ReturnsAsync(true);

        await HandleAsync("suspicious_ban_51_-2007");

        _moderationService.Verify(x => x.BanAndCleanupUserAsync(51, -2007, null), Times.Once);
        _bot.Verify(x => x.DeleteMessage(-2007, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyEditedMessageContains("Забанен и очищен");
        VerifyAnsweredOnce();
    }

    [Test]
    public async Task SuspiciousAi_TogglesAiDetection()
    {
        _moderationService.Setup(x => x.GetAiDetectUsers()).Returns([]);
        _moderationService.Setup(x => x.SetAiDetectForSuspiciousUser(50, -2006, true)).Returns(true);

        await HandleAsync("suspicious_ai_50_-2006");

        _moderationService.Verify(x => x.SetAiDetectForSuspiciousUser(50, -2006, true), Times.Once);
        VerifyEditedMessageContains("AI детект включен");
        VerifyAnsweredOnce();
    }

    [Test]
    public void CancellationFromTypedHandler_PropagatesWithoutCallbackAnswer()
    {
        _bot.Setup(x => x.RestrictChatMember(
                -2008,
                52,
                It.IsAny<ChatPermissions>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() => HandleAsync("aiOk_-2008_52"));

        _bot.Verify(x => x.AnswerCallbackQuery(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Noop_RemovesButtons()
    {
        await HandleAsync("noop");

        _bot.Verify(x => x.EditMessageReplyMarkup(AdminChatId, 0, null, It.IsAny<CancellationToken>()), Times.Once);
        VerifyAnsweredOnce();
    }

    private Task HandleAsync(string data)
    {
        return _handler.HandleAsync(new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "callback-id",
                Data = data,
                From = new User { Id = 1, FirstName = "Admin" },
                Message = new Message
                {
                    Text = "Review",
                    Chat = new Chat { Id = AdminChatId }
                }
            }
        });
    }

    private void VerifyEditedMessageContains(string expected)
    {
        _bot.Verify(x => x.EditMessageText(
            AdminChatId,
            0,
            It.Is<string>(text => text.Contains(expected)),
            It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
            It.IsAny<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyAnsweredOnce()
    {
        _bot.Verify(x => x.AnswerCallbackQuery(
            "callback-id",
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
