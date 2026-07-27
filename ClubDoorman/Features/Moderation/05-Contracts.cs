using Telegram.Bot.Types;
using ClubDoorman.Models;

namespace ClubDoorman.Features.Moderation;

public sealed record ContentModerationInput(
    Message Message,
    Chat DestinationChat,
    string? Text);

public interface IContentModerationPolicy
{
    Task<ModerationResult> CheckContentAsync(
        ContentModerationInput input,
        CancellationToken cancellationToken);
}

/// <summary>
/// Интерфейс фасада для функциональности модерации
/// <tags>moderation, facade, interface, coordination</tags>
/// </summary>
public interface IModerationFacade
{
    /// <summary>
    /// Проверяет сообщение на соответствие правилам модерации
    /// <tags>moderation, message-check, rules</tags>
    /// </summary>
    /// <param name="message">Сообщение для проверки</param>
    /// <returns>Результат модерации с рекомендуемым действием и причиной</returns>
    Task<ModerationResult> CheckMessageAsync(Message message);

    /// <summary>
    /// Проверяет пользователя на блокировку по имени
    /// <tags>moderation, username-check, validation</tags>
    /// </summary>
    /// <param name="user">Пользователь для проверки</param>
    /// <returns>Результат проверки имени пользователя</returns>
    Task<ModerationResult> CheckUserNameAsync(User user);

    /// <summary>
    /// Увеличивает счетчик хороших сообщений пользователя и обрабатывает логику одобрения
    /// <tags>moderation, approval, good-messages</tags>
    /// </summary>
    /// <param name="user">Пользователь, отправивший сообщение</param>
    /// <param name="chat">Чат, в котором было отправлено сообщение</param>
    /// <param name="messageText">Текст сообщения</param>
    Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText);

    /// <summary>
    /// Проверяет, одобрен ли пользователь в чате
    /// <tags>moderation, user-approval, status</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата (опционально)</param>
    /// <returns>True если пользователь одобрен</returns>
    bool IsUserApproved(long userId, long? chatId = null);

    /// <summary>
    /// Включает или выключает AI-детект для подозрительного пользователя
    /// <tags>moderation, ai-detect, suspicious-users</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="enabled">Включить или выключить</param>
    /// <returns>True если настройка изменена</returns>
    bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled);

    /// <summary>
    /// Получает статистику по подозрительным пользователям
    /// <tags>moderation, statistics, suspicious-users</tags>
    /// </summary>
    /// <returns>Кортеж с общей статистикой</returns>
    (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats();

    /// <summary>
    /// Получает список подозрительных пользователей с включенным AI-детектом
    /// <tags>moderation, ai-detect, suspicious-users</tags>
    /// </summary>
    /// <returns>Список пользователей с AI-детектом</returns>
    List<(long UserId, long ChatId)> GetAiDetectUsers();

    /// <summary>
    /// Проверяет, включен ли AI-детект для пользователя, и отправляет уведомление админам
    /// <tags>moderation, ai-detect, admin-notification</tags>
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="message">Сообщение</param>
    /// <returns>True если AI-детект включен</returns>
    Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message);

    /// <summary>
    /// Снимает ограничения с пользователя и одобряет его
    /// <tags>moderation, user-approval, restrictions</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>True если операция выполнена успешно</returns>
    Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId);

    /// <summary>
    /// Полностью удаляет пользователя из всех списков (подозрительных, одобренных, кэшей)
    /// <tags>moderation, user-cleanup, lists</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    void CleanupUserFromAllLists(long userId, long chatId);

    /// <summary>
    /// Банит пользователя и удаляет его из всех списков
    /// <tags>moderation, user-ban, cleanup</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageIdToDelete">ID сообщения для удаления (опционально)</param>
    /// <returns>True если операция выполнена успешно</returns>
    Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null);

    /// <summary>
    /// Выполняет действие модерации
    /// <tags>moderation, action-execution</tags>
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="result">Результат модерации</param>
    Task ExecuteModerationActionAsync(Message message, ModerationResult result);

    /// <summary>
    /// Обрабатывает сообщение пользователя на основе результата модерации
    /// <tags>moderation, message-handling, action-execution</tags>
    /// </summary>
    /// <param name="message">Сообщение для обработки</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="moderationResult">Результат модерации</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleUserMessageAsync(
        Message message,
        User user,
        Chat chat,
        ModerationResult moderationResult,
        bool isSilentMode,
        CancellationToken cancellationToken);
}

/// <summary>
/// Политика для принятия решений модерации
/// <tags>moderation, policy, decisions, logic</tags>
/// </summary>
public interface IModerationPolicy
{
    /// <summary>
    /// Проверяет сообщение на соответствие правилам модерации
    /// <tags>moderation, message-check, rules</tags>
    /// </summary>
    /// <param name="message">Сообщение для проверки</param>
    /// <returns>Результат модерации с рекомендуемым действием и причиной</returns>
    Task<ModerationResult> CheckMessageAsync(Message message);

    /// <summary>
    /// Проверяет пользователя на блокировку по имени
    /// <tags>moderation, username-check, validation</tags>
    /// </summary>
    /// <param name="user">Пользователь для проверки</param>
    /// <returns>Результат проверки имени пользователя</returns>
    Task<ModerationResult> CheckUserNameAsync(User user);

    /// <summary>
    /// Увеличивает счетчик хороших сообщений пользователя и обрабатывает логику одобрения
    /// <tags>moderation, approval, good-messages</tags>
    /// </summary>
    /// <param name="user">Пользователь, отправивший сообщение</param>
    /// <param name="chat">Чат, в котором было отправлено сообщение</param>
    /// <param name="messageText">Текст сообщения</param>
    Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText);

    /// <summary>
    /// Проверяет, одобрен ли пользователь в чате
    /// <tags>moderation, user-approval, status</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата (опционально)</param>
    /// <returns>True если пользователь одобрен</returns>
    bool IsUserApproved(long userId, long? chatId = null);

    /// <summary>
    /// Включает или выключает AI-детект для подозрительного пользователя
    /// <tags>moderation, ai-detect, suspicious-users</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="enabled">Включить или выключить</param>
    /// <returns>True если настройка изменена</returns>
    bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled);

    /// <summary>
    /// Получает статистику по подозрительным пользователям
    /// <tags>moderation, statistics, suspicious-users</tags>
    /// </summary>
    /// <returns>Кортеж с общей статистикой</returns>
    (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats();

    /// <summary>
    /// Получает список подозрительных пользователей с включенным AI-детектом
    /// <tags>moderation, ai-detect, suspicious-users</tags>
    /// </summary>
    /// <returns>Список пользователей с AI-детектом</returns>
    List<(long UserId, long ChatId)> GetAiDetectUsers();

    /// <summary>
    /// Проверяет, включен ли AI-детект для пользователя, и отправляет уведомление админам
    /// <tags>moderation, ai-detect, admin-notification</tags>
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="message">Сообщение</param>
    /// <returns>True если AI-детект включен</returns>
    Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message);

    /// <summary>
    /// Снимает ограничения с пользователя и одобряет его
    /// <tags>moderation, user-approval, restrictions</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>True если операция выполнена успешно</returns>
    Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId);

    /// <summary>
    /// Полностью удаляет пользователя из всех списков (подозрительных, одобренных, кэшей)
    /// <tags>moderation, user-cleanup, lists</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    void CleanupUserFromAllLists(long userId, long chatId);

    /// <summary>
    /// Банит пользователя и удаляет его из всех списков
    /// <tags>moderation, user-ban, cleanup</tags>
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageIdToDelete">ID сообщения для удаления (опционально)</param>
    /// <returns>True если операция выполнена успешно</returns>
    Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null);

    /// <summary>
    /// Выполняет действие модерации
    /// <tags>moderation, action-execution</tags>
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="result">Результат модерации</param>
    Task ExecuteModerationActionAsync(Message message, ModerationResult result);
}
