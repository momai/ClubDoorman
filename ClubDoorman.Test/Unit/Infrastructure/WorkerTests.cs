using ClubDoorman.Services.UserBan;
using NUnit.Framework;
using ClubDoorman;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Test.Unit;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:worker")]
public class WorkerTests
{

    [SetUp]
    public void Setup()
    {
        // Для тестирования статических методов Worker не нужны моки
    }

    [Test]
    public void FullName_WithFirstNameAndLastName_ReturnsCombinedName()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void FullName_WithFirstNameOnly_ReturnsFirstName()
    {
        // Arrange
        var firstName = "John";
        string? lastName = null;
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void FullName_WithEmptyLastName_ReturnsFirstName()
    {
        // Arrange
        var firstName = "John";
        var lastName = "";
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void UserToKey_ReturnsCorrectFormat()
    {
        // Arrange
        var chatId = 123456789L;
        var user = new User { Id = 987654321 };
        var method = typeof(Worker).GetMethod("UserToKey", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { chatId, user });

        // Assert
        Assert.That(result, Is.EqualTo("123456789_987654321"));
    }

    [Test]
    public void AdminDisplayName_WithUsername_ReturnsUsername()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John", Username = "john_doe" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("john_doe"));
    }

    [Test]
    public void AdminDisplayName_WithoutUsername_ReturnsFullName()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John", LastName = "Doe" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void AdminDisplayName_WithFirstNameOnly_ReturnsFirstName()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Worker_UsesRegisteredGlobalStatsManager_SharingStateWithOtherConsumers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<Worker>>(NullLogger<Worker>.Instance);
        services.AddSingleton(Mock.Of<IUpdateDispatcher>());
        services.AddSingleton(Mock.Of<ICaptchaService>());
        services.AddSingleton(Mock.Of<ISpamHamClassifier>());
        services.AddSingleton(Mock.Of<IUserManager>());
        services.AddSingleton(Mock.Of<IBadMessageManager>());
        services.AddSingleton(Mock.Of<IAiChecks>());
        services.AddSingleton(Mock.Of<IChatLinkFormatter>());
        services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        services.AddSingleton(Mock.Of<IMessageService>());
        services.AddSingleton(Mock.Of<IAppConfig>());
        services.AddSingleton(Mock.Of<IUserBanService>());
        services.AddStatisticsServices();
        services.AddSingleton(Mock.Of<IStatisticsService>());
        services.AddSingleton<Worker>();

        using var provider = services.BuildServiceProvider();
        var worker = provider.GetRequiredService<Worker>();
        var registeredStatsManager = provider.GetRequiredService<GlobalStatsManager>();
        var workerStatsManager = (GlobalStatsManager)typeof(Worker)
            .GetField("_globalStatsManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(worker)!;

        Assert.That(services.Count(d => d.ServiceType == typeof(GlobalStatsManager)), Is.EqualTo(1));
        Assert.That(workerStatsManager, Is.SameAs(registeredStatsManager));

        var chatId = DateTime.UtcNow.Ticks;
        registeredStatsManager.IncCaptcha(chatId, "DI test chat");
        var stats = (StatsRoot)typeof(GlobalStatsManager)
            .GetField("_stats", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(workerStatsManager)!;

        Assert.That(stats.Chats[chatId].CaptchaShown, Is.EqualTo(1));
    }

  }
