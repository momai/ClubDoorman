using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Test;

[TestFixture]
public class ModerationServiceSimpleTests : TestBase
{
    // Прямое использование FakeModerationService для детерминированных сценариев
    private IModerationService _moderationService = null!;
    private FakeModerationService _fakePolicy = null!;
    private Mock<ILogger<IModerationService>> _mockLogger = null!;
    // Removed legacy _setup-dependent convenience properties after refactor to direct FakeModerationService usage

    [SetUp]
    public void SetUp()
    {
        Console.WriteLine("Setting up test (FakeModerationService + adapter)...");

        // Минимальные mocks для FakeModerationService
        var classifier = new Mock<ISpamHamClassifier>();
        var mimicry = new Mock<IMimicryClassifier>();
        var badMessage = new Mock<IBadMessageManager>();
        var userManager = new Mock<IUserManager>();
        var aiChecks = new Mock<IAiChecks>();
        var suspicious = new Mock<ISuspiciousUsersStorage>();
        var botWrapper = new Mock<ITelegramBotClientWrapper>();
        var messageService = new Mock<IMessageService>();
        var userBan = new Mock<IUserBanService>();
        var fakeLogger = new Mock<ILogger<FakeModerationService>>();

        _fakePolicy = new FakeModerationService(
            classifier.Object,
            mimicry.Object,
            badMessage.Object,
            userManager.Object,
            aiChecks.Object,
            suspicious.Object,
            botWrapper.Object,
            messageService.Object,
            userBan.Object,
            fakeLogger.Object);

        _moderationService = new ModerationServiceAdapter(_fakePolicy);
        _mockLogger = TK.CreateLoggerMock<IModerationService>();

        Console.WriteLine("Setup completed");
    }

    [Test]
    public async Task CheckUserName_WithNullUser_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckUserName_WithNullUser_ThrowsArgumentNullException");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _moderationService.CheckUserNameAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("user"));
        Console.WriteLine("Completed CheckUserName_WithNullUser_ThrowsArgumentNullException");
    }
}
