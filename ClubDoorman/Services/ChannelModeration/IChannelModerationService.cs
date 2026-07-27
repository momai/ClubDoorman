using Telegram.Bot.Types;

namespace ClubDoorman.Services.ChannelModeration;

/// <summary>
/// Сервис для модерации каналов
/// <tags>channel, moderation, proxy</tags>
/// </summary>
public interface IChannelModerationService
{
    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleChannelMessageAsync(
        Message message,
        bool isSilentMode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, является ли чат обсуждением данного канала
    /// <tags>channel, discussion, linked</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если чат является обсуждением канала</returns>
    Task<bool> IsChannelDiscussionAsync(Message message, CancellationToken cancellationToken = default);

}
