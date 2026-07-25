using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
// using ClubDoorman.Services.UserBan; // duplicate removed
using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для CallbackQueryHandler
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class CallbackQueryHandlerTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IAiChecks> AiChecksMock { get; } = new();
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    public Mock<IViolationTracker> ViolationTrackerMock { get; } = new();
    public Mock<IUserBanService> UserBanServiceMock { get; } = new();
    public Mock<IAdminActionStore> AdminActionStoreMock { get; } = new();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = new();
    public Mock<ILogger<CallbackQueryHandler>> LoggerMock { get; } = new();
    public Mock<IAppConfig> AppConfigMock { get; } = new();

    public CallbackQueryHandler CreateCallbackQueryHandler()
    {
        return new CallbackQueryHandler(
            BotMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            BadMessageManagerMock.Object,
            StatisticsServiceMock.Object,
            AiChecksMock.Object,
            ModerationServiceMock.Object,
            MessageServiceMock.Object,
            ViolationTrackerMock.Object,
            UserBanServiceMock.Object,
            new Mock<ILogChatService>().Object,
            AdminActionStoreMock.Object,
            LoggerMock.Object,
            NullGoldenMasterRecorder.Instance,
            new Mock<IModerationEventPublisher>().Object,
            AppConfigMock.Object
        );
    }

    #region Configuration Methods

    public CallbackQueryHandlerTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithBadMessageManagerSetup(Action<Mock<IBadMessageManager>> setup)
    {
        setup(BadMessageManagerMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithAiChecksSetup(Action<Mock<IAiChecks>> setup)
    {
        setup(AiChecksMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<CallbackQueryHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithViolationTrackerSetup(Action<Mock<IViolationTracker>> setup)
    {
        setup(ViolationTrackerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();

    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public ModerationServiceAdapter CreateModerationServiceWithFake()
    {
        return new ModerationServiceAdapter(
            new Mock<IModerationPolicy>().Object
        );
    }

    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return new CaptchaService(
            new Mock<ITelegramBotClientWrapper>().Object,
            new Mock<ILogger<CaptchaService>>().Object,
            new Mock<IMessageService>().Object,
            AppConfigTestFactory.CreateDefault(),
            new Mock<IViolationTracker>().Object,
            new Mock<IUserBanService>().Object
        );
    }

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public async Task<CallbackQueryHandler> CreateAsync()
    {
        return await Task.FromResult(CreateCallbackQueryHandler());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}
