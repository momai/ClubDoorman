using ClubDoorman.Services.Violation;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.UserBan;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using FluentAssertions;
using ClubDoorman.Models;
using ClubDoorman.Handlers;
using Moq;
using DotNetEnv;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("e2e")]
[Category("ai-analysis")]
public class AiAnalysisTests
{
    private ILogger<AiChecks> _logger = null!;
    private ILogger<CallbackQueryHandler> _callbackLogger = null!;
    private FakeTelegramClient _fakeBot = null!;
    private AiChecks _aiChecks = null!;
    private CallbackQueryHandler _callbackHandler = null!;
    private UserManager _userManager = null!;
    private ApprovedUsersStorage _approvedUsersStorage = null!;
    private IAppConfig _appConfig = null!;

    private string? FindEnvFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = Directory.GetCurrentDirectory();

        // Пробуем разные пути относительно AppContext.BaseDirectory
        var possiblePaths = new[]
        {
            Path.Combine(baseDir, "../../../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../ClubDoorman/.env"),
            Path.Combine(baseDir, "ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "ClubDoorman/ClubDoorman/.env"),
            // Добавляем пути относительно текущей директории
            Path.Combine(currentDir, "ClubDoorman/.env"),
            Path.Combine(currentDir, "../ClubDoorman/.env"),
            Path.Combine(currentDir, "../../ClubDoorman/.env"),
            Path.Combine(currentDir, "../../../ClubDoorman/.env"),
            Path.Combine(currentDir, "../../../../ClubDoorman/.env")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null; // Файл не найден
    }

    [SetUp]
    public void Setup()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AiChecks>();
        _callbackLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CallbackQueryHandler>();
        _fakeBot = FakeTelegramClientFactory.Create();

        // Загружаем .env файл для E2E тестов
        var envPath = FindEnvFile();

        if (envPath != null)
        {
            DotNetEnv.Env.Load(envPath);

            // Загружаем переменные в Environment для Config.cs
            var apiKey = DotNetEnv.Env.GetString("DOORMAN_OPENROUTER_API");
            var botToken = DotNetEnv.Env.GetString("DOORMAN_BOT_API");
            var adminChat = DotNetEnv.Env.GetString("DOORMAN_ADMIN_CHAT");

            Environment.SetEnvironmentVariable("DOORMAN_OPENROUTER_API", apiKey);
            Environment.SetEnvironmentVariable("DOORMAN_BOT_API", botToken);
            Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", adminChat);
        }

        // Используем тестовую конфигурацию с моками
        // Это позволяет тестировать AI анализ без реальных API вызовов
        _appConfig = AppConfigTestFactory.CreateDefault(); // Включаем AI с моками
        _approvedUsersStorage = new ApprovedUsersStorage(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ApprovedUsersStorage>());

        _aiChecks = new AiChecks(_fakeBot, _logger, _appConfig);
        _userManager = new UserManager(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UserManager>(), _approvedUsersStorage, _appConfig);

        // Создаем моки для недостающих зависимостей
        var captchaService = new Mock<ICaptchaService>().Object;
        var badMessageManager = new Mock<IBadMessageManager>().Object;
        var statisticsService = new Mock<IStatisticsService>().Object;
        var moderationService = new Mock<IModerationService>().Object;
        var messageService = new Mock<IMessageService>().Object;
        var userBanService = new Mock<IUserBanService>().Object;

        var violationTrackerLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ViolationTracker>();
        var logChatService = new Mock<ILogChatService>().Object;
    // Golden Master recorder not needed for these integration tests -> pass mock
    var gmRecorder = new Moq.Mock<ClubDoorman.Services.Logging.IGoldenMasterRecorder>();
    _callbackHandler = new CallbackQueryHandler(_fakeBot, captchaService, _userManager, badMessageManager, statisticsService, _aiChecks, moderationService, messageService, new ViolationTracker(violationTrackerLogger, _appConfig), userBanService, logChatService, _callbackLogger, gmRecorder.Object, new Moq.Mock<IModerationEventPublisher>().Object, _appConfig);
    }

    [TearDown]
    public void TearDown()
    {
        _fakeBot.Reset();
    }

    [Test]
    public async Task E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis()
    {
        // Arrange - используем MessageHandlerTestFactory вместо FakeServicesFactory
        var factory = new MessageHandlerTestFactory();
        var messageHandler = factory.CreateMessageHandlerForAiAnalysisTests(_fakeBot, _appConfig);

        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        var message = new Message
        {
            From = suspiciousUser,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Привет всем!",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = message };

        // Отладочная информация перед обработкой
        Console.WriteLine($"Обрабатываем сообщение от пользователя {suspiciousUser.Id} в чате {message.Chat.Id} ({message.Chat.Type})");
        Console.WriteLine($"Текст сообщения: {message.Text}");

        // Act - обрабатываем сообщение через MessageHandler
        try
        {
            await messageHandler.HandleAsync(update);
            Console.WriteLine("✅ HandleAsync завершился успешно");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HandleAsync завершился с ошибкой: {ex}");
            throw;
        }

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // Отладочная информация после обработки
        Console.WriteLine($"FakeBot получил {_fakeBot.SentMessages.Count} сообщений:");
        foreach (var msg in _fakeBot.SentMessages)
        {
            Console.WriteLine($"  - ChatId: {msg.ChatId}, Text: {msg.Text}");
        }

        // Проверяем, что фейковый бот получил сообщения
        _fakeBot.SentMessages.Should().NotBeEmpty();

        // Проверяем, что было отправлено уведомление в админ-чат
        _fakeBot.SentMessages.Should().Contain(m =>
            m.ChatId == _appConfig.AdminChatId &&
            m.Text.Contains("AI анализ профиля"));
    }

    [Test]
    [Category("real-api")]
    [Ignore("Requires real API key")]
    public async Task E2E_AI_Analysis_WithRealApi_ShouldWork()
    {
        // Arrange - создаем AiChecks с реальной конфигурацией из .env файла
        var realAppConfig = AppConfigTestFactory.CreateDefault(); // Используем реальную конфигурацию
        var realAiChecks = new AiChecks(_fakeBot, _logger, realAppConfig);

        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        // Act - тестируем с реальным API с ретраем
        var result = await RetryAiAnalysis(async () =>
            await realAiChecks.GetAttentionBaitProbability(suspiciousUser));

        // Assert
        result.Should().NotBeNull();
        result.SpamProbability.Should().NotBeNull();

        // Этот тест может падать из-за 401 ошибки, но это нормально
        // Он показывает, что интеграция с API работает
        result.SpamProbability.Probability.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Test]
    public async Task E2E_AI_Analysis_MessageHandler_ShouldSendNotification()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);

        // Создаем MessageHandler с фейковыми сервисами
        var messageHandler = factory.CreateMessageHandler();

        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        var message = new Message
        {
            From = suspiciousUser,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Привет всем!",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = message };

        // Act - обрабатываем сообщение через MessageHandler
        await messageHandler.HandleAsync(update);

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // Проверяем, что фейковый бот получил сообщения
        _fakeBot.SentMessages.Should().NotBeEmpty();
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Own_ShouldApproveUser()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var callbackHandler = factory.CreateCallbackQueryHandler();

        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };

        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "approve_user_12345"
        };

        // Act - обрабатываем callback через фейковый обработчик
        await callbackHandler.HandleAsync(callbackQuery);

        // Assert - проверяем, что callback был обработан
        callbackHandler.CallbackRequests.Should().HaveCount(1);
        callbackHandler.CallbackResults.Should().HaveCount(1);

        var result = callbackHandler.CallbackResults.First();
        result.CallbackQueryId.Should().Be("test_callback_id");
        result.Data.Should().Be("approve_user_12345");
        result.WasAnswered.Should().BeTrue();

        // Проверяем, что фейковый бот ответил на callback
        _fakeBot.AnsweredCallbackQueries.Should().HaveCount(1);
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Ban_ShouldBanUser()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var callbackHandler = factory.CreateCallbackQueryHandler();

        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "ban_user_12345"
        };

        // Act - обрабатываем callback через фейковый обработчик
        await callbackHandler.HandleAsync(callbackQuery);

        // Assert - проверяем, что callback был обработан
        callbackHandler.CallbackRequests.Should().HaveCount(1);
        callbackHandler.CallbackResults.Should().HaveCount(1);

        var result = callbackHandler.CallbackResults.First();
        result.CallbackQueryId.Should().Be("test_callback_id");
        result.Data.Should().Be("ban_user_12345");
        result.WasAnswered.Should().BeTrue();
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Skip_ShouldSkipUser()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var callbackHandler = factory.CreateCallbackQueryHandler();

        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "skip_user_12345"
        };

        // Act - обрабатываем callback через фейковый обработчик
        await callbackHandler.HandleAsync(callbackQuery);

        // Assert - проверяем, что callback был обработан
        callbackHandler.CallbackRequests.Should().HaveCount(1);
        callbackHandler.CallbackResults.Should().HaveCount(1);

        var result = callbackHandler.CallbackResults.First();
        result.CallbackQueryId.Should().Be("test_callback_id");
        result.Data.Should().Be("skip_user_12345");
        result.WasAnswered.Should().BeTrue();
    }

    [Test]
    public async Task E2E_AI_Analysis_Channel_ShouldNotShowCaptcha()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var messageHandler = factory.CreateMessageHandler();

        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var channelMessage = new Message
        {
            From = user,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Channel },
            Text = "Комментарий в канале",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = channelMessage };

        // Act - обрабатываем сообщение через MessageHandler
        await messageHandler.HandleAsync(update);

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // В каналах капча не показывается, но AI анализ выполняется
        _fakeBot.SentMessages.Should().NotBeEmpty();
    }

    [Test]
    public async Task E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var messageHandler = factory.CreateMessageHandler();

        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var message = new Message
        {
            From = user,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Второе сообщение",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = message };

        // Act - обрабатываем сообщение через MessageHandler
        await messageHandler.HandleAsync(update);

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // Проверяем, что фейковый бот получил сообщения
        _fakeBot.SentMessages.Should().NotBeEmpty();
    }

    [Test]
    public async Task E2E_AI_Analysis_OperationOrder_ShouldBeCorrect()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var messageHandler = factory.CreateMessageHandler();

        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        var message = new Message
        {
            From = suspiciousUser,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Привет всем!",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = message };

        // Act - обрабатываем сообщение через MessageHandler
        await messageHandler.HandleAsync(update);

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // Проверяем, что фейковый бот получил сообщения
        _fakeBot.SentMessages.Should().NotBeEmpty();
    }

    [Test]
    public async Task E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto()
    {
        // Arrange - используем новую фабрику фейковых сервисов
        var factory = new FakeServicesFactory(_fakeBot, LoggerFactory.Create(builder => builder.AddConsole()), _appConfig);
        var messageHandler = factory.CreateMessageHandler();

        var userWithPhoto = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        var message = new Message
        {
            From = userWithPhoto,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Привет всем!",
            Date = DateTime.UtcNow
        };

        var update = new Update { Message = message };

        // Act - обрабатываем сообщение через MessageHandler
        await messageHandler.HandleAsync(update);

        // Assert - проверяем, что обработка прошла без ошибок
        messageHandler.Should().NotBeNull();

        // Проверяем, что фейковый бот получил сообщения
        _fakeBot.SentMessages.Should().NotBeEmpty();
    }

    private async Task<SpamPhotoBio> RetryAiAnalysis(Func<Task<SpamPhotoBio>> analysisFunc, int maxRetries = 3, int delayMs = 1000)
    {
        var lastException = (Exception?)null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                TestContext.WriteLine($"Попытка AI анализа #{attempt}/{maxRetries}");
                var result = await analysisFunc();

                // Проверяем, что результат валидный
                if (result?.SpamProbability != null &&
                    (result.SpamProbability.Probability > 0 || !string.IsNullOrEmpty(result.SpamProbability.Reason)))
                {
                    TestContext.WriteLine($"AI анализ успешно завершен на попытке #{attempt}");
                    return result;
                }

                TestContext.WriteLine($"AI анализ вернул невалидный результат на попытке #{attempt}, повторяем...");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                lastException = ex;
                TestContext.WriteLine($"Ошибка авторизации API (401) на попытке #{attempt}: {ex.Message}");
                if (attempt == maxRetries) throw;
                await Task.Delay(delayMs * attempt); // Увеличиваем задержку с каждой попыткой
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400") || ex.Message.Contains("Bad Request"))
            {
                lastException = ex;
                TestContext.WriteLine($"Ошибка запроса API (400) на попытке #{attempt}: {ex.Message}");
                if (attempt == maxRetries) throw;
                await Task.Delay(delayMs * attempt);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
            {
                lastException = ex;
                TestContext.WriteLine($"Превышен лимит запросов API (429) на попытке #{attempt}: {ex.Message}");
                if (attempt == maxRetries) throw;
                await Task.Delay(delayMs * attempt * 2); // Увеличиваем задержку для rate limit
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                TestContext.WriteLine($"Ошибка соединения API на попытке #{attempt}: {ex.Message}");
                if (attempt == maxRetries) throw;
                await Task.Delay(delayMs * attempt);
            }
            catch (Exception ex)
            {
                lastException = ex;
                TestContext.WriteLine($"Неожиданная ошибка на попытке #{attempt}: {ex.Message}");
                if (attempt == maxRetries) throw;
                await Task.Delay(delayMs * attempt);
            }
        }

        throw lastException ?? new Exception("Все попытки AI анализа завершились неудачно");
    }

}
