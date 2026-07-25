using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Features.AdminOps;

public sealed class SuspiciousUserCallbackHandler : IAdminCallbackHandler
{
    private readonly IModerationService _moderationService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<SuspiciousUserCallbackHandler> _logger;

    public SuspiciousUserCallbackHandler(
        IModerationService moderationService,
        ITelegramBotClientWrapper bot,
        ILogger<SuspiciousUserCallbackHandler> logger)
    {
        _moderationService = moderationService;
        _bot = bot;
        _logger = logger;
    }

    public string Prefix => "suspicious";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        var parts = context.Payload.Split('_');
        if ((parts.Length != 3 && parts.Length != 4) ||
            !long.TryParse(parts[1], out var userId) ||
            !long.TryParse(parts[2], out var chatId))
        {
            return AdminCallbackResult.Invalid();
        }

        long? messageId = null;
        if (parts.Length == 4)
        {
            if (!long.TryParse(parts[3], out var parsedMessageId))
                return AdminCallbackResult.Invalid();
            messageId = parsedMessageId;
        }

        return parts[0] switch
        {
            "approve" => await ApproveAsync(context, userId, chatId, cancellationToken),
            "ban" => await BanAsync(context, userId, chatId, messageId, cancellationToken),
            "ai" => await ToggleAiAsync(context, userId, chatId, cancellationToken),
            _ => AdminCallbackResult.Invalid()
        };
    }

    private async Task<AdminCallbackResult> ApproveAsync(
        AdminCallbackContext context,
        long userId,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _moderationService.UnrestrictAndApproveUserAsync(userId, chatId);
            var statusText = success
                ? $"{context.Message.Text}\n\n✅ *Разблокирован и одобрен администратором {context.AdminDisplayName}*"
                : $"{context.Message.Text}\n\n⚠️ *Одобрен администратором {context.AdminDisplayName}* (возможны проблемы с разблокировкой)";

            await _bot.EditMessageText(
                context.Message.Chat.Id,
                context.Message.MessageId,
                statusText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Подозрительный пользователь {UserId} разблокирован и одобрен администратором {AdminName}",
                userId,
                context.AdminDisplayName);
            return AdminCallbackResult.Handled();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке callback для подозрительного пользователя {UserId}", userId);
            return AdminCallbackResult.Handled("❌ Произошла ошибка", true);
        }
    }

    private async Task<AdminCallbackResult> BanAsync(
        AdminCallbackContext context,
        long userId,
        long chatId,
        long? messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _moderationService.BanAndCleanupUserAsync(userId, chatId);
            if (context.Message.ReplyToMessage != null)
            {
                try
                {
                    await _bot.DeleteMessage(
                        context.Message.ReplyToMessage.Chat.Id,
                        context.Message.ReplyToMessage.MessageId,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить пересланное сообщение пользователя {UserId}", userId);
                }
            }

            if (messageId.HasValue)
            {
                try
                {
                    await _bot.DeleteMessage(chatId, (int)messageId.Value, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить оригинальное сообщение пользователя {UserId}", userId);
                }
            }

            var statusText = success
                ? $"{context.Message.Text}\n\n🚫 Забанен и очищен администратором {context.AdminDisplayName}"
                : $"{context.Message.Text}\n\n⚠️ Обработан администратором {context.AdminDisplayName} (возможны проблемы с баном)";
            await _bot.EditMessageText(
                context.Message.Chat.Id,
                context.Message.MessageId,
                statusText,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Подозрительный пользователь {UserId} забанен и очищен администратором {AdminName}",
                userId,
                context.AdminDisplayName);
            return AdminCallbackResult.Handled();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось выполнить действие для пользователя {UserId}", userId);
            return AdminCallbackResult.Handled("❌ Не удалось выполнить действие", true);
        }
    }

    private async Task<AdminCallbackResult> ToggleAiAsync(
        AdminCallbackContext context,
        long userId,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            var isEnabled = _moderationService.GetAiDetectUsers()
                .Any(user => user.UserId == userId && user.ChatId == chatId);
            var newStatus = _moderationService.SetAiDetectForSuspiciousUser(userId, chatId, !isEnabled);
            var statusText = newStatus ? "включен" : "выключен";
            var statusEmoji = newStatus ? "🔍✅" : "🔍❌";

            await _bot.EditMessageText(
                context.Message.Chat.Id,
                context.Message.MessageId,
                $"{context.Message.Text}\n\n{statusEmoji} AI детект {statusText} администратором {context.AdminDisplayName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "AI детект для подозрительного пользователя {UserId} {Status} администратором {AdminName}",
                userId,
                statusText,
                context.AdminDisplayName);
            return AdminCallbackResult.Handled();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке callback для подозрительного пользователя {UserId}", userId);
            return AdminCallbackResult.Handled("❌ Произошла ошибка", true);
        }
    }
}
