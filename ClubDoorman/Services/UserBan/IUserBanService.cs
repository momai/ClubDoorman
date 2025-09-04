using Telegram.Bot.Types;

namespace ClubDoorman.Services.UserBan;

/// <summary>
/// Сервис для управления банами пользователей (заглушка для совместимости с тестами)
/// </summary>
public interface IUserBanService
{
    /// <summary>
    /// Банит пользователя за длинное имя
    /// </summary>
    Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken);

    /// <summary>
    /// Банит пользователя из блэклиста
    /// </summary>
    Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken);

    /// <summary>
    /// Автоматически банит пользователя за нарушение
    /// </summary>
    /// <param name="message">Сообщение пользователя</param>
    /// <param name="reason">Причина бана</param>
    /// <param name="suppressNotifications">Отключить уведомления в лог-чат (по умолчанию false)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task AutoBanAsync(Message message, string reason, bool suppressNotifications = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Автоматически банит канал
    /// </summary>
    Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обрабатывает бан пользователя из блэклиста
    /// </summary>
    Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken);

    /// <summary>
    /// Отслеживает нарушение и банит пользователя при достижении лимита
    /// </summary>
    Task TrackViolationAndBanIfNeededAsync(Message message, User user, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Централизованный метод для бана пользователя с указанием типа бана
    /// </summary>
    Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, Message? messageToDelete = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Централизованный метод для бана пользователя с указанием типа бана и удалением сообщения по ID
    /// </summary>
    Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, long? messageIdToDelete = null, long? chatIdForMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сообщение по ID в указанном чате
    /// </summary>
    Task DeleteMessageByIdAsync(long chatId, int messageId);
}