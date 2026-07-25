using ClubDoorman.Effects.Channel;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Effects.Channel;

[TestFixture]
public class ChannelModerationActionDispatcherTests
{
    [TestCaseSource(nameof(AllActions))]
    public async Task DispatchAsync_RoutesEveryAction(ModerationAction action)
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Select(CreateHandler)
            .ToArray();
        var dispatcher = new ChannelModerationActionDispatcher(
            handlers.Select(mock => mock.Object),
            NullLogger<ChannelModerationActionDispatcher>.Instance);
        var context = CreateContext();
        var result = new ModerationResult(action, "reason");

        await dispatcher.DispatchAsync(context, result, CancellationToken.None);

        handlers.Single(mock => mock.Object.Action == action).Verify(
            x => x.ExecuteAsync(context, result, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void Constructor_MissingHandler_Throws()
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Where(action => action != ModerationAction.Report)
            .Select(action => CreateHandler(action).Object);

        Assert.Throws<InvalidOperationException>(() =>
            new ChannelModerationActionDispatcher(
                handlers,
                NullLogger<ChannelModerationActionDispatcher>.Instance));
    }

    private static IEnumerable<ModerationAction> AllActions() => Enum.GetValues<ModerationAction>();

    private static Mock<IChannelModerationActionHandler> CreateHandler(ModerationAction action)
    {
        var handler = new Mock<IChannelModerationActionHandler>();
        handler.SetupGet(x => x.Action).Returns(action);
        return handler;
    }

    private static ChannelModerationContext CreateContext()
    {
        var chat = new Chat { Id = -1, Type = ChatType.Supergroup };
        var senderChat = new Chat { Id = -2, Type = ChatType.Channel };
        var message = new Message { Chat = chat, SenderChat = senderChat, Text = "text" };
        return new ChannelModerationContext(
            new ContentModerationInput(message, chat, message.Text),
            senderChat,
            false);
    }
}
