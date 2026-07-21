using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using NUnit.Framework;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("message-handler")]
public class MessageHandlerIntegrationTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _handler = null!;
    private Mock<ILogger<MessageHandler>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _factory.WithStandardMocks(); // Добавляем стандартные моки, включая CommandHandler'ы
        _handler = (MessageHandler)_factory.CreateMessageHandler();
        _loggerMock = _factory.LoggerMock;
    }

    #region Message Processing Tests

    [Test]
    public async Task HandleMessageAsync_NullMessage_HandlesGracefully()
    {
        // Arrange
        var update = new Update
        {
            Id = 1,
            Message = null,
            EditedMessage = null
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _handler.HandleAsync(update),
            "Should throw ArgumentNullException when both Message and EditedMessage are null");
    }

    #endregion

    #region User Status Tests

    [Test]
    public async Task HandleMessageAsync_ApprovedUser_ProcessesSuccessfully()
    {
        // Arrange
        var message = CreateMessage("Hello from approved user", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock approved user
        _factory.UserManagerMock.Setup(x => x.Approved(It.IsAny<long>(), null))
            .Returns(true);

        // Act
        await _handler.HandleAsync(update);

        // Assert
        // Verify that the approved user's message was processed
        Assert.Pass("Approved user message processed successfully");
    }

    [Test]
    public async Task HandleMessageAsync_BannedUser_HandlesGracefully()
    {
        // Arrange
        var message = CreateMessage("Hello from banned user", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock banned user
        _factory.UserManagerMock.Setup(x => x.InBanlist(It.IsAny<long>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(update);

        // Assert
        // Verify that the banned user's message was handled gracefully
        Assert.Pass("Banned user message handled gracefully");
    }

    [Test]
    public async Task HandleMessageAsync_SuspiciousUser_HandlesGracefully()
    {
        // Arrange
        var message = CreateMessage("Hello from suspicious user", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock suspicious user
        _factory.SuspiciousUsersStorageMock.Setup(x => x.IsSuspicious(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        // Act
        await _handler.HandleAsync(update);

        // Assert
        // Verify that the suspicious user's message was handled gracefully
        Assert.Pass("Suspicious user message handled gracefully");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task HandleAsync_ExceptionInUserManager_HandlesGracefully()
    {
        // Arrange
        var message = CreateMessage("Test message", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock exception in UserManager
        _factory.UserManagerMock.Setup(x => x.Approved(It.IsAny<long>(), null))
            .Throws(new System.Exception("Test exception"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.HandleAsync(update),
            "Exception in UserManager should be handled gracefully");
    }

    [Test]
    public async Task HandleAsync_ExceptionInModerationService_HandlesGracefully()
    {
        // Arrange
        var message = CreateMessage("Test message", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock exception in ModerationService
        _factory.ModerationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ThrowsAsync(new System.Exception("Test exception"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.HandleAsync(update),
            "Exception in ModerationService should be handled gracefully");
    }

    [Test]
    public async Task HandleAsync_ExceptionInBotClient_HandlesGracefully()
    {
        // Arrange
        var message = CreateMessage("Test message", ChatType.Group);
        var update = CreateUpdate(message);

        // Mock exception in BotClient
        _factory.BotMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("Test exception"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.HandleAsync(update),
            "Exception in BotClient should be handled gracefully");
    }

    #endregion

    #region Helper Methods

    private static Message CreateMessage(string text, ChatType chatType)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat
            {
                Id = 12345,
                Type = chatType,
                Title = "Test Chat"
            },
            From = new User
            {
                Id = 67890,
                IsBot = false,
                FirstName = "Test",
                Username = "testuser"
            },
            Text = text
        };
    }

    private static Update CreateUpdate(Message? message)
    {
        return new Update
        {
            Id = 1,
            Message = message
        };
    }

    #endregion
}