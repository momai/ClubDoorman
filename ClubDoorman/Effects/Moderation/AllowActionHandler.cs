using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class AllowActionHandler : IModerationActionHandler
{
    private readonly IModerationPolicy _moderationPolicy;
    private readonly ILogger<AllowActionHandler> _logger;

    public AllowActionHandler(
        IModerationPolicy moderationPolicy,
        ILogger<AllowActionHandler> logger)
    {
        _moderationPolicy = moderationPolicy;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Allow;

    public async Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Сообщение разрешено: {Reason}", context.Result.Reason);

        var aiDetectBlocked = await _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(
            context.User,
            context.Chat,
            context.Message);

        if (!aiDetectBlocked)
        {
            var messageText = context.Message.Text ?? context.Message.Caption ?? "";
            await _moderationPolicy.IncrementGoodMessageCountAsync(
                context.User,
                context.Chat,
                messageText);
        }
    }
}
