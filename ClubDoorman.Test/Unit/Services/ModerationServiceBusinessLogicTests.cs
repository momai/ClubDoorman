using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Infrastructure;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты бизнес-логики ModerationService
/// Проверяют реальную логику модерации
/// </summary>
[TestFixture]
[Category("business-logic")]
[Category("moderation")]
public class ModerationServiceBusinessLogicTests
{
    private ModerationServiceAdapter _service;
    private FakeTelegramClient _fakeClient;
    private ModerationServiceTestFactory _factory;

    [SetUp]
    public void Setup()
    {
        _fakeClient = new FakeTelegramClient();
        _factory = new ModerationServiceTestFactory();
        _service = _factory.CreateModerationService();
    }

    #region Тесты проверки сообщений

    [Test]
    public async Task CheckMessageAsync_UserInBanlist_ReturnsBanAction()
    {
        // Arrange - используем новые возможности TestKit
        var userId = 123456L;
        var message = TestKitBuilders.CreateMessage()
            .FromUser(userId)
            .WithText("Hello world")
            .Build();

        _factory.WithUserManagerSetup(mock =>
            mock.Setup(x => x.InBanlist(userId)).ReturnsAsync(true));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("блэклисте"));
    }

    [Test]
    public async Task CheckMessageAsync_MessageWithButtons_ReturnsBanAction()
    {
        // Arrange - используем новые возможности TestKit
        var message = TestKitBuilders.CreateMessage()
            .FromUser(123456L)
            .WithText("Hello")
            .Build();

        // Добавляем кнопки (пока нет билдера для этого)
        message.ReplyMarkup = new InlineKeyboardMarkup(new[]
        {
            new[] { new InlineKeyboardButton("Button 1") { CallbackData = "test" } }
        });

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("кнопками"));
    }

    [Test]
    public async Task CheckMessageAsync_StoryMessage_ReturnsDeleteAction()
    {
        // Arrange - используем новые возможности TestKit
        var message = TestKitBuilders.CreateMessage()
            .FromUser(123456L)
            .WithText("Hello")
            .Build();

        // Добавляем Story (пока нет билдера для этого)
        message.Story = new Story { Id = 1 };

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Does.Contain("Сторис"));
    }

    [Test]
    public async Task CheckMessageAsync_SpamDetected_ReturnsBanAction()
    {
        // Arrange - используем новые возможности TestKit
        var message = TestKitBuilders.CreateMessage()
            .FromUser(123456L)
            .WithText("SPAM MESSAGE")
            .Build();

        _factory.WithClassifierSetup(mock =>
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((true, 0.9f)));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Does.Contain("спам"));
    }

    [Test]
    public async Task CheckMessageAsync_GoodMessage_ReturnsAllowAction()
    {
        // Arrange - используем новые возможности TestKit
        var message = TestKitBuilders.CreateMessage()
            .FromUser(123456L)
            .WithText("Это нормальное сообщение с полезной информацией")
            .Build();

        _factory.WithClassifierSetup(mock =>
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)

        _factory.WithMimicryClassifierSetup(mock =>
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    #endregion

    #region Тесты проверки пользователей

    [Test]
    public async Task CheckUserNameAsync_EmptyFirstName_ThrowsException()
    {
        // Arrange - используем новые возможности TestKit
        var user = TestKitBuilders.CreateUser()
            .WithId(123456L)
            .WithUsername("john_doe")
            .Build();

        // Устанавливаем FirstName в null
        user.FirstName = null;

        // Act & Assert
        var ex = Assert.ThrowsAsync<ModerationException>(async () =>
            await _service.CheckUserNameAsync(user));

        Assert.That(ex.Message, Does.Contain("пустым"));
    }

    #endregion

    #region Тесты управления пользователями

    [Test]
    public async Task BanAndCleanupUserAsync_ValidUser_ReturnsTrue()
    {
        // Arrange
        var userId = 123456L;
        var chatId = 789L;

        _factory.WithBotClientSetup(mock =>
            mock.Setup(x => x.SendRequest(It.IsAny<global::Telegram.Bot.Requests.BanChatMemberRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true));

        // Act
        var result = await _service.BanAndCleanupUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UnrestrictAndApproveUserAsync_ValidUser_ReturnsTrue()
    {
        // Arrange
        var userId = 123456L;
        var chatId = 789L;

        _factory.WithBotClientSetup(mock =>
            mock.Setup(x => x.SendRequest(It.IsAny<global::Telegram.Bot.Requests.RestrictChatMemberRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true));

        // Act
        var result = await _service.UnrestrictAndApproveUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region Тесты исключений

    [Test]
    public void CheckMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.CheckMessageAsync(null!));

        Assert.That(ex.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void CheckMessageAsync_MessageWithoutUser_ThrowsModerationException()
    {
        // Arrange
        var message = new Message { Text = "Hello", Chat = new Chat { Id = 123 } };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ModerationException>(async () =>
            await _service.CheckMessageAsync(message));

        Assert.That(ex.Message, Does.Contain("пользователе"));
    }

    #endregion
}
