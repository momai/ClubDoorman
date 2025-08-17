using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Handlers;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserJoin;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Модуль для регистрации Handlers сервисов - упрощенная версия
/// </summary>
public static class HandlersModule
{
    /// <summary>
    /// Добавляет Handlers сервисы в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddHandlersServices(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateDispatcher, UpdateDispatcher>();
        services.AddSingleton<IntroFlowService>();
        services.AddSingleton<IBotPermissionsService, BotPermissionsService>();

        // Регистрируем прямые сервисы вместо фасадов
        services.AddSingleton<IModerationPolicy, ModerationPolicy>();
        services.AddSingleton<IUserJoinPolicy, UserJoinPolicy>();

        // Регистрируем MessageHandler
        services.AddSingleton<IUpdateHandler, MessageHandler>();

        services.AddSingleton<IUpdateHandler, CallbackQueryHandler>();
        services.AddSingleton<CallbackQueryHandler>();
        services.AddSingleton<IUpdateHandler, ChatMemberHandler>();
        services.AddSingleton<ChatMemberHandler>();

        return services;
    }
}