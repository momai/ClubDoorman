using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Ban;

/// <summary>
/// Эффект для бана пользователя
/// <tags>effects, ban, moderation</tags>
/// </summary>
public class BanUserEffect : IEffect
{
    private readonly IUserBanService _userBanService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly ILogger<BanUserEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly Chat _chat;
    private readonly string _reason;

    public BanUserEffect(
        IUserBanService userBanService,
        IUserFlowLogger userFlowLogger,
        ILogger<BanUserEffect> logger,
        Message message,
        User user,
        Chat chat,
        string reason)
    {
        _userBanService = userBanService;
        _userFlowLogger = userFlowLogger;
        _logger = logger;
        _message = message;
        _user = user;
        _chat = chat;
        _reason = reason;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _userFlowLogger.LogUserBanned(_user, _chat, _reason);
        await _userBanService.AutoBanAsync(_message, _reason, suppressNotifications: false, ct);
    }
}
