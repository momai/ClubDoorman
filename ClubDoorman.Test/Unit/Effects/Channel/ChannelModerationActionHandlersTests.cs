using ClubDoorman.Effects.Channel;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Effects.Channel;

[TestFixture]
public class ChannelModerationActionHandlersTests
{
    [Test]
    public async Task Delete_DeletesMessageThroughTelegramWrapper()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var handler = new ChannelDeleteActionHandler(bot.Object, NullLogger<ChannelDeleteActionHandler>.Instance);
        var context = CreateContext();
        using var cts = new CancellationTokenSource();

        await handler.ExecuteAsync(context, Result(ModerationAction.Delete), cts.Token);

        bot.Verify(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), cts.Token), Times.Once);
    }

    [Test]
    public async Task Ban_BansSenderChatIdentity()
    {
        var banService = new Mock<IUserBanService>();
        var handler = new ChannelBanActionHandler(
            banService.Object,
            NullLogger<ChannelBanActionHandler>.Instance);
        var context = CreateContext();

        await handler.ExecuteAsync(context, Result(ModerationAction.Ban), CancellationToken.None);

        banService.Verify(
            x => x.AutoBanChannelAsync(context.Content.Message, CancellationToken.None),
            Times.Once);
    }

    [TestCase(ModerationAction.Report)]
    [TestCase(ModerationAction.RequireManualReview)]
    [TestCase(ModerationAction.RequireAiAnalysis)]
    public async Task ReviewActions_ReportChannelMessage(ModerationAction action)
    {
        var reporter = new Mock<IChannelModerationReporter>();
        IChannelModerationActionHandler handler = action switch
        {
            ModerationAction.Report => new ChannelReportActionHandler(reporter.Object),
            ModerationAction.RequireManualReview => new ChannelManualReviewActionHandler(reporter.Object),
            _ => new ChannelAiAnalysisActionHandler(reporter.Object)
        };
        var context = CreateContext();
        var result = Result(action);

        await handler.ExecuteAsync(context, result, CancellationToken.None);

        reporter.Verify(x => x.ReportAsync(context, result, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task Reporter_WhenForwardSucceeds_DoesNotSendTextFallback()
    {
        var messages = new Mock<IMessageService>();
        messages
            .Setup(x => x.ForwardToAdminWithNotificationAsync(
                It.IsAny<Message>(),
                AdminNotificationType.ChannelMessage,
                It.IsAny<ChannelMessageNotificationData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());
        var reporter = new ChannelModerationReporter(
            messages.Object,
            NullLogger<ChannelModerationReporter>.Instance);

        await reporter.ReportAsync(CreateContext(isSilentMode: true), Result(ModerationAction.Report), CancellationToken.None);

        messages.Verify(
            x => x.ForwardToAdminWithNotificationAsync(
                It.IsAny<Message>(),
                AdminNotificationType.ChannelMessage,
                It.Is<ChannelMessageNotificationData>(data =>
                    data.MessageText == "hello" &&
                    data.Reason == ModerationAction.Report.ToString() &&
                    data.IsSilentMode),
                CancellationToken.None),
            Times.Once);
        messages.Verify(
            x => x.SendAdminNotificationAsync(
                It.IsAny<AdminNotificationType>(),
                It.IsAny<NotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Reporter_WhenForwardFails_SendsOriginalTextAsFallback()
    {
        var messages = new Mock<IMessageService>();
        messages
            .Setup(x => x.ForwardToAdminWithNotificationAsync(
                It.IsAny<Message>(),
                AdminNotificationType.ChannelMessage,
                It.IsAny<ChannelMessageNotificationData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);
        var reporter = new ChannelModerationReporter(
            messages.Object,
            NullLogger<ChannelModerationReporter>.Instance);

        await reporter.ReportAsync(CreateContext(isSilentMode: true), Result(ModerationAction.Report), CancellationToken.None);

        messages.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.ChannelMessage,
                It.Is<ChannelMessageNotificationData>(data =>
                    data.MessageText == "hello" &&
                    data.SenderChat.Id == -2 &&
                    data.Reason == ModerationAction.Report.ToString() &&
                    data.IsSilentMode),
                CancellationToken.None),
            Times.Once);
    }

    private static ModerationResult Result(ModerationAction action) => new(action, action.ToString());

    private static ChannelModerationContext CreateContext(bool isSilentMode = false)
    {
        var chat = new Chat { Id = -1, Type = ChatType.Supergroup, Title = "Chat" };
        var senderChat = new Chat { Id = -2, Type = ChatType.Channel, Title = "Channel" };
        var message = new Message { Chat = chat, SenderChat = senderChat, Text = "hello" };
        return new ChannelModerationContext(
            new ContentModerationInput(message, chat, message.Text),
            senderChat,
            isSilentMode);
    }
}
