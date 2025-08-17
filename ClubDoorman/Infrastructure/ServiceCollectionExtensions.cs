using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.TextProcessing;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Violation;
using ClubDoorman.Models.Logging;
using ClubDoorman.Effects;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ClubDoorman.Infrastructure;

/// <summary>
/// Упрощенная регистрация сервисов ClubDoorman
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все сервисы ClubDoorman в DI контейнер
    /// Упрощенная версия без лишних модулей и фасадов
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddClubDoorman(this IServiceCollection services)
    {
        // Регистрация конфигурации приложения (должна быть первой)
        services.AddConfigurationServices();
        
        // Регистрация инфраструктуры эффектов
        services.AddSingleton<EffectsConfiguration>(provider => new EffectsConfiguration
        {
            UseRealEffects = true,
            EnabledActions = new[] { "Delete", "Report", "Ban", "Allow", "RequireManualReview", "RequireAiAnalysis" },
            LegacyFallback = true,
            LogComparison = true
        });
        services.AddSingleton<IEffectBus, EffectBus>();
        services.AddSingleton<ModerationEffectsBuilder>();
        services.AddSingleton<IModerationEffectsBuilder, ModerationEffectsBuilder>();

        // Прямая регистрация сервисов (без лишних модулей)
        services.AddLinkFormattingServices();
        services.AddDispatcherServices();
        services.AddUserJoinServices();
        services.AddUserBanServices();
        services.AddModerationServices();
        services.AddChannelModerationServices();
        services.AddSuspiciousUsersServices();
        services.AddUserFlowServices();
        services.AddViolationServices();
        services.AddBadMessageServices();

        // Telegram Bot Client
        services.AddSingleton<TelegramBotClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TelegramBotClient>>();
            var appConfig = provider.GetRequiredService<IAppConfig>();

            if (string.IsNullOrEmpty(appConfig.BotApi))
            {
                throw new InvalidOperationException(
                    "❌ Бот не может запуститься: DOORMAN_BOT_API не настроен. " +
                    "Установите переменную окружения DOORMAN_BOT_API с валидным токеном бота."
                );
            }

            return new TelegramBotClient(appConfig.BotApi);
        });

        services.AddSingleton<ITelegramBotClient>(provider => provider.GetRequiredService<TelegramBotClient>());

        services.AddTelegramServices();
        services.AddStatisticsServices();
        services.AddAIServices();
        services.AddUserManagementServices();
        services.AddMessagingServices();
        services.AddTextProcessingServices();
        services.AddCaptchaServices();
        services.AddHandlersServices();
        services.AddCommandsServices();

        // Регистрация Worker как HostedService
        services.AddHostedService<Worker>(provider =>
        {
            return new Worker(
                provider.GetRequiredService<ILogger<Worker>>(),
                provider.GetRequiredService<IUpdateDispatcher>(),
                provider.GetRequiredService<ICaptchaService>(),
                provider.GetRequiredService<IStatisticsService>(),
                provider.GetRequiredService<ISpamHamClassifier>(),
                provider.GetRequiredService<IUserManager>(),
                provider.GetRequiredService<IBadMessageManager>(),
                provider.GetRequiredService<IAiChecks>(),
                provider.GetRequiredService<IChatLinkFormatter>(),
                provider.GetRequiredService<ITelegramBotClientWrapper>(),
                provider.GetRequiredService<IMessageService>(),
                provider.GetRequiredService<IAppConfig>(),
                provider.GetRequiredService<IUserBanService>()
            );
        });

        services.Configure<LoggingConfiguration>(options => { });

        return services;
    }
}