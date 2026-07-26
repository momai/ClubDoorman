using ClubDoorman.Models;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class ReportActionHandler : IModerationActionHandler
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReportActionHandler> _logger;

    public ReportActionHandler(
        INotificationService notificationService,
        ILogger<ReportActionHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Report;

    public Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Отправка в админ-чат: {Reason}", context.Result.Reason);
        return _notificationService.DontDeleteButReportMessage(
            context.Message,
            context.User,
            context.IsSilentMode,
            cancellationToken);
    }
}
