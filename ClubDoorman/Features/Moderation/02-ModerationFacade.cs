using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models;
using ClubDoorman.Effects.Moderation;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Фасад для функциональности модерации
/// <tags>moderation, facade, coordination, thin-layer</tags>
/// </summary>
public class ModerationFacade : IModerationFacade
{
    private readonly IModerationPolicy _moderationPolicy;
    private readonly ILogger<ModerationFacade> _logger;
    private readonly IModerationActionDispatcher _moderationActionDispatcher;


    public ModerationFacade(
        IModerationPolicy moderationPolicy,
        ILogger<ModerationFacade> logger,
        IModerationActionDispatcher moderationActionDispatcher)
    {
        _moderationPolicy = moderationPolicy;
        _logger = logger;
        _moderationActionDispatcher = moderationActionDispatcher;
    }

    public Task<ModerationResult> CheckMessageAsync(Message message)
    {
        return _moderationPolicy.CheckMessageAsync(message);
    }

    public Task<ModerationResult> CheckUserNameAsync(User user)
    {
        return _moderationPolicy.CheckUserNameAsync(user);
    }

    public Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        return _moderationPolicy.IncrementGoodMessageCountAsync(user, chat, messageText);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return _moderationPolicy.IsUserApproved(userId, chatId);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return _moderationPolicy.SetAiDetectForSuspiciousUser(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return _moderationPolicy.GetSuspiciousUsersStats();
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return _moderationPolicy.GetAiDetectUsers();
    }

    public Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        return _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
    }

    public Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        return _moderationPolicy.UnrestrictAndApproveUserAsync(userId, chatId);
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _moderationPolicy.CleanupUserFromAllLists(userId, chatId);
    }

    public Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        return _moderationPolicy.BanAndCleanupUserAsync(userId, chatId, messageIdToDelete);
    }

    public Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        return _moderationPolicy.ExecuteModerationActionAsync(message, result);
    }

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
    public Task HandleUserMessageAsync(
        Message message,
        User user,
        Chat chat,
        ModerationResult moderationResult,
        bool isSilentMode,
        CancellationToken cancellationToken)
    {
        var context = new ModerationActionContext(
            message,
            user,
            chat,
            moderationResult,
            isSilentMode);

        return _moderationActionDispatcher.DispatchAsync(context, cancellationToken);
    }
}
