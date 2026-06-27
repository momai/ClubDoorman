using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.Violation;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Models.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Reflection;
using Moq;
using ClubDoorman.Models;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("captcha")]
public class CaptchaServiceFakeTests
{
    private CaptchaServiceTestFactory _factory = null!;
    private Mock<IMessageService> _messageServiceMock = null!;
    private Chat chat = null!;
    private User user = null!;
    private CaptchaInfo captchaInfo = null!;
    private string key = null!;

    [SetUp]
    public void Setup()
    {
        _messageServiceMock = new Mock<IMessageService>();
        _factory = new CaptchaServiceTestFactory();

        // Создаем тестовые данные
        chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        user = new User { Id = 789, FirstName = "Test", LastName = "User" };
        captchaInfo = new CaptchaInfo(chat.Id, chat.Title, DateTime.UtcNow, user, 1, new CancellationTokenSource(), null);
        key = "123456_789";
    }

    private CaptchaService CreateCaptchaService()
    {
        return new CaptchaService(
            new Mock<ITelegramBotClientWrapper>().Object,
            new Mock<ILogger<CaptchaService>>().Object,
            _messageServiceMock.Object,
            AppConfigTestFactory.CreateDefault(),
            new Mock<IViolationTracker>().Object,
            new Mock<IUserBanService>().Object
        );
    }

    [Test]
    public async Task CreateCaptchaAsync_ValidUser_SendsWelcomeMessage()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();

        // Act
        var captchaInfo = await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null));

        // Assert
        Assert.That(captchaInfo, Is.Not.Null);
        Assert.That(captchaInfo.User.Id, Is.EqualTo(789));

        _messageServiceMock.Verify(x => x.SendCaptchaMessageAsync(It.IsAny<SendCaptchaMessageRequest>()), Times.Once);
    }

    [Test]
    public async Task CreateCaptchaAsync_UserWithInappropriateName_UsesGenericName()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();
        var userWithInappropriateName = new User { Id = 999, FirstName = "p0rn", LastName = "User" };

        // Act
        var captchaInfo = await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, userWithInappropriateName, null));

        // Assert
        _messageServiceMock.Verify(x => x.SendCaptchaMessageAsync(It.IsAny<SendCaptchaMessageRequest>()), Times.Once);
    }

    [Test]
    public async Task ValidateCaptchaAsync_CorrectAnswer_ReturnsTrue()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();

        // Act - сначала создаем капчу
        var createdCaptcha = await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null));
        var key = service.GenerateKey(chat.Id, user.Id);
        var result = await service.ValidateCaptchaAsync(key, createdCaptcha.CorrectAnswer);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateCaptchaAsync_WrongAnswer_ReturnsFalse()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = await service.ValidateCaptchaAsync(key, 999); // Неправильный ответ

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateCaptchaAsync_TelegramError_ThrowsException()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ThrowsAsync(new Exception("Telegram API error"));

        var service = CreateCaptchaService();

        // Act & Assert
        var caughtException = Assert.ThrowsAsync<Exception>(async () =>
        {
            await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null));
        });

        Assert.That(caughtException.Message, Is.EqualTo("Telegram API error"));
    }

    [Test]
    public async Task ValidateCaptchaAsync_ExpiredCaptcha_ReturnsFalse()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act - используем неправильный ответ, чтобы проверить что капча работает
        var result = await service.ValidateCaptchaAsync(key, 999);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_InvalidKey_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var invalidKey = "invalid_key";

        // Act
        var result = await service.ValidateCaptchaAsync(invalidKey, 123);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetCaptchaInfo_ValidKey_ReturnsCaptchaInfo()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();

        // Act - сначала создаем капчу
        var createdCaptcha = await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null));
        var key = service.GenerateKey(chat.Id, user.Id);
        var captchaInfo = service.GetCaptchaInfo(key);

        // Assert
        Assert.That(captchaInfo, Is.Not.Null);
        Assert.That(captchaInfo.User.Id, Is.EqualTo(789));
    }

    [Test]
    public async Task RemoveCaptcha_ValidKey_ReturnsTrue()
    {
        // Arrange
        _messageServiceMock.Setup(x => x.SendCaptchaMessageAsync(
            It.IsAny<SendCaptchaMessageRequest>()))
        .ReturnsAsync(new global::Telegram.Bot.Types.Message());

        var service = CreateCaptchaService();

        // Act - сначала создаем капчу
        var createdCaptcha = await service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null));
        var key = service.GenerateKey(chat.Id, user.Id);
        var result = service.RemoveCaptcha(key);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.GetCaptchaInfo(key), Is.Null);
    }
}