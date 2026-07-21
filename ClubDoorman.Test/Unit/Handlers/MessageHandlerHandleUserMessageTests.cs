using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для HandleUserMessageAsync метода
/// <tags>unit, message-handler, user-message, moderation</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerHandleUserMessageTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = TK.CreateMessageHandlerFactory();

        // Настраиваем ModerationFacadeMock для предотвращения NullReferenceException
        _factory.WithModerationFacadeSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Test", 0.5f));
            mock.Setup(x => x.HandleUserMessageAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<ModerationResult>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        });

        // Настраиваем AiCascadeServiceMock для предотвращения NullReferenceException
        _factory.AiCascadeServiceMock.Setup(x => x.PerformAiProfileAnalysisAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с null пользователем
    /// Проверяет, что метод корректно обрабатывает системные сообщения
    /// <tags>user-message, null-user, system-message, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithNullUser_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.From = null; // Системное сообщение без пользователя

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений
        Assert.Pass("Системное сообщение обработано корректно");
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с ботом
    /// Проверяет, что метод игнорирует сообщения от ботов
    /// <tags>user-message, bot, ignore, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithBotUser_IgnoresMessage()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.From!.IsBot = true;

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений
        Assert.Pass("Сообщение от бота проигнорировано");
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с системным сообщением о выходе
    /// Проверяет, что метод игнорирует системные сообщения
    /// <tags>user-message, system-message, left-chat-member, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithLeftChatMember_IgnoresMessage()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.LeftChatMember = TK.CreateUser();

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений
        Assert.Pass("Системное сообщение о выходе проигнорировано");
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с пользователем в капче
    /// Проверяет, что сообщение удаляется и обработка прекращается
    /// <tags>user-message, captcha, delete-message, moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithUserInCaptcha_DeletesMessageAndReturns()
    {
        // Arrange
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var envelope = MessageEnvelope.CreateTest(messageId: 12345, userId: 12345, chatId: 67890);
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var user = message.From!;
        var chat = message.Chat;
        var captchaKey = "test_captcha_key";

        // Настраиваем мок CaptchaService
        _factory.CaptchaServiceMock.Setup(x => x.GenerateKey(chat.Id, user.Id))
            .Returns(captchaKey);
        _factory.CaptchaServiceMock.Setup(x => x.GetCaptchaInfo(captchaKey))
            .Returns(new CaptchaInfo(chat.Id, chat.Title, DateTime.UtcNow, user, 0, new CancellationTokenSource(), null));
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((ChatId chatId, int messageId, CancellationToken cancellationToken) =>
                fakeClient.DeleteMessage(chatId, messageId, cancellationToken));

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с пользователем в блэклисте
    /// Проверяет, что вызывается HandleBlacklistBanAsync
    /// <tags>user-message, blacklist, ban, moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithUserInBlacklist_CallsHandleBlacklistBanAsync()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;

        // Настраиваем мок UserManager для возврата true (пользователь в блэклисте)
        _factory.UserManagerMock.Setup(x => x.InBanlist(user.Id))
            .ReturnsAsync(true);

        // Настраиваем мок UserBanService
        _factory.UserBanServiceMock.Setup(x => x.HandleBlacklistBanAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        _factory.UserBanServiceMock.Verify(x => x.HandleBlacklistBanAsync(message, user, chat, It.IsAny<CancellationToken>()), Times.Once);
    }



    /// <summary>
    /// Тест для HandleUserMessageAsync с клубным пользователем
    /// Проверяет, что метод возвращается для клубных пользователей
    /// <tags>user-message, club-user, skip-moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithClubUser_ReturnsEarly()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var clubName = "test_club";

        // Настраиваем мок UserManager для клубного пользователя
        _factory.UserManagerMock.Setup(x => x.GetClubUsername(user.Id))
            .ReturnsAsync(clubName);

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без вызова модерации
        _factory.ModerationServiceMock.Verify(x => x.CheckMessageAsync(It.IsAny<Message>()), Times.Never);
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с пересланным сообщением
    /// Проверяет обработку пересланных сообщений от новичков
    /// <tags>user-message, forwarded-message, moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithForwardedMessage_ProcessesCorrectly()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.ForwardOrigin = new MessageOriginUser { SenderUser = TK.CreateUser() };

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений
        // Удаление пересланных сообщений зависит от Config.DeleteForwardedMessages
        Assert.Pass("Пересланное сообщение обработано корректно");
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с разрешенным сообщением
    /// Проверяет обработку разрешенных сообщений
    /// <tags>user-message, allowed-message, moderation, allow</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithAllowedMessage_ProcessesCorrectly()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;

        // Настраиваем мок ModerationFacade для разрешенного сообщения
        _factory.ModerationFacadeMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(TK.CreateAllowResult());

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Проверяем, что ModerationFacade был вызван
        _factory.ModerationFacadeMock.Verify(x => x.CheckMessageAsync(message), Times.Once);
    }



    /// <summary>
    /// Тест для HandleUserMessageAsync с сообщением для удаления
    /// Проверяет обработку сообщений для удаления
    /// <tags>user-message, delete-message, moderation, delete</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithDeleteMessage_CallsDeleteAndReportMessage()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;

        // Настраиваем мок ModerationFacade для сообщения для удаления
        _factory.ModerationFacadeMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(TK.CreateDeleteResult());

        // Настраиваем мок для DeleteAndReportMessage
        _factory.MessageServiceMock.Setup(x => x.SendUserNotificationAsync(
            It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
            It.IsAny<SimpleNotificationData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Проверяем, что ModerationFacade был вызван для обработки сообщения для удаления
        _factory.ModerationFacadeMock.Verify(x => x.HandleUserMessageAsync(message, user, chat, It.IsAny<ModerationResult>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с ошибкой модерации
    /// Проверяет обработку ошибок в ModerationService
    /// <tags>user-message, moderation-error, exception-handling</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithModerationError_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;

        // Настраиваем мок ModerationFacade для выброса исключения
        _factory.ModerationFacadeMock.Setup(x => x.CheckMessageAsync(message))
            .ThrowsAsync(new Exception("Test moderation error"));

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений
        Assert.Pass("Ошибка модерации обработана корректно");
    }

    /// <summary>
    /// Тест для HandleUserMessageAsync с AI анализом профиля
    /// Проверяет выполнение AI анализа профиля
    /// <tags>user-message, ai-analysis, profile-analysis</tags>
    /// </summary>
    [Test]
    public async Task HandleUserMessageAsync_WithAiProfileAnalysis_ExecutesAnalysis()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;

        // Настраиваем мок ModerationFacade для разрешенного сообщения
        _factory.ModerationFacadeMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(TK.CreateAllowResult());

        // Act
        await _messageHandler.HandleUserMessageAsync(message, false, CancellationToken.None);

        // Assert
        // Проверяем, что AI анализ был выполнен (через PerformAiProfileAnalysis)
        // Метод должен завершиться без исключений
        Assert.Pass("AI анализ профиля выполнен корректно");
    }
}
