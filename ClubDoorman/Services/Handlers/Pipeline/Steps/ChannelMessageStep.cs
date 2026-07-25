using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.ChannelModeration;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// Обрабатывает сообщения от каналов (SenderChat != null) через ChannelModerationService.
/// Повторяет ветку channel_message.
/// </summary>
public class ChannelMessageStep : IMessageStep
{
    private readonly IChannelModerationService _channelModerationService;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<ChannelMessageStep> _logger;

    public int Order => 40;
    public string Name => nameof(ChannelMessageStep);

    public ChannelMessageStep(IChannelModerationService channelModerationService, IModerationEventPublisher events, ILogger<ChannelMessageStep> logger)
    {
        _channelModerationService = channelModerationService;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        if (msg.SenderChat == null) return StepResult.Continue();

        _logger.LogDebug("[Pipeline] ChannelMessageStep handling channel message {MessageId} senderChatId={SenderChatId}", msg.MessageId, msg.SenderChat.Id);
        try
        {
            await _channelModerationService.HandleChannelMessageAsync(msg, context.IsSilentMode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChannelMessageStep failed for senderChatId={SenderChatId}", msg.SenderChat.Id);
            return StepResult.Fail(ex, "channel-message-exception");
        }
        context.ChannelMessageHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("channel_message", Action: null, RuleCode: RuleCode.ChannelMessage));
        return StepResult.StopOk("channel-message");
    }
}
