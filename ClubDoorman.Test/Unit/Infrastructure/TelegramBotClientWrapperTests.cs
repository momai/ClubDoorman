using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Test.Unit.Infrastructure;

[TestFixture]
[Category("unit")]
[Category("infrastructure")]
public class TelegramBotClientWrapperTests
{
    private TelegramBotClientWrapperTestFactory _factory;
    private TelegramBotClientWrapper _wrapper;

    [SetUp]
    public void Setup()
    {
        _factory = new TelegramBotClientWrapperTestFactory();
        // Создаем реальный TelegramBotClient для wrapper'а
    var realBotClient = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz");
    _wrapper = new TelegramBotClientWrapper(realBotClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramBotClientWrapper>.Instance);
    }

    [Test]
    public void TelegramBotClientWrapper_Constructor_ThrowsOnNullBot()
    {
        // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new TelegramBotClientWrapper(null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramBotClientWrapper>.Instance));
    }

    [Test]
    public void TelegramBotClientWrapper_BotId_ReturnsCorrectId()
    {
        // Arrange
        var botClient = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz");
    var wrapper = new TelegramBotClientWrapper(botClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramBotClientWrapper>.Instance);

        // Act
        var botId = wrapper.BotId;

        // Assert
        Assert.That(botId, Is.EqualTo(1234567890));
    }

}
