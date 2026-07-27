using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Features.Moderation;

[TestFixture]
public class ModerationPolicyContentDelegationTests
{
    [Test]
    public async Task CheckMessageAsync_BanlistedUser_DoesNotRunContentPolicy()
    {
        var contentPolicy = new Mock<IContentModerationPolicy>();
        var policy = CreatePolicy(contentPolicy, inBanlist: true);

        var result = await policy.CheckMessageAsync(CreateMessage());

        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        contentPolicy.Verify(
            x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CheckMessageAsync_NonBanlistedUser_DelegatesContentDecision()
    {
        var expected = new ModerationResult(ModerationAction.Report, "content decision");
        var contentPolicy = new Mock<IContentModerationPolicy>();
        contentPolicy
            .Setup(x => x.CheckContentAsync(It.IsAny<ContentModerationInput>(), CancellationToken.None))
            .ReturnsAsync(expected);
        var policy = CreatePolicy(contentPolicy, inBanlist: false);
        var message = CreateMessage();

        var result = await policy.CheckMessageAsync(message);

        Assert.That(result, Is.SameAs(expected));
        contentPolicy.Verify(
            x => x.CheckContentAsync(
                It.Is<ContentModerationInput>(input =>
                    input.Message == message &&
                    input.DestinationChat == message.Chat &&
                    input.Text == message.Text),
                CancellationToken.None),
            Times.Once);
    }

    private static ModerationPolicy CreatePolicy(
        Mock<IContentModerationPolicy> contentPolicy,
        bool inBanlist)
    {
        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(inBanlist);

        return new ModerationPolicy(
            Mock.Of<ISpamHamClassifier>(),
            Mock.Of<IMimicryClassifier>(),
            contentPolicy.Object,
            userManager.Object,
            Mock.Of<IAiChecks>(),
            Mock.Of<ISuspiciousUsersStorage>(),
            Mock.Of<ITelegramBotClient>(),
            Mock.Of<IMessageService>(),
            Mock.Of<IUserBanService>(),
            Mock.Of<IUserCleanupService>(),
            NullLogger<ModerationPolicy>.Instance,
            Mock.Of<IAppConfig>());
    }

    private static Message CreateMessage() => new()
    {
        From = new User { Id = 42, FirstName = "User" },
        Chat = new Chat { Id = -1009876543277, Type = ChatType.Supergroup, Title = "Chat" },
        Text = "architecture discussion continues tomorrow"
    };
}
