using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Features.AdminOps;

public sealed class NoopCallbackHandler : IAdminCallbackHandler
{
    private readonly ITelegramBotClientWrapper _bot;

    public NoopCallbackHandler(ITelegramBotClientWrapper bot)
    {
        _bot = bot;
    }

    public string Prefix => "noop";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(context.Payload))
            return AdminCallbackResult.Invalid();

        await _bot.EditMessageReplyMarkup(
            context.Message.Chat.Id,
            context.Message.MessageId,
            cancellationToken: cancellationToken);
        return AdminCallbackResult.Handled();
    }
}
