using ClubDoorman.Models;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.AI;
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
public class AiProfileAnalysisStepTests
{
    [Test]
    public async Task ExecuteAsync_AllowedHumanMessage_PassesMessageToCascade()
    {
        var cascade = new Mock<IAiCascadeService>();
        cascade
            .Setup(x => x.PerformAiProfileAnalysisAsync(
                It.IsAny<Message>(),
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var events = new Mock<IModerationEventPublisher>();
        var step = CreateStep(cascade, events);
        var user = new User { Id = 42, FirstName = "User" };
        var message = CreateMessage(user, text: null, caption: "caption text");
        var context = CreateContext(message, ModerationAction.Allow);

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.False);
        cascade.Verify(x => x.PerformAiProfileAnalysisAsync(
            It.Is<Message>(candidate => ReferenceEquals(candidate, message)),
            It.Is<User>(candidate => candidate.Id == user.Id),
            It.Is<Chat>(candidate => candidate.Id == message.Chat.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        events.Verify(x => x.Publish(It.IsAny<string?>(), It.IsAny<ModerationEvent>()), Times.Never);
    }

    [TestCase(ModerationAction.Delete)]
    [TestCase(ModerationAction.Ban)]
    [TestCase(ModerationAction.Report)]
    public async Task ExecuteAsync_NonAllowModeration_DoesNotRunCascade(ModerationAction action)
    {
        var cascade = new Mock<IAiCascadeService>();
        var step = CreateStep(cascade, new Mock<IModerationEventPublisher>());
        var message = CreateMessage(new User { Id = 42, FirstName = "User" }, "text", null);
        var context = CreateContext(message, action);

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.False);
        cascade.Verify(x => x.PerformAiProfileAnalysisAsync(
            It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_BotMessage_DoesNotRunCascade()
    {
        var cascade = new Mock<IAiCascadeService>();
        var step = CreateStep(cascade, new Mock<IModerationEventPublisher>());
        var message = CreateMessage(new User { Id = 42, IsBot = true, FirstName = "Bot" }, "text", null);
        var context = CreateContext(message, ModerationAction.Allow);

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.False);
        cascade.Verify(x => x.PerformAiProfileAnalysisAsync(
            It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_CascadeRestrictsUser_StopsAndPublishesSemanticEvent()
    {
        var cascade = new Mock<IAiCascadeService>();
        cascade
            .Setup(x => x.PerformAiProfileAnalysisAsync(
                It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var events = new Mock<IModerationEventPublisher>();
        var step = CreateStep(cascade, events);
        var message = CreateMessage(new User { Id = 42, FirstName = "User" }, "text", null);
        var context = CreateContext(message, ModerationAction.Allow);

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.True);
        Assert.That(result.Failed, Is.False);
        Assert.That(result.Reason, Is.EqualTo("ai_profile_restricted"));
        Assert.That(context.AiProfileRestricted, Is.True);
        Assert.That(context.UserResultHandled, Is.True);
        events.Verify(x => x.Publish(
            context.GmCorrelation,
            It.Is<ModerationEvent>(eventData =>
                eventData.Kind == "ai_profile_restricted" &&
                eventData.RuleCode == RuleCode.AiProfileRestricted)), Times.Once);
    }

    private static AiProfileAnalysisStep CreateStep(
        Mock<IAiCascadeService> cascade,
        Mock<IModerationEventPublisher> events) =>
        new(
            cascade.Object,
            events.Object,
            NullLogger<AiProfileAnalysisStep>.Instance);

    private static MessageContext CreateContext(Message message, ModerationAction action) =>
        new()
        {
            Update = new Update { Message = message },
            Message = message,
            ModerationResult = new ModerationResult(action, "test")
        };

    private static Message CreateMessage(User user, string? text, string? caption) => new()
    {
        From = user,
        Chat = new Chat { Id = -100123, Type = ChatType.Supergroup },
        Text = text,
        Caption = caption
    };
}
