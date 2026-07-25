using ClubDoorman.Features.AdminOps;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Captcha;
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
using ClubDoorman.Effects.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ClubDoorman.Infrastructure;

/// <summary>
/// Расширения для IServiceCollection для централизованной регистрации всех сервисов ClubDoorman
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все сервисы ClubDoorman в DI контейнер
    /// Заменяет длинную цепочку вызовов Add*Services в Program.cs
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddClubDoorman(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        // Регистрация конфигурации приложения (должна быть первой)
        services.AddConfigurationServices();

        // Logging / tracing flags (optional configuration section)
        if (configuration != null)
        {
            services.Configure<LoggingFlagsOptions>(configuration.GetSection("LoggingFlags"));
        }
        else
        {
            services.Configure<LoggingFlagsOptions>(_ => { }); // defaults
        }

    // Golden Master recorder + moderation event publisher abstraction
    services.AddSingleton<IGoldenMasterRecorder, GoldenMasterRecorder>();
    services.AddSingleton<IModerationEventPublisher, GoldenMasterModerationEventPublisher>();

        // Регистрация инфраструктуры эффектов (должна быть перед AppConfig)
        services.AddSingleton<EffectsConfiguration>(provider => new EffectsConfiguration
        {
            UseRealEffects = true, // Включаем реальные эффекты
            EnabledActions = new[] { "Delete", "Report", "Ban", "Allow", "RequireManualReview", "RequireAiAnalysis" }, // Включаем все Actions
            LegacyFallback = true, // Включаем fallback для безопасности
            LogComparison = true // Включено сравнение логов
        });
        services.AddSingleton<IEffectBus, EffectBus>();
        services.AddSingleton<IModerationActionHandler, AllowActionHandler>();
        services.AddSingleton<IModerationActionHandler, DeleteActionHandler>();
        services.AddSingleton<IModerationActionHandler, BanActionHandler>();
        services.AddSingleton<IModerationActionHandler, ReportActionHandler>();
        services.AddSingleton<IModerationActionHandler, ManualReviewActionHandler>();
        services.AddSingleton<IModerationActionHandler, AiAnalysisActionHandler>();
        services.AddSingleton<IModerationActionDispatcher, ModerationActionDispatcher>();
        services.AddSingleton<IChannelModerationEffectsBuilder>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ChannelModerationEffectsBuilder>>();
            return new ChannelModerationEffectsBuilder(
                logger,
                sp.GetRequiredService<ITelegramBotClientWrapper>(),
                sp.GetRequiredService<IUserBanService>(),
                sp.GetRequiredService<IModerationService>());
        });

        // Регистрация основных сервисов в том же порядке, что и в Program.cs
        services.AddLinkFormattingServices();
        services.AddDispatcherServices();
        services.AddUserJoinServices();
        services.AddUserBanServices();
        services.AddUserJoinFeature();
        services.AddModerationFeature();
        services.AddModerationServices();
        services.AddChannelModerationServices();
        services.AddSuspiciousUsersServices();
        services.AddUserFlowServices();
        services.AddViolationServices();
        services.AddBadMessageServices();

        // Telegram Bot Client - создаем после регистрации IAppConfig
        services.AddSingleton<TelegramBotClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TelegramBotClient>>();
            logger.LogDebug("[DI] TelegramBotClient factory called");
            var appConfig = provider.GetRequiredService<IAppConfig>();
            var botApiPreview = appConfig.BotApi != null ? appConfig.BotApi.Substring(0, Math.Min(appConfig.BotApi.Length, 10)) : "null";
            logger.LogDebug("[DI] IAppConfig resolved: {AppConfigType}, BotApi: {BotApiPrefix}...", appConfig.GetType().Name, botApiPreview);

            // Baseline / golden harness mode: we don't need a real Telegram client; return a stub to avoid token validation.
            if (provider.GetRequiredService<IAppConfig>().GoldenBaselineMode)
            {
                logger.LogWarning("[DI] Golden baseline mode detected — returning dummy TelegramBotClient to skip token validation.");
                // Provide syntactically valid but obviously fake dummy token (avoids secret scanner hits).
                return new TelegramBotClient("000000000:PLACEHOLDER_PLACEHOLDER_PLACEHOLD");
            }

            if (string.IsNullOrEmpty(appConfig.BotApi) || appConfig.BotApi == "test-bot-token")
            {
                logger.LogError("[DI] DOORMAN_BOT_API is not set or is 'test-bot-token'.");
                throw new InvalidOperationException("❌ Бот не может запуститься: DOORMAN_BOT_API не настроен или равен 'test-bot-token'. Установите переменную окружения DOORMAN_BOT_API с валидным токеном бота.");
            }

            logger.LogDebug("[DI] 🤖 Starting bot with token: {BotApiPrefix}...", botApiPreview);
            return new TelegramBotClient(appConfig.BotApi);
        });

        // Telegram Bot Client интерфейсы
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ITelegramBotClient>>();
            logger.LogDebug("[DI] ITelegramBotClient factory called");
            return provider.GetRequiredService<TelegramBotClient>();
        });

        services.AddTelegramServices();
        services.AddStatisticsServices();
        services.AddAIServices();
        services.AddUserManagementServices();
        services.AddMessagingServices();
        services.AddTextProcessingServices();
        services.AddCaptchaServices();
    services.AddHandlersServices();
    // ВНИМАНИЕ: AddCommandsServices уже вызывает AddAdminOpsFeature внутри (обратная совместимость).
    // Ранее здесь вызывались И AddCommandsServices(), И AddAdminOpsFeature(), что приводило к двойной
    // регистрации ICommandHandler и предупреждениям CommandRouter про дубли команд.
    services.AddCommandsServices(); // один вызов достаточно

    // Pipeline core (Phase 2+) — регистрируем конвейер и ВСЕ шаги (10–220)
    services.AddSingleton<Services.Handlers.Pipeline.IMessagePipeline, Services.Handlers.Pipeline.MessagePipeline>();
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.CommandStep>(); // 10
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.SystemOrBotMessageStep>(); // 15
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.NewMembersStep>(); // 20
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.LeftMemberCleanupStep>(); // 30
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.ChannelMessageStep>(); // 40
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.PrivateSkipStep>(); // 50
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.CaptchaPendingStep>(); // 100
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.BanlistCheckStep>(); // 110
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.AlreadyApprovedStep>(); // 120
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.FirstMessageLogStep>(); // 130
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.ClubMemberSkipStep>(); // 140
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.BaseModerationStep>(); // 200
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.AiProfileAnalysisStep>(); // 210
    services.AddSingleton<Services.Handlers.Pipeline.IMessageStep, ClubDoorman.Services.Handlers.Pipeline.Steps.FinalModerationActionStep>(); // 220

        services.AddHostedService<Worker>();

        // Конфигурация логирования (централизованная система сообщений перенесена в MessagingModule)
        services.Configure<LoggingConfiguration>(options => { });

        return services;
    }
}
