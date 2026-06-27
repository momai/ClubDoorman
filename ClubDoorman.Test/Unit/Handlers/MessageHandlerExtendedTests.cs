using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;

using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestInfrastructure;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("extended")]
public class MessageHandlerExtendedTests
{
    private MessageHandlerTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();

        // Настройка базовых моков для предотвращения NullReferenceException
        _factory.WithModerationFacadeMock(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ClubDoorman.Models.ModerationResult(ClubDoorman.Models.ModerationAction.Allow, "Test"));
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ClubDoorman.Models.ModerationResult(ClubDoorman.Models.ModerationAction.Allow, "Test name"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });

        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(true);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
        });

        _factory.WithServiceProviderSetup(mock =>
        {
            // Создаем реальные экземпляры командных обработчиков для тестов
            var startCommandHandler = new StartCommandHandler(
                new TelegramBotClientWrapper(new Telegram.Bot.TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramBotClientWrapper>.Instance),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<StartCommandHandler>.Instance,
                new Moq.Mock<IMessageService>().Object,
                AppConfigTestFactory.CreateDefault()
            );

            var suspiciousCommandHandler = new SuspiciousCommandHandler(
                new TelegramBotClientWrapper(new Telegram.Bot.TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramBotClientWrapper>.Instance),
                _factory.ModerationFacadeMock.Object,
                new Moq.Mock<IMessageService>().Object,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SuspiciousCommandHandler>.Instance,
                AppConfigTestFactory.CreateDefault()
            );

            // Возвращаем реальные экземпляры для командных обработчиков через GetService
            mock.Setup(x => x.GetService(typeof(StartCommandHandler)))
                .Returns(startCommandHandler);

            mock.Setup(x => x.GetService(typeof(SuspiciousCommandHandler)))
                .Returns(suspiciousCommandHandler);

            // Для остальных сервисов возвращаем null, но только если это не командные обработчики
            mock.Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(serviceType =>
                {
                    if (serviceType == typeof(StartCommandHandler))
                        return startCommandHandler;
                    if (serviceType == typeof(SuspiciousCommandHandler))
                        return suspiciousCommandHandler;
                    return null;
                });
        });
    }

    #region Helper Methods

    private static Message CreateTestMessage(string? text = null, User? from = null, Chat? chat = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = chat ?? new Chat { Id = 123456, Type = ChatType.Group },
            From = from ?? new User { Id = 789, FirstName = "Test" },
            Text = text
        };
    }

    private static Message CreateTestMessageWithReply(string? text = null, Message? replyToMessage = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup },
            From = new User { Id = 789, FirstName = "Test" },
            Text = text,
            ReplyToMessage = replyToMessage
        };
    }

    private static Message CreateTestMessageWithNewMembers(User[] newMembers)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            From = new User { Id = 789, FirstName = "Test" },
            NewChatMembers = newMembers
        };
    }

    private static Message CreateTestMessageWithLeftMember(User leftMember, User? from = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            From = from ?? new User { Id = 789, FirstName = "Test" },
            LeftChatMember = leftMember
        };
    }

    private static Message CreateTestMessageWithSenderChat(Chat senderChat, string? text = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            SenderChat = senderChat,
            Text = text
        };
    }

    #endregion

    #region CanHandle Tests

    [Test]
    public void CanHandle_ValidMessage_ReturnsTrue()
    {
        // Arrange
        var message = CreateTestMessage("Hello world");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_EditedMessage_ReturnsTrue()
    {
        // Arrange
        var editedMessage = CreateTestMessage("Edited message");
        editedMessage.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = editedMessage };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_NullUpdate_ReturnsFalse()
    {
        // Arrange
        Update? update = null;
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandle_UpdateWithoutMessage_ReturnsFalse()
    {
        // Arrange
        var update = new Update { Message = null, EditedMessage = null };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandle_UpdateWithCallbackQuery_ReturnsFalse()
    {
        // Arrange
        var update = new Update
        {
            Message = null,
            EditedMessage = null,
            CallbackQuery = new CallbackQuery { Id = "test" }
        };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region HandleAsync - Basic Tests

    [Test]
    public async Task HandleAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Arrange
        Update? update = null;
        var handler = _factory.CreateMessageHandler();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await handler.HandleAsync(update));

        Assert.That(exception.ParamName, Is.EqualTo("update"));
    }

    #endregion

    #region HandleAsync - Command Tests

    #endregion

    #region HandleAsync - New Members Tests

    [Test]
    public async Task HandleAsync_NewChatMembers_HandlesSuccessfully()
    {
        // Arrange
        var newMembers = new[] { new User { Id = 999, FirstName = "NewUser", Username = "newuser" } };
        var message = CreateTestMessageWithNewMembers(newMembers);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MultipleNewChatMembers_HandlesSuccessfully()
    {
        // Arrange
        var newMembers = new[]
        {
            new User { Id = 999, FirstName = "User1", Username = "user1" },
            new User { Id = 1000, FirstName = "User2", Username = "user2" }
        };
        var message = CreateTestMessageWithNewMembers(newMembers);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_NewChatMembersInAdminChat_HandlesSuccessfully()
    {
        // Arrange
        var adminChat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup };
        var newMembers = new[] { new User { Id = 999, FirstName = "NewUser", Username = "newuser" } };
        var message = CreateTestMessageWithNewMembers(newMembers);
        message.Chat = adminChat;
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - Left Chat Member Tests

    [Test]
    public async Task HandleAsync_LeftChatMemberFromBot_HandlesSuccessfully()
    {
        // Arrange
        var leftMember = new User { Id = 999, FirstName = "LeftUser" };
        var botUser = new User { Id = 123456789, FirstName = "Bot" };
        var message = CreateTestMessageWithLeftMember(leftMember, botUser);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_LeftChatMemberFromUser_HandlesSuccessfully()
    {
        // Arrange
        var leftMember = new User { Id = 999, FirstName = "LeftUser" };
        var message = CreateTestMessageWithLeftMember(leftMember);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - Channel Message Tests

    [Test]
    public async Task HandleAsync_ChannelMessage_HandlesSuccessfully()
    {
        // Arrange
        var senderChat = new Chat { Id = -100987654321, Type = ChatType.Channel, Title = "Test Channel" };
        var message = CreateTestMessageWithSenderChat(senderChat, "Channel message");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task HandleAsync_MessageWithoutFrom_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("Message without from", null);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

   #endregion

    #region Integration Tests

     [Test]
    public async Task HandleAsync_CancellationToken_RespectsCancellation()
    {
        // Arrange
        var message = CreateTestMessage("Test message");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await handler.HandleAsync(update, cts.Token);
        // Should not throw if cancellation is handled properly
        Assert.Pass();
    }

    #endregion

 }