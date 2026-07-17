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
        var dispatcher = new ServiceChatDispatcher(
            bot,
            NullLogger<ServiceChatDispatcher>.Instance,
            config.Object);
        var data = CreateData(messageId: 123);

        await dispatcher.SendToAdminChatAsync(data);

        var sent = HasSingleSentMessage(bot);
        Assert.That(sent.ChatId, Is.EqualTo(config.Object.AdminChatId));
        Assert.That(sent.Text, Does.Contain("AI анализ профиля"));
        Assert.That(sent.Text, Does.Contain("Test &lt;reason&gt;"));
        Assert.That(sent.Text, Does.Contain(string.Format(CultureInfo.CurrentCulture, "{0:F1}%", 95.0)));
        Assert.That(ContainsCallback(sent.ReplyMarkup!, "banprofile_67890_12345"), Is.True);
        Assert.That(bot.SentPhotos, Is.Empty);
    }

    [Test]
    public async Task ServiceChatDispatcher_AiProfileAnalysisWithPhoto_SendsPhotoAndRepliesWithAnalysis()
    {
        var bot = TestKitTelegram.CreateFakeClient();
        var config = CreateConfig();
        var dispatcher = new ServiceChatDispatcher(
            bot,
            NullLogger<ServiceChatDispatcher>.Instance,
            config.Object);
        var data = CreateData(messageId: 123, photoBytes: new byte[] { 1, 2, 3 });

        await dispatcher.SendToAdminChatAsync(data);

        Assert.That(bot.SentPhotos, Has.Count.EqualTo(1));
        Assert.That(bot.SentPhotos[0].ChatId, Is.EqualTo(config.Object.AdminChatId));
        Assert.That(bot.SentPhotos[0].Caption, Does.Contain("Test message"));
        var sent = HasSingleSentMessage(bot);
        Assert.That(sent.Text, Does.Contain("AI анализ профиля"));
        Assert.That(sent.ReplyParameters, Is.Not.Null);
    }

    private static AiProfileAnalysisData CreateData(
        long? messageId = null,
        byte[]? photoBytes = null) =>
        new(
            new User { Id = 12345, FirstName = "Test", LastName = "User" },
            new Chat { Id = 67890, Type = ChatType.Group, Title = "Test Chat" },
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
