using ClubDoorman.Effects.Moderation;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Features.Moderation;

[TestFixture]
public class ModerationFacadeTests
{
    [Test]
    public async Task HandleUserMessageAsync_DispatchesExplicitRuntimeContext()
    {
        var messageUser = new User { Id = 1 };
        var messageChat = new Chat { Id = 2 };
        var facadeUser = new User { Id = 3 };
        var facadeChat = new Chat { Id = 4 };
        var message = new Message
        {
            From = messageUser,
            Chat = messageChat,
            Text = "test"
        };
        var result = new ModerationResult(ModerationAction.Report, "reason", 0.8);
        using var cancellation = new CancellationTokenSource();
        ModerationActionContext? dispatchedContext = null;
        CancellationToken dispatchedToken = default;
        var dispatcher = new Mock<IModerationActionDispatcher>();
        dispatcher
            .Setup(service => service.DispatchAsync(
                It.IsAny<ModerationActionContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<ModerationActionContext, CancellationToken>((context, token) =>
            {
                dispatchedContext = context;
                dispatchedToken = token;
            })
            .Returns(Task.CompletedTask);
        var facade = new ModerationFacade(
            new Mock<IModerationPolicy>().Object,
            NullLogger<ModerationFacade>.Instance,
            dispatcher.Object);

        await facade.HandleUserMessageAsync(
            message,
            facadeUser,
            facadeChat,
            result,
            true,
            cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(dispatchedContext, Is.Not.Null);
            Assert.That(dispatchedContext!.Message, Is.SameAs(message));
            Assert.That(dispatchedContext.User, Is.SameAs(facadeUser));
            Assert.That(dispatchedContext.Chat, Is.SameAs(facadeChat));
            Assert.That(dispatchedContext.Result, Is.SameAs(result));
            Assert.That(dispatchedContext.IsSilentMode, Is.True);
            Assert.That(dispatchedToken, Is.EqualTo(cancellation.Token));
        });
    }
}
