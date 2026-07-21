using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Telegram.Bot.Types;
using Moq;
using System;

namespace ClubDoorman.Test.Unit.Moderation;

[TestFixture]
[Category("unit")]
[Category("moderation")]
[Category("critical")]
public class ModerationServiceTests
{
    private ModerationServiceTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationServiceTestFactory();
    }

    [Test]
    public async Task CheckMessageAsync_ValidMessage_ReturnsAllow()
    {
        // Arrange
        _factory.WithClassifierSetup(mock =>
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)

        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello, this is a valid message!",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.Not.Empty);
    }

    [Test]
    public async Task CheckMessageAsync_SpamMessage_ReturnsDelete()
    {
        // Arrange
        _factory.WithClassifierSetup(mock =>
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((true, 0.9f)));
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "BUY NOW!!! AMAZING OFFER!!!",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Does.Contain("спам"));
    }

    [Test]
    public async Task CheckMessageAsync_BannedUser_ReturnsBan()
    {
        // Arrange
        _factory.WithUserManagerSetup(mock =>
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(true));
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("блэклист"));
    }

    [Test]
    public async Task CheckMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateModerationService();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CheckMessageAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public async Task CheckMessageAsync_EmptyMessage_ReturnsReport()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
    }

    [Test]
    public async Task CheckMessageAsync_ClassifierException_ThrowsException()
    {
        // Arrange
        _factory.WithClassifierSetup(mock =>
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Classifier error")));
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Test message",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await service.CheckMessageAsync(message));

        Assert.That(exception.Message, Is.EqualTo("Classifier error"));
    }

}
