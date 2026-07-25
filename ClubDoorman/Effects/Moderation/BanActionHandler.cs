using ClubDoorman.Models;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class BanActionHandler : IModerationActionHandler
{
    private readonly IUserBanService _userBanService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly ILogger<BanActionHandler> _logger;

    public BanActionHandler(
        IUserBanService userBanService,
        IUserFlowLogger userFlowLogger,
        ILogger<BanActionHandler> logger)
    {
        _userBanService = userBanService;
        _userFlowLogger = userFlowLogger;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Ban;

    public async Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        var reason = context.Result.Reason;
        _logger.LogInformation("Бан пользователя: {Reason}", reason);
        _userFlowLogger.LogUserBanned(context.User, context.Chat, reason);
        await _userBanService.AutoBanAsync(context.Message, reason, cancellationToken);
    }
}
