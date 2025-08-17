using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Services.AI;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик сообщений - упрощенная версия без фасадов
/// </summary>
public class MessageHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly IAppConfig _appConfig;
    private readonly IUserBanService _userBanService;
    private readonly IChannelModerationService _channelModerationService;
    private readonly ICommandRouter _commandRouter;
    private readonly IUserJoinPolicy _userJoinPolicy; // Прямой сервис вместо фасада
    private readonly IModerationPolicy _moderationPolicy; // Прямой сервис вместо фасада
    private readonly ILogger<MessageHandler> _logger;
    private readonly IBotPermissionsService _botPermissionsService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IForwardingService _forwardingService;
    private readonly IAiCascadeService _aiCascadeService;

    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IUserManager userManager,
        IAppConfig appConfig,
        IUserBanService userBanService,
        IChannelModerationService channelModerationService,
        ICommandRouter commandRouter,
        IUserJoinPolicy userJoinPolicy, // Прямой сервис
        IModerationPolicy moderationPolicy, // Прямой сервис
        ILogger<MessageHandler> logger,
        IBotPermissionsService botPermissionsService,
        ICaptchaService captchaService,
        IUserFlowLogger userFlowLogger,
        IForwardingService forwardingService,
        IAiCascadeService aiCascadeService)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _channelModerationService = channelModerationService ?? throw new ArgumentNullException(nameof(channelModerationService));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _userJoinPolicy = userJoinPolicy ?? throw new ArgumentNullException(nameof(userJoinPolicy));
        _moderationPolicy = moderationPolicy ?? throw new ArgumentNullException(nameof(moderationPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _forwardingService = forwardingService ?? throw new ArgumentNullException(nameof(forwardingService));
        _aiCascadeService = aiCascadeService ?? throw new ArgumentNullException(nameof(aiCascadeService));
    }

    public bool CanHandle(Update update)
    {
        return update?.Message != null || update?.EditedMessage != null;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update == null)
        {
            _logger.LogError("HandleAsync: update is null");
            throw new ArgumentNullException(nameof(update));
        }
        
        if (update.Message == null && update.EditedMessage == null)
        {
            _logger.LogError("HandleAsync: update.Message and update.EditedMessage are null");
            throw new ArgumentNullException(nameof(update.Message));
        }

        var message = update.EditedMessage ?? update.Message!;
        var chat = message.Chat;

        _logger.LogDebug("Received message '{MessageText}' in chat {ChatId} (type: {ChatType}, title: {ChatTitle})",
            message.Text, chat.Id, chat.Type, chat.Title);

        var isAdminChat = chat.Id == _appConfig.AdminChatId || chat.Id == _appConfig.LogAdminChatId;
        
        if (!_appConfig.IsChatAllowed(chat.Id) && !isAdminChat)
        {
            _logger.LogDebug("Chat {ChatId} not in whitelist, skipping", chat.Id);
            return;
        }

        if (_appConfig.DisabledChats.Contains(chat.Id))
        {
            _logger.LogDebug("Chat {ChatId} is disabled, skipping", chat.Id);
            return;
        }

        var isSilentMode = await _botPermissionsService.IsSilentModeAsync(chat.Id, cancellationToken);
        
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        if (message.Text?.StartsWith("/") == true)
        {
            _logger.LogDebug("Handling command '{Command}' in chat {ChatId}", message.Text, chat.Id);
            await HandleCommandAsync(message, cancellationToken);
            return;
        }

        if (chat.Type == ChatType.Private)
        {
            _logger.LogDebug("Private chat {ChatId}, only commands processed", chat.Id);
            return;
        }

        if (message.NewChatMembers != null && chat.Id != _appConfig.AdminChatId)
        {
            _logger.LogDebug("Handling new chat members in chat {ChatId}", chat.Id);
            await _userJoinPolicy.HandleNewMembersAsync(message, cancellationToken); // Прямой вызов
            return;
        }

        if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
        {
            _logger.LogDebug("Message about left chat member detected. MessageId: {MessageId}, UserId: {UserId}",
                message.MessageId, message.LeftChatMember.Id);
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Удалено сообщение о бане/исключении пользователя (UserId: {UserId})", message.LeftChatMember.Id);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении (UserId: {UserId})", message.LeftChatMember.Id);
            }
            return;
        }

        if (message.SenderChat != null)
        {
            _logger.LogDebug("Message from channel detected. SenderChatId: {SenderChatId}", message.SenderChat.Id);
            await HandleChannelMessageAsync(message, cancellationToken);
            return;
        }

        _logger.LogDebug("Processing user message in chat {ChatId}, messageId: {MessageId}", chat.Id, message.MessageId);
        await HandleUserMessageAsync(message, isSilentMode, cancellationToken);
    }

    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleCommandAsync called. MessageId: {MessageId}, ChatId: {ChatId}, Text: {Text}",
            message.MessageId, message.Chat.Id, message.Text);

        var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        if (handled)
        {
            _logger.LogDebug("Команда обработана через CommandRouter: {Command} (chatId: {ChatId}, messageId: {MessageId})",
                message.Text?.Split(' ')[0], message.Chat.Id, message.MessageId);
        }
        else
        {
            _logger.LogDebug("CommandRouter не смог обработать команду: {Command} (chatId: {ChatId}, messageId: {MessageId})",
                message.Text?.Split(' ')[0], message.Chat.Id, message.MessageId);
        }
    }

    public async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleChannelMessageAsync called. MessageId: {MessageId}, SenderChatId: {SenderChatId}, ChatId: {ChatId}",
            message.MessageId, message.SenderChat?.Id, message.Chat.Id);
        await _channelModerationService.HandleChannelMessageAsync(message, cancellationToken);
    }

    internal async Task HandleUserMessageAsync(Message message, bool isSilentMode, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleUserMessageAsync called. MessageId: {MessageId}, IsSilentMode: {IsSilentMode}",
            message.MessageId, isSilentMode);
        var user = message.From;
        var chat = message.Chat;

        if (user == null)
        {
            _logger.LogDebug("Игнорируем системное сообщение без пользователя. MessageId: {MessageId}, ChatId: {ChatId}",
                message.MessageId, chat.Id);
            return;
        }

        if (user.IsBot)
        {
            _logger.LogDebug("Игнорируем сообщение от бота {BotId}. MessageId: {MessageId}, ChatId: {ChatId}",
                user.Id, message.MessageId, chat.Id);
            return;
        }

        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Игнорируем системное сообщение о выходе пользователя. MessageId: {MessageId}, LeftUserId: {LeftUserId}, ChatId: {ChatId}",
                message.MessageId, message.LeftChatMember.Id, chat.Id);
            return;
        }

        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        var captchaInfo = _captchaService.GetCaptchaInfo(captchaKey);
        
        if (captchaInfo != null)
        {
            _logger.LogInformation("Удаляем сообщение от пользователя {UserId}, который должен пройти капчу. MessageId: {MessageId}, ChatId: {ChatId}",
                user.Id, message.MessageId, chat.Id);
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Сообщение пользователя {UserId} удалено из-за незавершённой капчи. MessageId: {MessageId}, ChatId: {ChatId}",
                    user.Id, message.MessageId, chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить сообщение от пользователя проходящего капчу. UserId: {UserId}, MessageId: {MessageId}, ChatId: {ChatId}",
                    user.Id, message.MessageId, chat.Id);
            }
            return;
        }

        _logger.LogDebug("🔍 Проверяем пользователя {UserId} по блэклисту lols.bot", user.Id);
        if (await _userManager.InBanlist(user.Id))
        {
            _logger.LogWarning("Пользователь {UserId} найден в блэклисте lols.bot. Применяем бан. ChatId: {ChatId}, MessageId: {MessageId}",
                user.Id, chat.Id, message.MessageId);
            await _userBanService.HandleBlacklistBanAsync(message, user, chat, cancellationToken);
            return;
        }
        _logger.LogDebug("✅ Пользователь {UserId} не найден в блэклисте", user.Id);

        if (_moderationPolicy.IsUserApproved(user.Id, chat.Id)) // Прямой вызов
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию. MessageId: {MessageId}",
                user.Id, chat.Id, message.MessageId);
            return;
        }

        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        var isChannelDiscussion = await _forwardingService.IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";

        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================",
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id,
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        var clubName = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club. UserId: {UserId}, ChatId: {ChatId}", clubName, user.Id, chat.Id);
            return;
        }

        ModerationResult moderationResult;
        try
        {
            moderationResult = await _moderationPolicy.CheckMessageAsync(message); // Прямой вызов
            if (moderationResult == null)
            {
                _logger.LogWarning("HandleUserMessageAsync: moderationResult == null (CheckMessageAsync вернул null). Подставляем RequireManualReview.");
                moderationResult = new ModerationResult(ModerationAction.RequireManualReview, "Null moderation result", 0);
            }
            _logger.LogDebug("Результат модерации: Action={Action}, Reason={Reason}, Confidence={Confidence}",
                moderationResult.Action, moderationResult.Reason, moderationResult.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при модерации сообщения. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}",
                user.Id, chat.Id, message.MessageId);
            moderationResult = new ModerationResult(ModerationAction.RequireManualReview, "Ошибка модерации - требуется ручной анализ", 0);
        }
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);

        if (moderationResult.Action == ModerationAction.Allow)
        {
            _logger.LogTrace("AI анализ профиля для пользователя {UserId} после успешной базовой модерации. ChatId: {ChatId}, MessageId: {MessageId}",
                user.Id, chat.Id, message.MessageId);
            var profileAnalysisResult = await _aiCascadeService.PerformAiProfileAnalysisAsync(message, user, chat, cancellationToken);
            _logger.LogDebug("Результат AI анализа профиля: {ProfileAnalysisResult} (UserId: {UserId}, ChatId: {ChatId})",
                profileAnalysisResult, user.Id, chat.Id);
            if (profileAnalysisResult)
            {
                _logger.LogWarning("Пользователь {UserId} получил ограничения за подозрительный профиль. ChatId: {ChatId}, MessageId: {MessageId}",
                    user.Id, chat.Id, message.MessageId);
                return;
            }
        }

        _logger.LogTrace("Передаём сообщение на финальную обработку в ModerationPolicy. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}, Action: {Action}",
            user.Id, chat.Id, message.MessageId, moderationResult.Action);
        await _moderationPolicy.HandleUserMessageAsync(message, user, chat, moderationResult, isSilentMode, cancellationToken); // Прямой вызов
    }

    public void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DeleteMessageLater called. MessageId: {MessageId}, ChatId: {ChatId}, After: {After}",
            message?.MessageId, message?.Chat?.Id, after);
        if (message == null)
        {
            _logger.LogWarning("DeleteMessageLater: message is null, skipping");
            return;
        }
        if (after == default) after = TimeSpan.FromMinutes(5);
        _logger.LogDebug("Запланировано удаление сообщения {MessageId} в чате {ChatId} через {After}",
            message.MessageId, message.Chat.Id, after);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    if (after > TimeSpan.Zero)
                    {
                        _logger.LogTrace("Ожидание {After} перед удалением сообщения {MessageId} в чате {ChatId}",
                            after, message.MessageId, message.Chat.Id);
                        await Task.Delay(after, cancellationToken);
                    }
                    _logger.LogTrace("Удаляем сообщение {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                    _logger.LogDebug("Сообщение {MessageId} в чате {ChatId} успешно удалено", message.MessageId, message.Chat.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessageLater failed for message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);
                }
            },
            cancellationToken);
    }
}