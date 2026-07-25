using ClubDoorman.Models;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class ManualReviewActionHandler : IModerationActionHandler
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ManualReviewActionHandler> _logger;

    public ManualReviewActionHandler(
        INotificationService notificationService,
        ILogger<ManualReviewActionHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.RequireManualReview;

    public Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Требует ручной проверки: {Reason}", context.Result.Reason);
        return _notificationService.DontDeleteButReportMessage(
            context.Message,
            context.User,
            context.IsSilentMode,
            cancellationToken);
    }
}
