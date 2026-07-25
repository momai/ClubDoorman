using ClubDoorman.Effects.Moderation;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects.Moderation;

[TestFixture]
public class ModerationActionHandlersTests
{
    private User _user;
    private Chat _chat;
    private Message _message;

    [SetUp]
    public void SetUp()
    {
        _user = new User { Id = 456, Username = "testuser" };
        _chat = new Chat { Id = 789, Title = "Test Chat" };
        _message = new Message
        {
            Text = "test message",
            From = _user,
            Chat = _chat
        };
    }

    [TestCase("Ссылки запрещены")]
    [TestCase("Банальное приветствие")]
    public async Task Delete_SpecialReason_DeletesToLogThenTracksViolation(string reason)
    {
        var calls = new List<string>();
        var notifications = new Mock<INotificationService>();
        var userBan = new Mock<IUserBanService>();
        notifications
            .Setup(service => service.DeleteAndReportToLogChat(_message, reason, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("delete"))
            .Returns(Task.CompletedTask);
        userBan
            .Setup(service => service.TrackViolationAndBanIfNeededAsync(_message, _user, reason, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("track"))
            .Returns(Task.CompletedTask);
        var handler = new DeleteActionHandler(
            notifications.Object,
            userBan.Object,
            NullLogger<DeleteActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Delete, reason), CancellationToken.None);

        Assert.That(calls, Is.EqualTo(new[] { "delete", "track" }));
        notifications.Verify(
            service => service.DeleteAndReportMessage(
                It.IsAny<Message>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Delete_GenericReason_PassesSilentModeThenTracksViolation()
    {
        const string reason = "Спам сообщение";
        var calls = new List<string>();
        var notifications = new Mock<INotificationService>();
        var userBan = new Mock<IUserBanService>();
        notifications
            .Setup(service => service.DeleteAndReportMessage(_message, reason, true, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("delete"))
            .Returns(Task.CompletedTask);
        userBan
            .Setup(service => service.TrackViolationAndBanIfNeededAsync(_message, _user, reason, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("track"))
            .Returns(Task.CompletedTask);
        var handler = new DeleteActionHandler(
            notifications.Object,
            userBan.Object,
            NullLogger<DeleteActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Delete, reason, isSilentMode: true), CancellationToken.None);

        Assert.That(calls, Is.EqualTo(new[] { "delete", "track" }));
    }

    [Test]
    public void Delete_WhenDeletionFails_DoesNotTrackViolation()
    {
        var notifications = new Mock<INotificationService>();
        var userBan = new Mock<IUserBanService>();
        notifications
            .Setup(service => service.DeleteAndReportMessage(
                _message,
                "reason",
                false,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));
        var handler = new DeleteActionHandler(
            notifications.Object,
            userBan.Object,
            NullLogger<DeleteActionHandler>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.ExecuteAsync(CreateContext(ModerationAction.Delete), CancellationToken.None));
        userBan.Verify(
            service => service.TrackViolationAndBanIfNeededAsync(
                It.IsAny<Message>(),
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Report_PassesFacadeUserSilentModeAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var notifications = new Mock<INotificationService>();
        var handler = new ReportActionHandler(
            notifications.Object,
            NullLogger<ReportActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Report, isSilentMode: true), cancellation.Token);

        notifications.Verify(
            service => service.DontDeleteButReportMessage(_message, _user, true, cancellation.Token),
            Times.Once);
    }

    [Test]
    public async Task Ban_LogsFlowBeforeAutoBan()
    {
        var calls = new List<string>();
        var userBan = new Mock<IUserBanService>();
        var userFlow = new Mock<IUserFlowLogger>();
        userFlow
            .Setup(logger => logger.LogUserBanned(_user, _chat, "reason"))
            .Callback(() => calls.Add("flow"));
        userBan
            .Setup(service => service.AutoBanAsync(_message, "reason", It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("ban"))
            .Returns(Task.CompletedTask);
        var handler = new BanActionHandler(
            userBan.Object,
            userFlow.Object,
            NullLogger<BanActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Ban), CancellationToken.None);

        Assert.That(calls, Is.EqualTo(new[] { "flow", "ban" }));
    }

    [Test]
    public async Task Allow_WhenAiDetectPasses_UsesCaptionForGoodMessage()
    {
        var policy = new Mock<IModerationPolicy>();
        _message.Text = null;
        _message.Caption = "caption";
        policy
            .Setup(service => service.CheckAiDetectAndNotifyAdminsAsync(_user, _chat, _message))
            .ReturnsAsync(false);
        var handler = new AllowActionHandler(
            policy.Object,
            NullLogger<AllowActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Allow), CancellationToken.None);

        policy.Verify(
            service => service.IncrementGoodMessageCountAsync(_user, _chat, "caption"),
            Times.Once);
    }

    [Test]
    public async Task Allow_WhenAiDetectBlocks_DoesNotIncrementGoodMessageCount()
    {
        var policy = new Mock<IModerationPolicy>();
        policy
            .Setup(service => service.CheckAiDetectAndNotifyAdminsAsync(_user, _chat, _message))
            .ReturnsAsync(true);
        var handler = new AllowActionHandler(
            policy.Object,
            NullLogger<AllowActionHandler>.Instance);

        await handler.ExecuteAsync(CreateContext(ModerationAction.Allow), CancellationToken.None);

        policy.Verify(
            service => service.IncrementGoodMessageCountAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task ManualReview_PassesSilentMode()
    {
        var notifications = new Mock<INotificationService>();
        var handler = new ManualReviewActionHandler(
            notifications.Object,
            NullLogger<ManualReviewActionHandler>.Instance);

        await handler.ExecuteAsync(
            CreateContext(ModerationAction.RequireManualReview, isSilentMode: true),
            CancellationToken.None);

        notifications.Verify(
            service => service.DontDeleteButReportMessage(_message, _user, true, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task AiAnalysis_WhenConfidenceIsMissing_PassesZeroAndSilentMode()
    {
        var aiCascade = new Mock<IAiCascadeService>();
        var handler = new AiAnalysisActionHandler(
            aiCascade.Object,
            NullLogger<AiAnalysisActionHandler>.Instance);

        await handler.ExecuteAsync(
            CreateContext(ModerationAction.RequireAiAnalysis, confidence: null, isSilentMode: true),
            CancellationToken.None);

        aiCascade.Verify(
            service => service.HandleAiCascadeAnalysisAsync(
                _message,
                _user,
                0,
                true,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task AiAnalysis_WhenConfidenceIsPresent_PassesScoreUnchanged()
    {
        const double confidence = 0.73;
        var aiCascade = new Mock<IAiCascadeService>();
        var handler = new AiAnalysisActionHandler(
            aiCascade.Object,
            NullLogger<AiAnalysisActionHandler>.Instance);

        await handler.ExecuteAsync(
            CreateContext(ModerationAction.RequireAiAnalysis, confidence: confidence),
            CancellationToken.None);

        aiCascade.Verify(
            service => service.HandleAiCascadeAnalysisAsync(
                _message,
                _user,
                confidence,
                false,
                CancellationToken.None),
            Times.Once);
    }

    private ModerationActionContext CreateContext(
        ModerationAction action,
        string reason = "reason",
        double? confidence = null,
        bool isSilentMode = false)
    {
        return new ModerationActionContext(
            _message,
            _user,
            _chat,
            new ModerationResult(action, reason, confidence),
            isSilentMode);
    }
}
