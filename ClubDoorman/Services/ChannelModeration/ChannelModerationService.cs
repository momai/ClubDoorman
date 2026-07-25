using ClubDoorman.Effects.Channel;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services.ChannelModeration;

public sealed class ChannelModerationService : IChannelModerationService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IContentModerationPolicy _contentPolicy;
    private readonly IChannelModerationActionDispatcher _actionDispatcher;
    private readonly ILogger<ChannelModerationService> _logger;
    private readonly IAppConfig _appConfig;

    public ChannelModerationService(
        ITelegramBotClientWrapper bot,
        IContentModerationPolicy contentPolicy,
        IChannelModerationActionDispatcher actionDispatcher,
        ILogger<ChannelModerationService> logger,
        IAppConfig appConfig)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _contentPolicy = contentPolicy ?? throw new ArgumentNullException(nameof(contentPolicy));
        _actionDispatcher = actionDispatcher ?? throw new ArgumentNullException(nameof(actionDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    }

    public async Task HandleChannelMessageAsync(
        Message message,
        bool isSilentMode,
        CancellationToken cancellationToken = default)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat ??
                         throw new ArgumentException("Channel message must have SenderChat", nameof(message));

        var isAnonymousGroupAdmin =
            chat.Type == ChatType.Supergroup && senderChat.Id == chat.Id;
        if (isAnonymousGroupAdmin)
            return;

        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
            return;

        if (await IsChannelDiscussionAsync(message, cancellationToken))
            return;

        var input = new ContentModerationInput(
            message,
            chat,
            message.Text ?? message.Caption);
        var context = new ChannelModerationContext(input, senderChat, isSilentMode);

        if (_appConfig.ChannelAutoBan)
        {
            await _actionDispatcher.DispatchAsync(
                context,
                new ModerationResult(ModerationAction.Ban, "Автобан сообщений от channel identity"),
                cancellationToken);
            return;
        }

        ModerationResult result;
        try
        {
            result = await _contentPolicy.CheckContentAsync(input, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Ошибка content-модерации channel identity {SenderChatId}; отправляем на ручную проверку",
                senderChat.Id);
            result = new ModerationResult(
                ModerationAction.RequireManualReview,
                "Ошибка content-модерации channel identity",
                0);
        }

        await _actionDispatcher.DispatchAsync(context, result, cancellationToken);
    }

    public async Task<bool> IsChannelDiscussionAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat!;

        if (chat.Type == ChatType.Supergroup && message.IsAutomaticForward)
            return true;

        try
        {
            var chatFullInfo = await _bot.GetChatFullInfo(chat.Id, cancellationToken);
            return chatFullInfo.LinkedChatId == senderChat.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Не удалось получить ChatFullInfo для чата {ChatId}", chat.Id);
            return false;
        }
    }
}
