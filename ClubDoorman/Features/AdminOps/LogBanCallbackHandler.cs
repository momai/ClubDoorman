using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Features.AdminOps;

public sealed class LogBanCallbackHandler : IAdminCallbackHandler
{
    private readonly ILogChatService _logChatService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<LogBanCallbackHandler> _logger;

    public LogBanCallbackHandler(
        ILogChatService logChatService,
        ITelegramBotClientWrapper bot,
        ILogger<LogBanCallbackHandler> logger)
    {
        _logChatService = logChatService;
        _bot = bot;
        _logger = logger;
    }

    public string Prefix => "logban";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        var parts = context.Payload.Split('_');
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], out var chatId) ||
            !long.TryParse(parts[1], out var userId))
        {
            return AdminCallbackResult.Invalid();
        }

        var message = context.Message;
        try
        {
            await _logChatService.HandleLogBanAsync(
                chatId,
                userId,
                context.AdminDisplayName,
                cancellationToken);

            var updatedText = $"{message.Text}\n\n🚫 Забанен администратором {context.AdminDisplayName}\n🧹 Пользователь очищен из всех списков";
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                updatedText,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Пользователь {UserId} забанен из лог-чата администратором {AdminName}",
                userId,
                context.AdminDisplayName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось забанить пользователя через лог-чат callback");
            var errorText = $"{message.Text}\n\n❌ Ошибка при бане администратором {context.AdminDisplayName}\nНе могу забанить. Не хватает могущества? Сходите забаньте руками";
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                errorText,
                cancellationToken: cancellationToken);
        }

        return AdminCallbackResult.Handled();
    }
}
