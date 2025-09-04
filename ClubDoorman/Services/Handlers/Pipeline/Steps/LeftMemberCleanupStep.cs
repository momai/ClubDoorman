using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// Удаляет сервисные сообщения об исключении/выходе пользователя, если сообщение создано ботом.
/// Повторяет ветку: if (message.LeftChatMember != null && message.From?.Id == _bot.BotId) {...}
/// </summary>
public class LeftMemberCleanupStep : IMessageStep
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<LeftMemberCleanupStep> _logger;

    public int Order => 30;
    public string Name => nameof(LeftMemberCleanupStep);

    public LeftMemberCleanupStep(ITelegramBotClientWrapper bot, IModerationEventPublisher events, ILogger<LeftMemberCleanupStep> logger)
    {
        _bot = bot;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var left = msg.LeftChatMember;
        if (left == null) return StepResult.Continue();
        if (msg.From?.Id != _bot.BotId) return StepResult.Continue();

        _logger.LogDebug("[Pipeline] LeftMemberCleanupStep deleting system left-member message {MessageId} in chat {ChatId}", msg.MessageId, msg.Chat.Id);
        try
        {
            await _bot.DeleteMessage(msg.Chat.Id, msg.MessageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LeftMemberCleanupStep: failed to delete left-member system message {MessageId} in chat {ChatId}", msg.MessageId, msg.Chat.Id);
        }
        context.LeftMemberCleanupHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("left_member_cleanup", Action: "Delete", RuleCode: RuleCode.LeftMemberCleanup));
        return StepResult.StopOk("left-member-cleanup");
    }
}
