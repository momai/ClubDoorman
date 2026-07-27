using ClubDoorman.Effects.Channel;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services.ChannelModeration;

public interface IChannelModerationReporter
{
    Task ReportAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken);
}

public sealed class ChannelModerationReporter : IChannelModerationReporter
{
    private const int MaxMessageExcerptLength = 1000;
    private readonly IMessageService _messageService;
    private readonly ILogger<ChannelModerationReporter> _logger;

    public ChannelModerationReporter(
        IMessageService messageService,
        ILogger<ChannelModerationReporter> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    public async Task ReportAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var message = context.Content.Message;
        var messageText = CreateExcerpt(message.Text ?? message.Caption ?? "[медиа]");
        var data = new ChannelMessageNotificationData(
            context.SenderChat,
            context.Content.DestinationChat,
            messageText,
            result.Reason,
            message.MessageId,
            context.IsSilentMode);

        _logger.LogInformation(
            "Отправляем сообщение от channel identity {SenderChatId} в админ-чат: {Reason}",
            context.SenderChat.Id,
            result.Reason);

        var forwarded = await _messageService.ForwardToAdminWithNotificationAsync(
            message,
            AdminNotificationType.ChannelMessage,
            data,
            cancellationToken);

        if (forwarded == null)
        {
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.ChannelMessage,
                data,
                cancellationToken);
        }
    }

    private static string CreateExcerpt(string text) =>
        text.Length <= MaxMessageExcerptLength
            ? text
            : text[..(MaxMessageExcerptLength - 3)] + "...";
}
