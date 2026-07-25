using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Features.AdminOps;

public sealed class ApproveUserCallbackHandler : IAdminCallbackHandler
{
    private readonly IUserManager _userManager;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<ApproveUserCallbackHandler> _logger;

    public ApproveUserCallbackHandler(
        IUserManager userManager,
        ITelegramBotClientWrapper bot,
        ILogger<ApproveUserCallbackHandler> logger)
    {
        _userManager = userManager;
        _bot = bot;
        _logger = logger;
    }

    public string Prefix => "approve";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(context.Payload, out var userId))
            return AdminCallbackResult.Invalid();

        await _userManager.Approve(userId);

        var message = context.Message;
        var updatedText = $"{message.Text}\n\n✅ Одобрен администратором {context.AdminDisplayName}\n👤 Пользователь добавлен в список доверенных";
        await _bot.EditMessageText(
            message.Chat.Id,
            message.MessageId,
            updatedText,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Пользователь {UserId} одобрен администратором {AdminName}",
            userId,
            context.AdminDisplayName);
        return AdminCallbackResult.Handled();
    }
}
