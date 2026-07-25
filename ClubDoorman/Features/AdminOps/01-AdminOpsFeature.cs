using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Регистрация зависимостей для фичи AdminOps
/// </summary>
public static class AdminOpsFeature
{
    /// <summary>
    /// Регистрирует все сервисы для админских операций
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    public static IServiceCollection AddAdminOpsFeature(this IServiceCollection services)
    {
        // Регистрируем все CommandHandler'ы как ICommandHandler
        services.AddSingleton<ICommandHandler, SpamCommandHandler>();
        services.AddSingleton<ICommandHandler, HamCommandHandler>();
        services.AddSingleton<ICommandHandler, CheckCommandHandler>();
        services.AddSingleton<ICommandHandler, StatsCommandHandler>();
        services.AddSingleton<ICommandHandler, SayCommandHandler>();
        // Убираем StatsAliasCommandHandler - StatsCommandHandler уже обрабатывает и "stat", и "stats"
        services.AddSingleton<ICommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<ICommandHandler, StartCommandHandler>();

        // Регистрируем дополнительные интерфейсы для обратной совместимости
        services.AddSingleton<IStartCommandHandler, StartCommandHandler>();
        services.AddSingleton<ISuspiciousCommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<StatsCommandHandler>(); // Для обратной совместимости

        // Регистрируем CommandRouter
        services.AddSingleton<ICommandRouter, CommandRouter>();

        services.AddSingleton<IAdminCallbackHandler, ApproveUserCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, BanUserCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, LogBanCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, BanProfileCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, AiOkCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, SuspiciousUserCallbackHandler>();
        services.AddSingleton<IAdminCallbackHandler, NoopCallbackHandler>();
        services.AddSingleton<IAdminCallbackDispatcher, AdminCallbackDispatcher>();

        // Регистрируем CommandProcessingService для обратной совместимости
        services.AddSingleton<ICommandProcessingService, CommandProcessingService>();

        // Регистрируем фасад
        services.AddSingleton<IAdminOpsFacade, AdminOpsFacade>();

        return services;
    }
}
