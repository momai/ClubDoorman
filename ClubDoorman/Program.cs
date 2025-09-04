using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using Serilog;
using Serilog.Events;
using DotNetEnv;
using ClubDoorman.Effects;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Загружаем переменные из .env файла если он существует
        var currentDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(currentDir, ".env");

        if (File.Exists(envPath))
        {
            Console.WriteLine($"📄 Загружаем переменные из файла: {envPath}");
            Env.Load(envPath);
        }
        else
        {
            Console.WriteLine("📄 Файл .env не найден, используем переменные окружения");
            Console.WriteLine($"🔍 Искали в: {envPath}");
        }

        InitData();
        var hostBuilder = Host.CreateDefaultBuilder(args)
                .UseSerilog(
                    (_, _, config) =>
                    {
                        // Создаем директорию для логов если её нет
                        var logsDir = "logs";
                        if (!Directory.Exists(logsDir))
                        {
                            Directory.CreateDirectory(logsDir);
                        }

                        // Dynamic logging configuration from environment.
                        // Base (legacy) var:
                        //   DOORMAN_LOG_LEVEL: Verbose|Debug|Information|Warning|Error|Fatal  (default: Information)
                        // New (optional, overrides DOORMAN_LOG_LEVEL for specific sinks):
                        //   DOORMAN_LOG_LEVEL_CONSOLE
                        //   DOORMAN_LOG_LEVEL_FILE
                        // Trace booster:
                        //   DOORMAN_TRACE_ENABLE=true  (forces FILE level to Verbose, leaves console as‑is)
                        static LogEventLevel ParseLevel(string? value, LogEventLevel fallback)
                        {
                            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<LogEventLevel>(value, true, out var lvl))
                                return lvl;
                            return fallback;
                        }

                        var baseLevel = ParseLevel(Environment.GetEnvironmentVariable("DOORMAN_LOG_LEVEL"), LogEventLevel.Information);
                        var consoleLevel = ParseLevel(Environment.GetEnvironmentVariable("DOORMAN_LOG_LEVEL_CONSOLE"), baseLevel);
                        var fileLevel = ParseLevel(Environment.GetEnvironmentVariable("DOORMAN_LOG_LEVEL_FILE"), baseLevel);
                        var traceEnabled = bool.TryParse(Environment.GetEnvironmentVariable("DOORMAN_TRACE_ENABLE"), out var te) && te;
                        if (traceEnabled && fileLevel > LogEventLevel.Verbose)
                            fileLevel = LogEventLevel.Verbose; // escalate only file sink for deep diagnostics

                        // Root minimum must be the lowest of all sink minima so that higher-verbosity sinks receive events.
                        var rootMin = consoleLevel < fileLevel ? consoleLevel : fileLevel;

                        config
                            .MinimumLevel.Is(rootMin)
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .MinimumLevel.Override("System", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            .Enrich.WithProperty("Application", "ClubDoorman")
                            .Enrich.WithProperty("TraceEnabled", traceEnabled)
                            // Console sink with its own minimum level (can be higher than file level to reduce noise in stdout)
                            .WriteTo.Async(a => a.Console(restrictedToMinimumLevel: consoleLevel))
                            .WriteTo.Async(a => a.File(
                                path: Path.Combine(logsDir, "clubdoorman-.log"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 7,
                                restrictedToMinimumLevel: fileLevel,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                            ))
                            .WriteTo.Async(a => a.File(
                                path: Path.Combine(logsDir, "errors-.log"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 30,
                                restrictedToMinimumLevel: LogEventLevel.Error,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                            ))
                            .WriteTo.Async(a => a.File(
                                path: Path.Combine(logsDir, "system-.log"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 14,
                                restrictedToMinimumLevel: LogEventLevel.Information,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [System] {Message:lj}{NewLine}{Exception}"
                            ))
                            .WriteTo.Logger(lc => lc
                                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("UserFlow"))
                                .WriteTo.Async(a => a.File(
                                    path: Path.Combine(logsDir, "userflow-.log"),
                                    rollingInterval: RollingInterval.Day,
                                    retainedFileCountLimit: 7,
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [UserFlow] {Message:lj}{NewLine}{Exception}"
                                ))
                            );
                    }
                )
                .ConfigureServices((ctx, services) =>
                {
                    // Единая точка регистрации всех сервисов ClubDoorman
                    services.AddClubDoorman(ctx.Configuration);

                    // Логируем статус AI и Mimicry систем после полной инициализации
                    services.PostConfigure<IAppConfig>(appConfig =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("[DI] PostConfigure: IAppConfig loaded. AI/Mimicry/Other system status will be logged here if needed.");
                    });
                });


        var host = hostBuilder.Build();

        // Логируем статус AI и Mimicry систем после полной инициализации
        var appConfig = host.Services.GetRequiredService<IAppConfig>();

        if (appConfig.OpenRouterApi != null)
        {
            Console.WriteLine("🤖 AI анализ: ВКЛЮЧЕН");
        }
        else
        {
            Console.WriteLine("🤖 AI анализ: ОТКЛЮЧЕН (DOORMAN_OPENROUTER_API не настроен)");
        }

        if (appConfig.SuspiciousDetectionEnabled)
        {
            Console.WriteLine($"🎭 Система мимикрии: ВКЛЮЧЕНА (порог: {appConfig.MimicryThreshold:F1})");
        }
        else
        {
            Console.WriteLine("🎭 Система мимикрии: ОТКЛЮЧЕНА (DOORMAN_SUSPICIOUS_DETECTION_ENABLE не установлен)");
        }

        // Информация о загруженных переменных окружения
        Console.WriteLine("📋 Загруженные переменные окружения:");
        Console.WriteLine($"   • DOORMAN_BOT_API: {(string.IsNullOrEmpty(appConfig.BotApi) ? "не найдено" : "найдено")}");
        Console.WriteLine($"   • DOORMAN_ADMIN_CHAT: {appConfig.AdminChatId}");
        Console.WriteLine($"   • DOORMAN_LOG_ADMIN_CHAT: {appConfig.LogAdminChatId}");
        Console.WriteLine($"   • DOORMAN_OPENROUTER_API: {(appConfig.OpenRouterApi != null ? "найдено" : "не найдено")}");
        Console.WriteLine($"   • DOORMAN_SUSPICIOUS_DETECTION_ENABLE: {appConfig.SuspiciousDetectionEnabled}");
        Console.WriteLine($"   • DOORMAN_MIMICRY_THRESHOLD: {appConfig.MimicryThreshold:F1}");
        Console.WriteLine($"   • DOORMAN_SUSPICIOUS_TO_APPROVED_COUNT: {appConfig.SuspiciousToApprovedMessageCount}");
        // Остальные свойства теперь доступны через IAppConfig
        Console.WriteLine($"   • DOORMAN_GLOBAL_APPROVAL_MODE: {appConfig.GlobalApprovalMode}");
        Console.WriteLine($"   • DOORMAN_BLACKLIST_AUTOBAN_DISABLE: {!appConfig.BlacklistAutoBan}");
        Console.WriteLine($"   • DOORMAN_CHANNELS_AUTOBAN_DISABLE: {!appConfig.ChannelAutoBan}");
        Console.WriteLine($"   • DOORMAN_BAN_FOLDER_INVITE_USERS: {appConfig.BanFolderInviteUsers}");
        Console.WriteLine($"   • DOORMAN_BAN_FOLDER_INVITE_NOTIFICATIONS_DISABLE: {appConfig.BanFolderInviteNotificationsDisable}");
        Console.WriteLine($"   • DOORMAN_BUTTON_AUTOBAN_DISABLE: {!appConfig.ButtonAutoBan}");
        Console.WriteLine($"   • DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE: {!appConfig.HighConfidenceAutoBan}");
        Console.WriteLine($"   • DOORMAN_LOW_CONFIDENCE_HAM_ENABLE: {appConfig.LowConfidenceHamForward}");
        Console.WriteLine($"   • DOORMAN_APPROVE_BUTTON: {appConfig.ApproveButtonEnabled}");
        Console.WriteLine($"   • DOORMAN_DISABLE_MEDIA_FILTERING: {appConfig.DisableMediaFiltering}");
        Console.WriteLine($"   • DOORMAN_DELETE_FORWARDED_MESSAGES: {appConfig.DeleteForwardedMessages}");
        Console.WriteLine($"   • DOORMAN_PRIVATE_START_DISABLE: {!appConfig.IsPrivateStartAllowed()}");
        Console.WriteLine($"   • Отключенные чаты: {appConfig.DisabledChats.Count}");
        Console.WriteLine($"   • Белый список чатов: {appConfig.WhitelistChats.Count}");
        Console.WriteLine($"   • AI-включенные чаты: {appConfig.AiEnabledChats.Count}");
        Console.WriteLine($"   • Группы без VPN-рекламы: {appConfig.NoVpnAdGroups.Count}");
        Console.WriteLine($"   • Группы с отключенной капчей: {appConfig.NoCaptchaGroups.Count}");
        Console.WriteLine($"   • Чаты с отключенной фильтрацией медиа: {appConfig.MediaFilteringDisabledChats.Count}");

        await host.RunAsync();
    }

    private static void InitData()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var dataInit = Path.Combine(basePath, "data_init");
        if (!Directory.Exists(dataInit))
            return;

        var data = Path.Combine(basePath, "data");
        if (!Directory.Exists(data))
            Directory.CreateDirectory(data);
        foreach (var sourceFullPath in Directory.EnumerateFiles(dataInit))
        {
            var file = Path.GetFileName(sourceFullPath);
            var target = Path.Combine(data, file);
            if (!File.Exists(target))
                File.Copy(sourceFullPath, target);
        }
    }
}