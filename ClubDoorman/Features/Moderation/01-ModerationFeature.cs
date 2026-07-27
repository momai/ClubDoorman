using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Feature модуль для Moderation функциональности
/// <tags>moderation, feature, di, registration</tags>
/// </summary>
public static class ModerationFeature
{
    /// <summary>
    /// Регистрирует Moderation сервисы в DI контейнере
    /// <tags>moderation, di, registration, services</tags>
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    public static IServiceCollection AddModerationFeature(this IServiceCollection services)
    {
        services.AddSingleton<IContentModerationPolicy, ContentModerationPolicy>();
        services.AddSingleton<IModerationPolicy, ModerationPolicy>();
        services.AddSingleton<IModerationFacade, ModerationFacade>();
        return services;
    }
}
