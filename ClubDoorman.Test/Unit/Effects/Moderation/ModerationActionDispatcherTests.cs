using ClubDoorman.Effects.Moderation;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects.Moderation;

[TestFixture]
public class ModerationActionDispatcherTests
{
    [Test]
    public async Task DispatchAsync_ExecutesOnlyHandlerForSelectedAction()
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Select(action => new RecordingHandler(action))
            .ToArray();
        var dispatcher = CreateDispatcher(handlers);
        var context = CreateContext(ModerationAction.Ban);

        await dispatcher.DispatchAsync(context, CancellationToken.None);

        Assert.That(handlers.Single(handler => handler.Action == ModerationAction.Ban).Calls, Is.EqualTo(1));
        Assert.That(handlers.Where(handler => handler.Action != ModerationAction.Ban).Sum(handler => handler.Calls), Is.Zero);
    }

    [Test]
    public void Constructor_WhenActionIsMissing_FailsWithActionName()
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Where(action => action != ModerationAction.Report)
            .Select(action => new RecordingHandler(action));

        var exception = Assert.Throws<InvalidOperationException>(() => CreateDispatcher(handlers));

        Assert.That(exception!.Message, Does.Contain(nameof(ModerationAction.Report)));
    }

    [Test]
    public void Constructor_WhenActionIsDuplicated_FailsWithActionName()
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Select(action => new RecordingHandler(action))
            .Append(new RecordingHandler(ModerationAction.Delete));

        var exception = Assert.Throws<InvalidOperationException>(() => CreateDispatcher(handlers));

        Assert.That(exception!.Message, Does.Contain(nameof(ModerationAction.Delete)));
    }

    [Test]
    public void DispatchAsync_WhenActionIsUnknown_FailsInsteadOfSilentlySkippingIt()
    {
        var handlers = Enum.GetValues<ModerationAction>()
            .Select(action => new RecordingHandler(action));
        var dispatcher = CreateDispatcher(handlers);
        var context = CreateContext((ModerationAction)999);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.DispatchAsync(context, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain("999"));
    }

    private static ModerationActionDispatcher CreateDispatcher(IEnumerable<IModerationActionHandler> handlers)
    {
        return new ModerationActionDispatcher(
            handlers,
            NullLogger<ModerationActionDispatcher>.Instance);
    }

    private static ModerationActionContext CreateContext(ModerationAction action)
    {
        var user = new User { Id = 1 };
        var chat = new Chat { Id = 2 };
        var message = new Message { From = user, Chat = chat, Text = "test" };
        return new ModerationActionContext(
            message,
            user,
            chat,
            new ModerationResult(action, "reason"),
            false);
    }

    private sealed class RecordingHandler(ModerationAction action) : IModerationActionHandler
    {
        public ModerationAction Action { get; } = action;

        public int Calls { get; private set; }

        public Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
