using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Handlers.Pipeline;
using ClubDoorman.Services.Handlers.Pipeline.Steps;
using ClubDoorman.Services.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
public class ChannelMessageStepTests
{
    [Test]
    public async Task ExecuteAsync_ExternalChannelIdentity_UsesChannelModeration()
    {
        var moderation = new Mock<IChannelModerationService>();
        var step = new ChannelMessageStep(
            moderation.Object,
            Mock.Of<IModerationEventPublisher>(),
            NullLogger<ChannelMessageStep>.Instance);
        var message = new Message
        {
            Chat = new Chat { Id = -1001, Type = ChatType.Supergroup, Title = "Group" },
            SenderChat = new Chat { Id = -1002, Type = ChatType.Channel, Title = "Sender" },
            Text = "message"
        };
        var context = CreateContext(message);
        context.IsSilentMode = true;

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.True);
        Assert.That(context.ChannelMessageHandled, Is.True);
        moderation.Verify(
            x => x.HandleChannelMessageAsync(message, true, CancellationToken.None),
            Times.Once);
    }

    private static MessageContext CreateContext(Message message) => new()
    {
        Update = new Update { Message = message },
        Message = message
    };
}
