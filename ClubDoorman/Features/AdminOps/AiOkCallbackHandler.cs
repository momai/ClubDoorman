using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Features.AdminOps;

public sealed class AiOkCallbackHandler : IAdminCallbackHandler
{
    private readonly IAiChecks _aiChecks;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly ILogger<AiOkCallbackHandler> _logger;

    public AiOkCallbackHandler(
        IAiChecks aiChecks,
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        ILogger<AiOkCallbackHandler> logger)
    {
        _aiChecks = aiChecks;
        _bot = bot;
        _messageService = messageService;
        _logger = logger;
    }

    public string Prefix => "aiOk";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        var parts = context.Payload.Split('_');
        long? chatId = null;
        long userId;

        if (parts.Length == 1 && long.TryParse(parts[0], out userId))
        {
            // Legacy aiOk_{userId} only marks the profile as safe.
        }
        else if (parts.Length == 2 &&
                 long.TryParse(parts[0], out var parsedChatId) &&
                 long.TryParse(parts[1], out userId))
        {
            chatId = parsedChatId;
        }
        else
        {
            return AdminCallbackResult.Invalid();
        }

        _aiChecks.MarkUserOkay(userId);

        var resultText = $"✅ {context.AdminDisplayName} отметил профиль как безопасный - AI проверки отключены для этого пользователя";
        if (chatId.HasValue)
        {
            try
            {
                await _bot.RestrictChatMember(
                    chatId.Value,
                    userId,
                    new ChatPermissions
                    {
                        CanSendMessages = true,
                        CanSendAudios = true,
                        CanSendDocuments = true,
                        CanSendPhotos = true,
                        CanSendVideos = true,
                        CanSendVideoNotes = true,
                        CanSendVoiceNotes = true,
                        CanSendPolls = true,
                        CanSendOtherMessages = true,
                        CanAddWebPagePreviews = true,
                        CanChangeInfo = false,
                        CanInviteUsers = false,
                        CanPinMessages = false,
                        CanManageTopics = false
                    },
                    cancellationToken: cancellationToken);

                resultText += " + ограничения сняты";
                _logger.LogInformation(
                    "Ограничения сняты с пользователя {UserId} в чате {ChatId} администратором {AdminName}",
                    userId,
                    chatId.Value,
                    context.AdminDisplayName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Не удалось снять ограничения с пользователя {UserId} в чате {ChatId}",
                    userId,
                    chatId.Value);
                resultText += " (не удалось снять ограничения - возможно, недостаточно прав)";
            }
        }

        var message = context.Message;
        try
        {
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                $"{message.Text}\n\n{resultText}",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отредактировать сообщение с результатом AI анализа");
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.UserApproved,
                new SimpleNotificationData(context.CallbackQuery.From, message.Chat, resultText),
                cancellationToken);
            await _bot.EditMessageReplyMarkup(
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken);
        }

        return AdminCallbackResult.Handled();
    }
}
