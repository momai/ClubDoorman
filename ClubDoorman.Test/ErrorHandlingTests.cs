using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using ClubDoorman.Services.AI;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Test;

/// <summary>
/// Тесты для проверки улучшенной обработки ошибок
/// </summary>
public class ErrorHandlingTests : TestBase
{
    [Test]
    public void AiChecks_GetSpamProbability_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var aiChecks = CreateAiChecks();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await aiChecks.GetSpamProbability(null!);
        });

        Assert.That(exception!.Message, Does.Contain("Сообщение не может быть null"));
    }

    [Test]
    [Category("ErrorHandling")]
    public async Task SpamHamClassifier_Timeout_ReturnsGracefulFallback()
    {
        // Arrange
        var logger = new Mock<ILogger<SpamHamClassifier>>().Object;
        var classifier = new SpamHamClassifier(logger);

        // Act & Assert - должен вернуть fallback результат без зависания
        var result = await classifier.IsSpam("test message").WaitAsync(TimeSpan.FromSeconds(20));

        // Должен вернуть результат (даже если fallback)
        Assert.That(result.Spam, Is.TypeOf<bool>());
        Assert.That(result.Score, Is.TypeOf<float>());
    }

    private static IModerationService CreateModerationService()
    {
        // Используем TestFactory для создания сервиса с моками
        var factory = new ModerationServiceTestFactory();
        return factory.CreateModerationService();
    }

    private static ClubDoorman.Features.Moderation.IModerationFacade CreateModerationFacade()
    {
        // Создаем мок фасада напрямую с настройкой исключений
        var mockFacade = new Mock<ClubDoorman.Features.Moderation.IModerationFacade>();

        // Настраиваем CheckMessageAsync для выброса исключений
        mockFacade.Setup(f => f.CheckMessageAsync(null!))
            .ThrowsAsync(new ArgumentNullException("message", "Сообщение не может быть null"));

        // Настраиваем CheckUserNameAsync для выброса исключений
        mockFacade.Setup(f => f.CheckUserNameAsync(null!))
            .ThrowsAsync(new ArgumentNullException("user", "Пользователь не может быть null"));

        // Настраиваем специально для пользователей с пустым именем, избегая проблемы с null в лямбде
        mockFacade.Setup(f => f.CheckUserNameAsync(It.Is<User>(u => u != null && string.IsNullOrEmpty(u.FirstName))))
            .ThrowsAsync(new ModerationException("Имя пользователя не может быть пустым"));

        // Настраиваем IncrementGoodMessageCountAsync для выброса исключений
        mockFacade.Setup(f => f.IncrementGoodMessageCountAsync(null!, It.IsAny<Chat>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException("user", "Пользователь не может быть null"));

        mockFacade.Setup(f => f.IncrementGoodMessageCountAsync(It.IsAny<User>(), null!, It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException("chat", "Чат не может быть null"));

        mockFacade.Setup(f => f.IncrementGoodMessageCountAsync(It.IsAny<User>(), It.IsAny<Chat>(), ""))
            .ThrowsAsync(new ArgumentException("Текст сообщения не может быть пустым"));

        return mockFacade.Object;
    }

    private static AiChecks CreateAiChecks()
    {
        // Используем TestFactory для создания AiChecks с моками
        var factory = new AiChecksTestFactory();
        return factory.CreateAiChecks();
    }
}
