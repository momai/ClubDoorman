using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class AiNotificationDeliveryTests
{
    private const long TestChatId = 678901;
    private const long TestUserId = 123451;

    [Test]
    public async Task MessageService_SendAiProfileAnalysis_RoutesToAdminDispatcher()
    {
        var dispatcher = new Mock<IServiceChatDispatcher>();
        dispatcher.Setup(x => x.ShouldSendToAdminChat(It.IsAny<NotificationData>())).Returns(true);
        var data = CreateData();
        var service = CreateMessageService(dispatcher);

        await service.SendAiProfileAnalysisAsync(data);

        dispatcher.Verify(x => x.ShouldSendToAdminChat(data), Times.Once);
        dispatcher.Verify(x => x.SendToAdminChatAsync(data, It.IsAny<CancellationToken>()), Times.Once);
        dispatcher.Verify(x => x.SendToLogChatAsync(It.IsAny<NotificationData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ServiceChatDispatcher_AiProfileAnalysis_SendsAdminMessageWithReasonAndActions()
    {
        var bot = TestKitTelegram.CreateFakeClient();
        var config = CreateConfig();
        var actionStore = new Mock<IAdminActionStore>();
        actionStore.Setup(x => x.PutProfileReview(It.IsAny<ProfileReviewActionState>(), It.IsAny<TimeSpan>()))
            .Returns("opaque-token");
        var dispatcher = new ServiceChatDispatcher(
            bot,
            NullLogger<ServiceChatDispatcher>.Instance,
            config.Object,
            actionStore.Object);
        var data = CreateData(messageId: 123);

        await dispatcher.SendToAdminChatAsync(data);

        var sent = HasSingleSentMessage(bot);
        Assert.That(sent.ChatId, Is.EqualTo(config.Object.AdminChatId));
        Assert.That(sent.Text, Does.Contain("AI анализ профиля"));
        Assert.That(sent.Text, Does.Contain("Test &lt;reason&gt;"));
        Assert.That(sent.Text, Does.Contain(string.Format(CultureInfo.CurrentCulture, "{0:F1}%", 95.0)));
        Assert.That(ContainsCallback(sent.ReplyMarkup!, "banprofile_opaque-token"), Is.True);
        actionStore.Verify(x => x.PutProfileReview(
            It.Is<ProfileReviewActionState>(state =>
                state.ChatId == TestChatId &&
                state.UserId == TestUserId &&
                state.MessageId == 123),
            TimeSpan.FromHours(12)), Times.Once);
        Assert.That(bot.SentPhotos, Is.Empty);
    }

    [Test]
    public async Task ServiceChatDispatcher_AiProfileAnalysisWithPhoto_SendsPhotoAndSetsReplyParameters()
    {
        var bot = TestKitTelegram.CreateFakeClient();
        var config = CreateConfig();
        var actionStore = new Mock<IAdminActionStore>();
        actionStore.Setup(x => x.PutProfileReview(It.IsAny<ProfileReviewActionState>(), It.IsAny<TimeSpan>()))
            .Returns("opaque-token");
        var dispatcher = new ServiceChatDispatcher(
            bot,
            NullLogger<ServiceChatDispatcher>.Instance,
            config.Object,
            actionStore.Object);
        var data = CreateData(messageId: 123, photoBytes: new byte[] { 1, 2, 3 });

        await dispatcher.SendToAdminChatAsync(data);

        Assert.That(bot.SentPhotos, Has.Count.EqualTo(1));
        Assert.That(bot.SentPhotos[0].ChatId, Is.EqualTo(config.Object.AdminChatId));
        Assert.That(bot.SentPhotos[0].Caption, Does.Contain("Test message"));
        var sent = HasSingleSentMessage(bot);
        Assert.That(sent.Text, Does.Contain("AI анализ профиля"));
        Assert.That(sent.ReplyParameters, Is.Not.Null);
    }

    [Test]
    public void ServiceChatDispatcher_MainMessageFails_DiscardsProfileReviewState()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        bot.Setup(x => x.ForwardMessage(
                It.IsAny<ChatId>(),
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());
        bot.Setup(x => x.SendMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<ReplyMarkup?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Telegram unavailable"));

        using var store = new AdminActionStore();
        string? token = null;
        var actionStore = new Mock<IAdminActionStore>();
        actionStore.Setup(x => x.PutProfileReview(It.IsAny<ProfileReviewActionState>(), It.IsAny<TimeSpan>()))
            .Returns((ProfileReviewActionState state, TimeSpan ttl) =>
            {
                token = store.PutProfileReview(state, ttl);
                return token;
            });
        actionStore.Setup(x => x.DiscardProfileReview(It.IsAny<string>()))
            .Callback((string value) => store.DiscardProfileReview(value));
        var dispatcher = new ServiceChatDispatcher(
            bot.Object,
            NullLogger<ServiceChatDispatcher>.Instance,
            CreateConfig().Object,
            actionStore.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.SendToAdminChatAsync(CreateData(messageId: 123)));

        Assert.That(token, Is.Not.Null);
        actionStore.Verify(x => x.DiscardProfileReview(token!), Times.Once);
        Assert.That(store.TakeProfileReview(token!), Is.Null);
    }

    private static AiProfileAnalysisData CreateData(
        long? messageId = null,
        byte[]? photoBytes = null) =>
        new(
            new User { Id = TestUserId, FirstName = "Test", LastName = "User" },
            new Chat { Id = TestChatId, Type = ChatType.Group, Title = "Test Chat" },
            0.95,
            "Test <reason>",
            "Test name bio",
            "Test message",
            photoBytes,
            messageId,
            "Read-only");

    private static Mock<IAppConfig> CreateConfig()
    {
        var config = new Mock<IAppConfig>();
        config.SetupGet(x => x.AdminChatId).Returns(123456);
        config.SetupGet(x => x.LogAdminChatId).Returns(654321);
        return config;
    }

    private static MessageService CreateMessageService(Mock<IServiceChatDispatcher> dispatcher)
    {
        var appConfig = CreateConfig();
        return new MessageService(
            new Mock<ITelegramBotClientWrapper>().Object,
            NullLogger<MessageService>.Instance,
            new MessageTemplates(appConfig.Object),
            new Mock<ILoggingConfigurationService>().Object,
            dispatcher.Object,
            appConfig.Object);
    }

    private static bool ContainsCallback(ReplyMarkup markup, string callbackData)
    {
        return markup is InlineKeyboardMarkup keyboard &&
               keyboard.InlineKeyboard.SelectMany(row => row)
                   .Any(button => button.CallbackData == callbackData);
    }

    private static SentMessage HasSingleSentMessage(FakeTelegramClient bot)
    {
        Assert.That(bot.SentMessages, Has.Count.EqualTo(1));
        return bot.SentMessages[0];
    }
}
