using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Notifications; // switched to Notifications namespace for implementation

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Модуль для регистрации сервисов мессенджинга
/// </summary>
public static class MessagingModule
{
    /// <summary>
    /// Добавляет все сервисы мессенджинга в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов с добавленными сервисами мессенджинга</returns>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        // Централизованная система сообщений
        services.AddSingleton<MessageTemplates>();
        services.AddSingleton<ILoggingConfigurationService, LoggingConfigurationService>();
        services.AddSingleton<IAdminActionStore, AdminActionStore>();
        services.AddSingleton<IServiceChatDispatcher, ServiceChatDispatcher>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();
        // Bind Messaging.INotificationService to Notifications.NotificationService implementation
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ILogChatService, LogChatService>();

        return services;
    }
}