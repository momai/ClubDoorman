using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фейковый сервис модерации для тестирования
/// Позволяет настраивать результаты модерации сообщений
/// </summary>
public class FakeModerationService : IModerationPolicy
{
    private readonly ILogger<FakeModerationService> _logger;
    private readonly ISpamHamClassifier _classifier;
    private readonly IMimicryClassifier _mimicryClassifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IUserManager _userManager;
    private readonly IAiChecks _aiChecks;
    private readonly ISuspiciousUsersStorage _suspiciousUsersStorage;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IUserBanService _userBanService;
    // Флаг для принудительного применения настроенного результата при следующем обращении
    private bool _forceNextResult;

    // Настраиваемые результаты
    public ModerationResult NextResult { get; set; } = new ModerationResult(ModerationAction.Allow, "Безопасно");
    public TimeSpan ResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }

    // История вызовов
    public List<ModerationRequest> ModerationRequests { get; } = new();

    public FakeModerationService(
        ISpamHamClassifier classifier,
        IMimicryClassifier mimicryClassifier,
        IBadMessageManager badMessageManager,
        IUserManager userManager,
        IAiChecks aiChecks,
        ISuspiciousUsersStorage suspiciousUsersStorage,
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IUserBanService userBanService,
        ILogger<FakeModerationService> logger)
    {
        _classifier = classifier;
        _mimicryClassifier = mimicryClassifier;
        _badMessageManager = badMessageManager;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _suspiciousUsersStorage = suspiciousUsersStorage;
        _bot = bot;
        _messageService = messageService;
        _userBanService = userBanService;
        _logger = logger;
    }

    /// <summary>
    /// Настройка результата модерации
    /// </summary>
    public FakeModerationService SetResult(ModerationResult result)
    {
        NextResult = result;
        // После вызова SetResult() следующий вызов CheckMessageAsync вернет это значение, минуя внутреннюю логику
        _forceNextResult = true;
        return this;
    }

    /// <summary>
    /// Настройка времени ответа
    /// </summary>
    public FakeModerationService SetResponseTime(TimeSpan responseTime)
    {
        ResponseTime = responseTime;
        return this;
    }

    /// <summary>
    /// Настройка исключения
    /// </summary>
    public FakeModerationService SetException(Exception exception)
    {
        ShouldThrowException = true;
        ExceptionToThrow = exception;
        return this;
    }

    /// <summary>
    /// Сброс настроек к значениям по умолчанию
    /// </summary>
    public FakeModerationService Reset()
    {
        NextResult = new ModerationResult(ModerationAction.Allow, "Безопасно");
        ResponseTime = TimeSpan.FromMilliseconds(100);
        ShouldThrowException = false;
        ExceptionToThrow = null;
        return this;
    }

    /// <summary>
    /// Очистка истории
    /// </summary>
    public FakeModerationService ClearHistory()
    {
        ModerationRequests.Clear();
        return this;
    }

    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (message.From == null)
            throw new ModerationException("Информация о пользователе отсутствует в сообщении");

        // Если в тесте явно настроен следующий результат — отдаем его немедленно (упрощает детерминированные unit-тесты)
        if (_forceNextResult)
        {
            _forceNextResult = false; // одноразовое применение
            _logger.LogInformation("FakeModerationService: принудительно возвращен настроенный результат {Action}", NextResult.Action);
            return NextResult;
        }

        var request = new ModerationRequest
        {
            MessageId = message.MessageId,
            ChatId = message.Chat?.Id ?? 0,
            UserId = message.From?.Id ?? 0,
            Text = message.Text ?? "",
            Timestamp = DateTime.UtcNow
        };

        ModerationRequests.Add(request);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake moderation exception");
        }

        await Task.Delay(ResponseTime);

        // Проверяем, есть ли пользователь в блэклисте
        if (message.From != null && _userManager.InBanlist(message.From.Id).Result)
        {
            var result = new ModerationResult(ModerationAction.Ban, "Пользователь в блэклисте спамеров");
            _logger.LogInformation("FakeModerationService: модерация сообщения {MessageId} от пользователя {UserId}, действие: {Action}",
                message.MessageId, message.From.Id, result.Action);
            return result;
        }

        // Пустое сообщение -> Report (extended tests ожидают Report)
        if (string.IsNullOrWhiteSpace(message.Text) && string.IsNullOrWhiteSpace(message.Caption))
        {
            var emptyResult = new ModerationResult(ModerationAction.Report, "Пустое сообщение");
            _logger.LogInformation("FakeModerationService: пустое сообщение {MessageId} от пользователя {UserId}, действие: {Action}",
                message.MessageId, message.From?.Id, emptyResult.Action);
            return emptyResult;
        }

        // Сообщение с кнопками -> Ban
        if (message.ReplyMarkup is InlineKeyboardMarkup)
        {
            var buttonsResult = new ModerationResult(ModerationAction.Ban, "Сообщение с кнопками");
            _logger.LogInformation("FakeModerationService: сообщение с кнопками {MessageId} от пользователя {UserId}, действие: {Action}",
                message.MessageId, message.From?.Id, buttonsResult.Action);
            return buttonsResult;
        }

        // Story -> Delete
        if (message.Story != null)
        {
            var storyResult = new ModerationResult(ModerationAction.Delete, "Сообщение со Сторис");
            _logger.LogInformation("FakeModerationService: сообщение со сторис {MessageId} от пользователя {UserId}, действие: {Action}",
                message.MessageId, message.From?.Id, storyResult.Action);
            return storyResult;
        }

        // Проверяем BadMessageManager
        if (_badMessageManager.KnownBadMessage(message.Text ?? ""))
        {
            var result = new ModerationResult(ModerationAction.Ban, "Известное спам-сообщение");
            _logger.LogInformation("FakeModerationService: модерация сообщения {MessageId} от пользователя {UserId}, действие: {Action}",
                message.MessageId, message.From?.Id, result.Action);
            return result;
        }

        // Проверяем SpamHamClassifier
        try
        {
            var (isSpam, probability) = await _classifier.IsSpam(message.Text ?? "");
            if (isSpam)
            {
                var result = new ModerationResult(ModerationAction.Delete, $"ML решил что это спам (вероятность: {probability:F2})");
                _logger.LogInformation("FakeModerationService: модерация сообщения {MessageId} от пользователя {UserId}, действие: {Action}",
                    message.MessageId, message.From?.Id, result.Action);
                return result;
            }
        }
        catch (Exception)
        {
            // Пробрасываем исключение как есть, без оборачивания в AggregateException
            throw;
        }

        // Если все проверки пройдены, возвращаем Allow
        var allowResult = new ModerationResult(ModerationAction.Allow, "Сообщение прошло все проверки");
        _logger.LogInformation("FakeModerationService: модерация сообщения {MessageId} от пользователя {UserId}, действие: {Action}",
            message.MessageId, message.From?.Id, allowResult.Action);
        return allowResult;
    }

    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.FirstName))
            throw new ModerationException("Имя пользователя не может быть пустым"); // ожидается тестами

        var request = new ModerationRequest
        {
            UserId = user.Id,
            Text = $"{user.FirstName} {user.LastName}",
            Timestamp = DateTime.UtcNow
        };

        ModerationRequests.Add(request);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake user moderation exception");
        }

        await Task.Delay(ResponseTime);

        // Анализируем длину имени пользователя
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        if (fullName.Length > 100)
        {
            var result = new ModerationResult(ModerationAction.Ban, "Экстремально длинное имя");
            _logger.LogInformation("FakeModerationService: модерация пользователя {UserId}, действие: {Action}",
                user.Id, result.Action);
            return result;
        }

        if (fullName.Length > 50)
        {
            var result = new ModerationResult(ModerationAction.Report, "Подозрительно длинное имя");
            _logger.LogInformation("FakeModerationService: модерация пользователя {UserId}, действие: {Action}",
                user.Id, result.Action);
            return result;
        }

        var allowResult = new ModerationResult(ModerationAction.Allow, "Имя пользователя корректно");
        _logger.LogInformation("FakeModerationService: модерация пользователя {UserId}, действие: {Action}",
            user.Id, allowResult.Action);
        return allowResult;
    }

    public async Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake moderation action exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: выполнено действие модерации для сообщения {MessageId}", message.MessageId);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        // Для юнит-тестов ожидается false по умолчанию (пользователь не одобрен)
        return false;
    }

    public async Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (chat == null) throw new ArgumentNullException(nameof(chat));
        if (messageText == null) throw new ArgumentNullException(nameof(messageText));
        if (string.IsNullOrWhiteSpace(messageText)) throw new ArgumentException("messageText cannot be empty", nameof(messageText));
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake increment good message exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: увеличен счетчик хороших сообщений для пользователя {UserId}", user.Id);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        _logger.LogInformation("FakeModerationService: установлен AI-детект для пользователя {UserId} в чате {ChatId}: {Enabled}",
            userId, chatId, enabled);
        // Делегируем в storage — бизнес-логика тестов ожидает true при соответствующей настройке
        return _suspiciousUsersStorage.SetAiDetectEnabled(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        // Тесты ожидают нули по умолчанию
        return (0, 0, 0);
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        // Тесты ожидают null
        return null!; // оглушаем предупреждение, возвращаем null намеренно
    }

    public async Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake AI detect check exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: проверен AI-детект для пользователя {UserId}", user.Id);
        return NextResult.Action != ModerationAction.Allow;
    }

    public async Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake unrestrict and approve exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: сняты ограничения и одобрен пользователь {UserId} в чате {ChatId}",
            userId, chatId);
        return true;
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _logger.LogInformation("FakeModerationService: очищен пользователь {UserId} из всех списков в чате {ChatId}",
            userId, chatId);
    }

    public async Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake ban and cleanup exception");
        }

        await Task.Delay(ResponseTime);

        try
        {
            // Создаем фейковые объекты для бана
            var chat = new Chat { Id = chatId };
            var user = new User { Id = userId };

            // Вызываем реальные методы через зависимости
            await _userBanService.BanUserAsync(chat, user, BanTypeEnum.AutoBan, "Автобан", null, CancellationToken.None);

            // Удаляем сообщение, если указано
            if (messageIdToDelete.HasValue)
            {
                await _userBanService.DeleteMessageByIdAsync(chatId, messageIdToDelete.Value);
            }

            // Очищаем пользователя из всех списков
            CleanupUserFromAllLists(userId, chatId);

            _logger.LogInformation("FakeModerationService: забанен и очищен пользователь {UserId} в чате {ChatId}",
                userId, chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FakeModerationService: ошибка при бане пользователя {UserId} в чате {ChatId}",
                userId, chatId);
            return false;
        }
    }

    public async Task HandleUserMessageAsync(
        Message message,
        User user,
        Chat chat,
        ModerationResult moderationResult,
        bool isSilentMode,
        CancellationToken cancellationToken)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake HandleUserMessageAsync exception");
        }

        await Task.Delay(ResponseTime);

        // Логируем вызов
        _logger.LogInformation("FakeModerationService: HandleUserMessageAsync вызван для пользователя {UserId} в чате {ChatId}, действие: {Action}",
            user.Id, chat.Id, moderationResult.Action);

        // В зависимости от результата модерации выполняем соответствующие действия
        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                // Ничего не делаем для разрешенных сообщений
                break;

            case ModerationAction.Ban:
                // Бан пользователя
                await BanAndCleanupUserAsync(user.Id, chat.Id, message.MessageId);
                break;

            case ModerationAction.Delete:
                // Удаляем сообщение
                await _userBanService.DeleteMessageByIdAsync(chat.Id, message.MessageId);
                break;

            case ModerationAction.Report:
                // Отправляем отчет (в фейке просто логируем)
                _logger.LogInformation("FakeModerationService: отправлен отчет для сообщения {MessageId}", message.MessageId);
                break;

            default:
                _logger.LogWarning("FakeModerationService: неизвестное действие модерации {Action}", moderationResult.Action);
                break;
        }
    }


}

/// <summary>
/// Запрос на модерацию
/// </summary>
public record ModerationRequest
{
    public int MessageId { get; init; }
    public long ChatId { get; init; }
    public long UserId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}