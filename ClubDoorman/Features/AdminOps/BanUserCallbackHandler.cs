using System.Runtime.Caching;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

public sealed class BanUserCallbackHandler : IAdminCallbackHandler
{
    private readonly IBadMessageManager _badMessageManager;
    private readonly IUserBanService _userBanService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<BanUserCallbackHandler> _logger;

    public BanUserCallbackHandler(
        IBadMessageManager badMessageManager,
        IUserBanService userBanService,
        ITelegramBotClientWrapper bot,
        ILogger<BanUserCallbackHandler> logger)
    {
        _badMessageManager = badMessageManager;
        _userBanService = userBanService;
        _bot = bot;
        _logger = logger;
    }

    public string Prefix => "ban";

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

        var userMessage = MemoryCache.Default.Remove($"ban_{chatId}_{userId}") as Message;
        var text = userMessage?.Caption ?? userMessage?.Text;
        if (!string.IsNullOrWhiteSpace(text))
            await _badMessageManager.MarkAsBad(text);

        var message = context.Message;
        try
        {
            await _userBanService.BanUserAsync(
                new Chat { Id = chatId },
                new User { Id = userId },
                BanTypeEnum.ManualBan,
                "Ручной бан",
                userMessage,
                cancellationToken);

            var updatedText = $"{message.Text}\n\n🚫 Забанен администратором {context.AdminDisplayName}\n🧹 Пользователь очищен из всех списков\n📝 Сообщение добавлено в список авто-бана";
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                updatedText,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Пользователь {UserId} забанен администратором {AdminName}",
                userId,
                context.AdminDisplayName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось забанить пользователя через админский callback");
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
