using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.Unit.Features.Moderation;

[TestFixture]
public class ContentModerationPolicyTests
{
    [Test]
    public async Task CheckContentAsync_MessageWithButtons_ReturnsBan()
    {
        var policy = CreatePolicy();
        var input = CreateInput();
        input.Message.ReplyMarkup = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("button", "callback"));

        var result = await policy.CheckContentAsync(input, CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
    }

    [Test]
    public async Task CheckContentAsync_ClassifierDetectsSpam_ReturnsDelete()
    {
        var policy = CreatePolicy(classifierResult: (true, 1f));

        var result = await policy.CheckContentAsync(CreateInput(), CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
    }

    [Test]
    public async Task CheckContentAsync_KnownBadMessage_ReturnsBan()
    {
        var policy = CreatePolicy(knownBad: true);

        var result = await policy.CheckContentAsync(CreateInput(), CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
    }

    [Test]
    public async Task CheckContentAsync_Url_ReturnsDeleteBeforeClassifier()
    {
        var policy = CreatePolicy(textMentionFilterEnabled: true);

        var result = await policy.CheckContentAsync(
            CreateInput("https://example.com"),
            CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
    }

    [Test]
    public async Task CheckContentAsync_DocumentWithoutCaption_ReturnsDelete()
    {
        var policy = CreatePolicy();
        var input = CreateInput(text: null);
        input.Message.Document = new Document
        {
            FileId = "file",
            FileUniqueId = "unique"
        };

        var result = await policy.CheckContentAsync(input, CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
    }

    [Test]
    public async Task CheckContentAsync_UnsignedContent_ReturnsReport()
    {
        var policy = CreatePolicy();
        var input = CreateInput(text: null);

        var result = await policy.CheckContentAsync(input, CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
    }

    [Test]
    public async Task CheckContentAsync_ConfidentHam_ReturnsAllow()
    {
        var policy = CreatePolicy(classifierResult: (false, -1f), lowConfidenceHamForward: true);

        var result = await policy.CheckContentAsync(CreateInput(), CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckContentAsync_LowConfidenceHam_ReturnsRequireAiAnalysis()
    {
        var policy = CreatePolicy(classifierResult: (false, 0f), lowConfidenceHamForward: true);

        var result = await policy.CheckContentAsync(CreateInput(), CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.RequireAiAnalysis));
    }

    [Test]
    public async Task CheckContentAsync_FakeCompatibilityUser_DoesNotAffectDecision()
    {
        var policy = CreatePolicy(classifierResult: (false, -1f));
        var input = CreateInput();
        input.Message.From = new User
        {
            Id = 1087968824,
            IsBot = true,
            FirstName = "GroupAnonymousBot"
        };

        var result = await policy.CheckContentAsync(input, CancellationToken.None);

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public void CheckContentAsync_CancelledClassifier_PropagatesCancellation()
    {
        var classifier = new Mock<ISpamHamClassifier>();
        classifier
            .Setup(x => x.IsSpam(It.IsAny<string>()))
            .Returns(new TaskCompletionSource<(bool Spam, float Score)>().Task);
        var policy = new ContentModerationPolicy(
            classifier.Object,
            Mock.Of<IBadMessageManager>(),
            Mock.Of<IAppConfig>(),
            NullLogger<ContentModerationPolicy>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() =>
            policy.CheckContentAsync(CreateInput(), cts.Token));
    }

    private static ContentModerationPolicy CreatePolicy(
        (bool Spam, float Score)? classifierResult = null,
        bool lowConfidenceHamForward = false,
        bool knownBad = false,
        bool textMentionFilterEnabled = false)
    {
        var classifier = new Mock<ISpamHamClassifier>();
        classifier
            .Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync(classifierResult ?? (false, -1f));

        var appConfig = new Mock<IAppConfig>();
        appConfig.SetupGet(x => x.LowConfidenceHamForward).Returns(lowConfidenceHamForward);
        appConfig.SetupGet(x => x.TextMentionFilterEnabled).Returns(textMentionFilterEnabled);
        appConfig.Setup(x => x.IsMediaFilteringDisabledForChat(It.IsAny<long>())).Returns(false);

        var badMessages = new Mock<IBadMessageManager>();
        badMessages.Setup(x => x.KnownBadMessage(It.IsAny<string>())).Returns(knownBad);

        return new ContentModerationPolicy(
            classifier.Object,
            badMessages.Object,
            appConfig.Object,
            NullLogger<ContentModerationPolicy>.Instance);
    }

    private static ContentModerationInput CreateInput(string? text = "architecture discussion continues tomorrow")
    {
        var chat = new Chat { Id = -1009876543277, Type = ChatType.Supergroup, Title = "Chat" };
        var message = new Message
        {
            Chat = chat,
            SenderChat = new Chat { Id = -1009876543276, Type = ChatType.Channel, Title = "Channel" },
            Text = text
        };
        return new ContentModerationInput(message, chat, text);
    }
}
