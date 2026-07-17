
namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("e2e")]
public class SimpleE2ETests
{
    private ILogger<AiChecks> _logger = null!;
    private FakeTelegramClient _fakeBot = null!;
    private AiChecks _aiChecks = null!;
    private SpamHamClassifier _spamHamClassifier = null!;
    private MimicryClassifier _mimicryClassifier = null!;

    private string? FindEnvFile()
    {
        var baseDir = AppContext.BaseDirectory;

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
            Path.Combine(baseDir, "ClubDoorman/ClubDoorman/.env")
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
        _fakeBot = FakeTelegramClientFactory.Create();

        // Загружаем .env файл
        var envPath = FindEnvFile();
        if (envPath == null)
        {
            Assert.Ignore("Файл .env не найден, пропускаем E2E тесты");
        }
        DotNetEnv.Env.Load(envPath);

        // Загружаем переменные в Environment для Config.cs
        var apiKey = DotNetEnv.Env.GetString("DOORMAN_OPENROUTER_API");
        var botToken = DotNetEnv.Env.GetString("DOORMAN_BOT_API");
        var adminChat = DotNetEnv.Env.GetString("DOORMAN_ADMIN_CHAT");

        Environment.SetEnvironmentVariable("DOORMAN_OPENROUTER_API", apiKey);
        Environment.SetEnvironmentVariable("DOORMAN_BOT_API", botToken);
        Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", adminChat);

        // Проверяем наличие API ключей для E2E тестов
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(botToken))
        {
            Assert.Ignore("API ключи не настроены, пропускаем E2E тесты");
        }



        // Инициализируем сервисы с правильными логгерами
        _aiChecks = new AiChecks(_fakeBot, _logger, AppConfigTestFactory.CreateDefault());
        _spamHamClassifier = new SpamHamClassifier(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SpamHamClassifier>());
        _mimicryClassifier = new MimicryClassifier(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MimicryClassifier>());
    }

    [Test]
    public async Task E2E_SpamHamClassifier_ShouldDetectSpam()
    {
        // Arrange - спам сообщение
        var spamMessage = "🔥🔥🔥 СРОЧНО! ЗАРАБОТАЙ 1000000$ ЗА ДЕНЬ! 🔥🔥🔥 ПЕРЕХОДИ ПО ССЫЛКЕ: https://scam.com";

        // Act
        var result = await _spamHamClassifier.IsSpam(spamMessage);

        // Assert
        Assert.That(result.Score, Is.GreaterThan(0.5), "Спам сообщение должно иметь высокую вероятность");
        Assert.That(result.Spam, Is.True, "Сообщение должно быть классифицировано как спам");

        Console.WriteLine($"E2E тест: Спам сообщение классифицировано с вероятностью {result.Score}");
    }

    [Test]
    public async Task E2E_SpamHamClassifier_ShouldDetectHam()
    {
        // Arrange - нормальное сообщение
        var hamMessage = "Привет всем! Как дела? Надеюсь, у всех все хорошо.";

        // Act
        var result = await _spamHamClassifier.IsSpam(hamMessage);

        // Assert
        Assert.That(result.Score, Is.LessThan(0.5), "Нормальное сообщение должно иметь низкую вероятность спама");
        Assert.That(result.Spam, Is.False, "Сообщение должно быть классифицировано как не спам");

        Console.WriteLine($"E2E тест: Нормальное сообщение классифицировано с вероятностью {result.Score}");
    }

    [Test]
    public async Task E2E_MimicryClassifier_ShouldDetectMimicry()
    {
        // Arrange - подозрительные сообщения
        var messages = new List<string>
        {
            "Здравствуйте! Я администратор. Нужна помощь?",
            "Привет! Я модератор. Могу помочь?",
            "Добрый день! Я поддержка. Есть вопросы?"
        };

        // Act
        var result = _mimicryClassifier.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.3), "Подозрительные сообщения должны иметь повышенную вероятность");

        Console.WriteLine($"E2E тест: Mimicry анализ показал вероятность {result}");
    }

}
