using ClubDoorman.Effects.Channel;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class ChannelModerationServiceTests
{
    private Mock<ITelegramBotClientWrapper> _bot = null!;
    private Mock<IContentModerationPolicy> _contentPolicy = null!;
    private Mock<IChannelModerationActionDispatcher> _dispatcher = null!;
    private Mock<IAppConfig> _appConfig = null!;
    private ChannelModerationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bot = new Mock<ITelegramBotClientWrapper>();
        _contentPolicy = new Mock<IContentModerationPolicy>();
        _dispatcher = new Mock<IChannelModerationActionDispatcher>();
        _appConfig = new Mock<IAppConfig>();
        _service = new ChannelModerationService(
            _bot.Object,
            _contentPolicy.Object,
            _dispatcher.Object,
            NullLogger<ChannelModerationService>.Instance,
            _appConfig.Object);
    }

    [Test]
    public async Task HandleChannelMessageAsync_AnonymousGroupAdmin_BypassesModeration()
    {
        var message = CreateMessage();
        message.SenderChat = message.Chat;

        await _service.HandleChannelMessageAsync(message, false);

        _contentPolicy.Verify(
            x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleChannelMessageAsync_FakeFrom_UsesContentAndSenderChatIdentity()
    {
        var message = CreateMessage();
        message.From = new User
        {
            Id = 1087968824,
            IsBot = true,
            FirstName = "GroupAnonymousBot"
        };
        var expected = new ModerationResult(ModerationAction.Allow, "allowed");
        _contentPolicy
            .Setup(x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), CancellationToken.None))
            .ReturnsAsync(expected);

        await _service.HandleChannelMessageAsync(message, true);

        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.Is<ChannelModerationContext>(context =>
                    context.SenderChat == message.SenderChat &&
                    context.Content.DestinationChat == message.Chat &&
                    context.IsSilentMode),
                expected,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task HandleChannelMessageAsync_ContentPolicyFails_DispatchesManualReview()
    {
        var message = CreateMessage();
        _contentPolicy
            .Setup(x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("content failure"));

        await _service.HandleChannelMessageAsync(message, false);

        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.IsAny<ChannelModerationContext>(),
                It.Is<ModerationResult>(result => result.Action == ModerationAction.RequireManualReview),
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void HandleChannelMessageAsync_ContentPolicyIsCancelled_DoesNotDispatchReview()
    {
        var message = CreateMessage();
        _contentPolicy
            .Setup(x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.HandleChannelMessageAsync(message, false));
        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.IsAny<ChannelModerationContext>(),
                It.IsAny<ModerationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void HandleChannelMessageAsync_DiscussionLookupCancelled_DoesNotDispatch()
    {
        var message = CreateMessage();
        _bot
            .Setup(x => x.GetChatFullInfo(message.Chat.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.HandleChannelMessageAsync(message, false));
        _contentPolicy.Verify(
            x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.IsAny<ChannelModerationContext>(),
                It.IsAny<ModerationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleChannelMessageAsync_ChannelAutoBan_DispatchesBanWithoutContentCheck()
    {
        var message = CreateMessage();
        _appConfig.SetupGet(x => x.ChannelAutoBan).Returns(true);

        await _service.HandleChannelMessageAsync(message, false);

        _contentPolicy.Verify(
            x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.IsAny<ChannelModerationContext>(),
                It.Is<ModerationResult>(result => result.Action == ModerationAction.Ban),
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task IsChannelDiscussionAsync_AutomaticForward_ReturnsTrue()
    {
        var message = CreateMessage();
        message.IsAutomaticForward = true;

        var result = await _service.IsChannelDiscussionAsync(message);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HandleChannelMessageAsync_LinkedDiscussion_BypassesModeration()
    {
        var message = CreateMessage();
        _bot
            .Setup(x => x.GetChatFullInfo(message.Chat.Id, CancellationToken.None))
            .ReturnsAsync(new ChatFullInfo
            {
                Id = message.Chat.Id,
                Type = message.Chat.Type,
                LinkedChatId = message.SenderChat!.Id
            });

        await _service.HandleChannelMessageAsync(message, false);

        _contentPolicy.Verify(
            x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dispatcher.Verify(
            x => x.DispatchAsync(
                It.IsAny<ChannelModerationContext>(),
                It.IsAny<ModerationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Message CreateMessage() => new()
    {
        Chat = new Chat { Id = -1009876543299, Title = "Test Chat", Type = ChatType.Supergroup },
        SenderChat = new Chat { Id = -1009876543298, Title = "Test Channel", Type = ChatType.Channel },
        Text = "Test message"
    };
}
