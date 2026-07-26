using ClubDoorman.Models;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class DeleteActionHandler : IModerationActionHandler
{
    private readonly INotificationService _notificationService;
    private readonly IUserBanService _userBanService;
    private readonly ILogger<DeleteActionHandler> _logger;

    public DeleteActionHandler(
        INotificationService notificationService,
        IUserBanService userBanService,
        ILogger<DeleteActionHandler> logger)
    {
        _notificationService = notificationService;
        _userBanService = userBanService;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Delete;

    public async Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        var reason = context.Result.Reason;
        _logger.LogInformation("Удаление сообщения: {Reason}", reason);

        if (reason.Contains("Ссылки запрещены") || reason.Contains("Банальное приветствие"))
        {
            await _notificationService.DeleteAndReportToLogChat(
                context.Message,
                reason,
                cancellationToken);
        }
        else
        {
            await _notificationService.DeleteAndReportMessage(
                context.Message,
                reason,
                context.IsSilentMode,
                cancellationToken);
        }

        await _userBanService.TrackViolationAndBanIfNeededAsync(
            context.Message,
            context.User,
            reason,
            cancellationToken);
    }
}
