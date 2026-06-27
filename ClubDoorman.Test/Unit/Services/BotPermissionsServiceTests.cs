using ClubDoorman.Services;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:bot-permissions")]
public class BotPermissionsServiceTests : TestBase
{
    private readonly Mock<ITelegramBotClientWrapper> _mockBot;
    private readonly Mock<ILogger<BotPermissionsService>> _mockLogger;
    private readonly BotPermissionsService _service;
    private readonly Mock<IAppConfig> _mockAppConfig;
    private const long AdminChatIdConst = 123456789; // formerly Config.AdminChatId test value
    private const long LogAdminChatIdConst = 223456789; // deterministic test value

    public BotPermissionsServiceTests()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockLogger = new Mock<ILogger<BotPermissionsService>>();
    _mockAppConfig = new Mock<IAppConfig>();
    _mockAppConfig.SetupGet(x => x.AdminChatId).Returns(AdminChatIdConst);
    _mockAppConfig.SetupGet(x => x.LogAdminChatId).Returns(LogAdminChatIdConst);
    _service = new BotPermissionsService(_mockBot.Object, _mockLogger.Object, _mockAppConfig.Object);
    }

    [TestCase(ChatMemberStatus.Administrator, true)]
    [TestCase(ChatMemberStatus.Member, false)]
    [TestCase(ChatMemberStatus.Left, false)]
    [TestCase(ChatMemberStatus.Kicked, false)]
    [TestCase(ChatMemberStatus.Creator, false)]
    public async Task IsBotAdminAsync_WithDifferentStatuses_ReturnsExpectedResult(
        ChatMemberStatus status, bool expectedResult)
    {
        // Arrange
        var chatId = 1000L + (long)status; // Уникальный chatId для каждого статуса
        ChatMember chatMember = status switch
        {
            ChatMemberStatus.Administrator => new ChatMemberAdministrator(),
            ChatMemberStatus.Creator => new ChatMemberOwner(),
            _ => new ChatMemberMember()
        };

        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsBotAdminAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
        _mockBot.Verify(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IsBotAdminAsync_WhenExceptionOccurs_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var chatId = 2000L;
        var exception = new Exception("Test exception");

        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.IsBotAdminAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        // Проверяем, что было залогировано предупреждение о неудачной проверке прав администратора
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось проверить права администратора")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestCase(3000L, true)] // Обычный чат, не админ
    [TestCase(3001L, true)] // Обычный чат, не админ
    public async Task IsSilentModeAsync_ForRegularChats_ReturnsExpectedResult(long chatId, bool expectedResult)
    {
        // Arrange
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberMember();

        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task IsSilentModeAsync_ForAdminChat_ReturnsFalse()
    {
        // Arrange
    var adminChatId = AdminChatIdConst;

        // Act
    var result = await _service.IsSilentModeAsync(adminChatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_ForLogAdminChat_ReturnsFalse()
    {
        // Arrange
    var logAdminChatId = LogAdminChatIdConst;

        // Act
    var result = await _service.IsSilentModeAsync(logAdminChatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_ForPrivateChat_ReturnsFalse()
    {
        // Arrange
        var chatId = 4000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Private };

        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(chatId, It.IsAny<CancellationToken>()), Times.Once);
        // Для приватных чатов GetChatMember не должен вызываться для этого конкретного chatId
        _mockBot.Verify(x => x.GetChatMember(chatId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenChatGetFails_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var chatId = 5000L;
        var exception = new Exception("Test exception");

        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось получить информацию о чате")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenBotIsAdmin_ReturnsFalse()
    {
        // Arrange
        var chatId = 6000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberAdministrator();

        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenBotIsNotAdmin_ReturnsTrue()
    {
        // Arrange
        var chatId = 7000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberMember();

        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetBotChatMemberAsync_WhenNotCached_ReturnsFromApiAndCaches()
    {
        // Arrange
        var chatId = 8000L;
        var botId = 456L;
        var chatMember = new ChatMemberAdministrator();

        _mockBot.Setup(x => x.BotId).Returns(botId);
        _mockBot.Setup(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.GetBotChatMemberAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(chatMember));
        _mockBot.Verify(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()), Times.Once);
        // Проверяем, что было залогировано сообщение о получении информации о правах бота
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Получена информация о правах бота")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task GetBotChatMemberAsync_WhenCached_ReturnsFromCache()
    {
        // Arrange
        var chatId = 9000L;
        var botId = 456L;
        var chatMember = new ChatMemberAdministrator();

        _mockBot.Setup(x => x.BotId).Returns(botId);
        _mockBot.Setup(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act - первый вызов для кэширования
        await _service.GetBotChatMemberAsync(chatId);

        // Второй вызов должен использовать кэш
        var result = await _service.GetBotChatMemberAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(chatMember));
        _mockBot.Verify(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()), Times.Once);
    }

}