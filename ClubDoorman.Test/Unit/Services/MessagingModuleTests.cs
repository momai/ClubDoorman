using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Options;
using Moq;

namespace ClubDoorman.Test.Unit.Services.Messaging;

/// <summary>
/// Тесты для MessagingModule
/// <tags>unit, messaging-module, di-registration</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("messaging-module")]
public class MessagingModuleTests
{
    private IServiceCollection _services = null!;

    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();

        // Добавляем необходимые зависимости для тестирования
        _services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        _services.AddSingleton(Mock.Of<IUpdateHandler>());
        _services.AddSingleton(Mock.Of<IOptions<LoggingConfiguration>>());
        _services.AddSingleton(Mock.Of<IAppConfig>());
        _services.AddSingleton(Mock.Of<IUserBanService>());

        // Добавляем логгеры
        _services.AddLogging();
    }

    /// <summary>
    /// POC: Проверка регистрации MessageTemplates
    /// <tags>poc, message-templates, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterMessageTemplates()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var messageTemplates = serviceProvider.GetService<MessageTemplates>();
        Assert.That(messageTemplates, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации ILoggingConfigurationService
    /// <tags>poc, logging-configuration, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterILoggingConfigurationService()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var loggingConfigService = serviceProvider.GetService<ILoggingConfigurationService>();
        Assert.That(loggingConfigService, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации IServiceChatDispatcher
    /// <tags>poc, service-chat-dispatcher, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterIServiceChatDispatcher()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var serviceChatDispatcher = serviceProvider.GetService<IServiceChatDispatcher>();
        Assert.That(serviceChatDispatcher, Is.Not.Null);
    }

    [Test]
    public void AddMessagingServices_ShouldRegisterAdminActionStoreAsSingleton()
    {
        _services.AddMessagingServices();

        var serviceProvider = _services.BuildServiceProvider();
        var first = serviceProvider.GetRequiredService<IAdminActionStore>();
        var second = serviceProvider.GetRequiredService<IAdminActionStore>();

        Assert.That(first, Is.SameAs(second));
    }

    /// <summary>
    /// POC: Проверка регистрации IMessageService
    /// <tags>poc, message-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterIMessageService()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var messageService = serviceProvider.GetService<IMessageService>();
        Assert.That(messageService, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации IChatLinkFormatter
    /// <tags>poc, chat-link-formatter, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterIChatLinkFormatter()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var chatLinkFormatter = serviceProvider.GetService<IChatLinkFormatter>();
        Assert.That(chatLinkFormatter, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации INotificationService
    /// <tags>poc, notification-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterINotificationService()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var notificationService = serviceProvider.GetService<INotificationService>();
        Assert.That(notificationService, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации ILogChatService
    /// <tags>poc, log-chat-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldRegisterILogChatService()
    {
        // Act
        _services.AddMessagingServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var logChatService = serviceProvider.GetService<ILogChatService>();
        Assert.That(logChatService, Is.Not.Null);
    }

    /// <summary>
    /// Production: Проверка возврата IServiceCollection для цепочки вызовов
    /// <tags>production, fluent-api, di-registration</tags>
    /// </summary>
    [Test]
    public void AddMessagingServices_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddMessagingServices();

        // Assert
        Assert.That(result, Is.SameAs(_services));
    }
}