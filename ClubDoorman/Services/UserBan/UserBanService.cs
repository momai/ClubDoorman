using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Moderation;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services.UserBan;

/// <summary>
/// Сервис для управления банами пользователей
/// 
/// ПРИМЕЧАНИЕ: CaptchaService НЕ мигрирован на UserBanService по следующим причинам:
/// - Автоматический разбан критически важен для капчи (через 20 минут)
/// - UserBanService не поддерживает автоматический разбан
/// - Логика капчи тесно связана с баном за неудачу
/// - Принцип единственной ответственности не нарушается
/// - CaptchaService остается самодостаточным
/// </summary>
public class UserBanService : IUserBanService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly ILogger<UserBanService> _logger;
    private readonly IViolationTracker _violationTracker;
    private readonly IAppConfig _appConfig;
    private readonly IStatisticsService _statisticsService;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IUserManager _userManager;
    private readonly IUserCleanupService _userCleanupService;

    public UserBanService(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IUserFlowLogger userFlowLogger,
        ILogger<UserBanService> logger,
        IViolationTracker violationTracker,
        IAppConfig appConfig,
        IStatisticsService statisticsService,
        GlobalStatsManager globalStatsManager,
        IUserManager userManager,
        IUserCleanupService userCleanupService)
    {
        _bot = bot;
        _messageService = messageService;
        _userFlowLogger = userFlowLogger;
        _logger = logger;
        _violationTracker = violationTracker;
        _appConfig = appConfig;
        _statisticsService = statisticsService;
        _globalStatsManager = globalStatsManager;
        _userManager = userManager;
        _userCleanupService = userCleanupService;
    }

    public async Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage?.Chat!;

            if (!await ValidateBanOperationAsync(chat, user, "Бан за длинное имя", cancellationToken))
                return;

            await BanUserAsync(chat, user, banDuration, cancellationToken: cancellationToken);
            await DeleteMessageAsync(userJoinMessage, cancellationToken: cancellationToken);
            var banType = banDuration.HasValue ? "Автобан на 10 минут" : "🚫 Перманентный бан";
            var banData = new AutoBanNotificationData(user, chat, banType, reason, userJoinMessage?.MessageId);
            await SendNotificationAsync(banData, LogNotificationType.BanForLongName, userJoinMessage, cancellationToken: cancellationToken);

            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя за длинное имя");
        }
    }

    public async Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage.Chat;

            if (!await ValidateBanOperationAsync(chat, user, "Бан из блэклиста", cancellationToken))
                return;

            // Баним пользователя на 4 часа с revokeMessages: true
            await BanUserAsync(chat, user, TimeSpan.FromMinutes(240), revokeMessages: true, cancellationToken: cancellationToken);

            // Удаляем сообщение о входе в чат
            await DeleteMessageAsync(userJoinMessage, cancellationToken: cancellationToken);

            // Удаляем из списка одобренных (как в старом коде)
            if (_userCleanupService.RemoveUserFromAllApprovals(user.Id, "Бан по блэклисту"))
            {
                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.UserCleanup,
                    new UserCleanupNotificationData(user, chat, $"Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после бана по блеклисту")
                );
            }

            // Логируем успех
            _logger.LogInformation("Пользователь {User} (id={UserId}) из блэклиста забанен на 4 часа в чате {ChatTitle} (id={ChatId})",
                FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id);

            // Обновляем статистику
            _statisticsService.IncrementBlacklistBan(chat.Id);
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");

            _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
            throw; // Re-throw для сохранения оригинального поведения
        }
    }

    public async Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;

        if (!await ValidateBanOperationAsync(chat, user, reason, cancellationToken))
            return;

        var autoBanData = CreateAutoBanData(user, message, reason);
        var logNotificationType = DetermineLogNotificationType(reason);

        await SendNotificationAsync(autoBanData, logNotificationType, withErrorHandling: true, cancellationToken: cancellationToken);
        await DeleteMessageAsync(message, withErrorHandling: true, cancellationToken: cancellationToken);
        await BanUserPermanentlyAsync(message, user, cancellationToken);
        await CleanupUserDataAsync(user, chat, cancellationToken);
    }

    public async Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat!;
        var failures = new List<string>();

        try
        {
            await _bot.DeleteMessage(chat, message.MessageId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Не удалось удалить сообщение перед баном channel identity {SenderChatId}", senderChat.Id);
            failures.Add("не удалось удалить сообщение");
        }

        try
        {
            await _bot.BanChatSenderChat(chat, senderChat.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Не удалось забанить channel identity {SenderChatId}", senderChat.Id);
            failures.Add("не удалось забанить channel identity");
        }

        if (failures.Count > 0)
        {
            var errorData = new ErrorNotificationData(
                new InvalidOperationException(string.Join("; ", failures)),
                "Ошибка enforcement для channel identity",
                null,
                chat);
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.ChannelError, errorData, cancellationToken);
        }
    }

    public async Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        await LogBlacklistBanAttemptAsync(message, user, chat);
        await SendBlacklistBanNotificationAsync(message, user, chat, cancellationToken);
        await DeleteMessageSafelyAsync(message, cancellationToken);
        await BanUserAsync(chat, user, TimeSpan.FromMinutes(240), revokeMessages: true, withErrorHandling: true, "Не удалось забанить пользователя из блэклиста", cancellationToken);
        await UpdateBlacklistStatisticsAsync(message, chat);
        await RemoveUserFromApprovedAsync(user, message, chat, cancellationToken);
        await LogBlacklistBanSuccessAsync(user, chat);
    }

    public async Task TrackViolationAndBanIfNeededAsync(Message message, User user, string reason, CancellationToken cancellationToken)
    {
        try
        {
            // Определяем тип нарушения по причине
            ViolationType? violationType = reason switch
            {
                var r when r.Contains("ML решил что это спам") => ViolationType.MlSpam,
                var r when r.Contains("стоп-слова") => ViolationType.StopWords,
                var r when r.Contains("многовато эмоджи") => ViolationType.TooManyEmojis,
                var r when r.Contains("lookalike") => ViolationType.LookalikeSymbols,
                var r when r.Contains("Банальное приветствие") => ViolationType.BoringGreetings,
                var r when r.Contains("непройденная капча") => ViolationType.CaptchaFailed,
                _ => null
            };

            if (violationType == null)
            {
                _logger.LogDebug("Неизвестный тип нарушения: {Reason}", reason);
                return;
            }

            // Регистрируем нарушение
            var shouldBan = _violationTracker.RegisterViolation(user.Id, message.Chat.Id, violationType.Value);

            if (shouldBan)
            {
                _logger.LogWarning("Достигнут лимит нарушений {ViolationType} для пользователя {UserId} в чате {ChatId} - бан",
                    ViolationTracker.GetViolationTypeName(violationType.Value), user.Id, message.Chat.Id);

                // Баним пользователя за повторные нарушения
                var banReason = $"Повторные нарушения: {ViolationTracker.GetViolationTypeName(violationType.Value)}";
                await AutoBanAsync(message, banReason, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отслеживании нарушений для пользователя {UserId}", user.Id);
        }
    }

    /// <summary>
    /// Основной метод бана пользователя с использованием enum типов
    /// </summary>
    public async Task BanUserAsync(
        Chat chat,
        User user,
        BanTypeEnum banType,
        string? customReason = null,
        Message? messageToDelete = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (duration, reason) = GetBanConfiguration(banType, customReason);

            if (!await ValidateBanOperationAsync(chat, user, reason, cancellationToken))
                return;

            await BanUserAsync(chat, user, duration, cancellationToken: cancellationToken);
            await DeleteMessageAsync(messageToDelete, cancellationToken: cancellationToken);

            var banData = new AutoBanNotificationData(user, chat, GetBanTypeDescription(banType), reason, messageToDelete?.MessageId);
            await SendNotificationAsync(banData, GetNotificationType(banType), messageToDelete, cancellationToken: cancellationToken);

            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя типа {BanTypeEnum}", banType);
            throw; // Re-throw для сохранения оригинального поведения
        }
    }

    public async Task BanUserAsync(
        Chat chat,
        User user,
        BanTypeEnum banType,
        string? customReason = null,
        long? messageIdToDelete = null,
        long? chatIdForMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (duration, reason) = GetBanConfiguration(banType, customReason);

            if (!await ValidateBanOperationAsync(chat, user, reason, cancellationToken))
                return;

            await BanUserAsync(chat, user, duration, cancellationToken: cancellationToken);

            // Удаляем сообщение по ID, если указано
            if (messageIdToDelete.HasValue && chatIdForMessage.HasValue)
            {
                await DeleteMessageByIdAsync(chatIdForMessage.Value, (int)messageIdToDelete.Value, withErrorHandling: false, cancellationToken);
            }

            var banData = new AutoBanNotificationData(user, chat, GetBanTypeDescription(banType), reason, messageIdToDelete);
            await SendNotificationAsync(banData, GetNotificationType(banType), null, cancellationToken: cancellationToken);

            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя типа {BanTypeEnum}", banType);
            throw; // Re-throw для сохранения оригинального поведения
        }
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type switch
        {
            ChatType.Supergroup => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}",
            ChatType.Group when !string.IsNullOrEmpty(chat.Username) => $"https://t.me/{chat.Username}/{messageId}",
            _ => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}"
        };

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    // Приватные вспомогательные методы

    private async Task<bool> ValidateBanOperationAsync(Chat chat, User user, string operation, CancellationToken cancellationToken)
    {
        if (chat.Type == ChatType.Private)
        {
            // Сохраняем оригинальные сообщения для совместимости с тестами
            var logMessage = operation switch
            {
                "Бан за длинное имя" => $"Попытка бана за длинное имя в приватном чате {chat.Id} - операция невозможна",
                "Бан из блэклиста" => $"Попытка бана из блэклиста в приватном чате {chat.Id} - операция невозможна",
                _ => $"Попытка бана в приватном чате {chat.Id} - операция невозможна"
            };

            _logger.LogWarning(logMessage);
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Попытка бана в приватном чате"),
                operation,
                user,
                chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
            return false;
        }
        return true;
    }



    private async Task DeleteMessageAsync(Message? message, bool withErrorHandling = false, CancellationToken cancellationToken = default)
    {
        if (message == null) return;

        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (withErrorHandling)
        {
            _logger.LogWarning(ex, "Не удалось удалить сообщение {MessageId} из чата {ChatId} (возможно, уже удалено)", message.MessageId, message.Chat.Id);
        }
    }

    private async Task DeleteMessageByIdAsync(long chatId, int messageId, bool withErrorHandling = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _bot.DeleteMessage(chatId, messageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (withErrorHandling)
        {
            _logger.LogWarning(ex, "Не удалось удалить сообщение {MessageId} из чата {ChatId} (возможно, уже удалено)", messageId, chatId);
        }
    }

    private async Task BanUserAsync(Chat chat, User user, TimeSpan? banDuration, bool revokeMessages = true, bool withErrorHandling = false, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime? banUntil = banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null;
            await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: revokeMessages, cancellationToken: cancellationToken);
        }
        catch (Exception e) when (withErrorHandling)
        {
            _logger.LogWarning(e, errorMessage ?? "Не удалось забанить пользователя");
        }
    }

    private async Task SendNotificationAsync(AutoBanNotificationData banData, LogNotificationType logType, Message? message = null, bool withErrorHandling = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message != null)
            {
                await _messageService.ForwardToLogWithNotificationAsync(message, logType, banData, cancellationToken);
            }
            else if (_appConfig.RepeatedViolationsBanToAdminChat)
            {
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.AutoBan, banData, cancellationToken);
            }
            else
            {
                await _messageService.SendLogNotificationAsync(logType, banData, cancellationToken);
            }
        }
        catch (Exception ex) when (withErrorHandling)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления о бане типа {NotificationType}", logType);
        }
    }

    private AutoBanNotificationData CreateAutoBanData(User user, Message message, string reason) =>
        new AutoBanNotificationData(
            user,
            message.Chat,
            "Автобан",
            reason,
            message.MessageId,
            LinkToMessage(message.Chat, message.MessageId)
        );

    private LogNotificationType DetermineLogNotificationType(string reason) =>
        reason switch
        {
            var r when r.Contains("Известное спам-сообщение") => LogNotificationType.AutoBanKnownSpam,
            var r when r.Contains("Ссылки запрещены") => LogNotificationType.AutoBanTextMention,
            var r when r.Contains("Повторные нарушения") => LogNotificationType.AutoBanRepeatedViolations,
            _ => LogNotificationType.AutoBanBlacklist
        };

    private async Task BanUserPermanentlyAsync(Message message, User user, CancellationToken cancellationToken)
    {
        try
        {
            await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: cancellationToken);
            _logger.LogInformation("✅ Пользователь {UserId} успешно забанен в чате {ChatId}", user.Id, message.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при бане пользователя {UserId} в чате {ChatId}", user.Id, message.Chat.Id);
        }
    }

    private async Task CleanupUserDataAsync(User user, Chat chat, CancellationToken cancellationToken)
    {
        _userCleanupService.RemoveUserFromGroupApproval(user.Id, chat.Id, "Очистка при бане");
        _violationTracker.ResetViolations(user.Id, chat.Id, ViolationType.MlSpam);
        _violationTracker.ResetViolations(user.Id, chat.Id, ViolationType.StopWords);
        _violationTracker.ResetViolations(user.Id, chat.Id, ViolationType.TooManyEmojis);
        _violationTracker.ResetViolations(user.Id, chat.Id, ViolationType.LookalikeSymbols);

        _logger.LogInformation("🧹 Счетчики нарушений сброшены для пользователя {UserId} в чате {ChatId}", user.Id, chat.Id);
    }

    private async Task LogBlacklistBanAttemptAsync(Message message, User user, Chat chat)
    {
        var userMessageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _logger.LogWarning("🚫 БЛЭКЛИСТ LOLS.BOT: {UserName} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) написал: {MessageText}",
            FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id,
            userMessageText.Length > 100 ? userMessageText.Substring(0, 100) + "..." : userMessageText);

        _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot");
    }

    private async Task SendBlacklistBanNotificationAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        try
        {
            var blacklistData = new AutoBanNotificationData(
                user,
                message.Chat,
                "Автобан по блэклисту lols.bot",
                "первое сообщение",
                message.MessageId,
                LinkToMessage(message.Chat, message.MessageId)
            );

            // Пересылаем сообщение и отправляем уведомление как реплай
            var forwardedMessage = await _bot.ForwardMessage(
                new ChatId(_appConfig.LogAdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );

            var template = _messageService.GetTemplates().GetLogTemplate(LogNotificationType.AutoBanBlacklist);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, blacklistData);

            await _bot.SendMessage(
                _appConfig.LogAdminChatId,
                messageText,
                parseMode: ParseMode.Html,
                replyParameters: forwardedMessage,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение в лог-чат");
        }
    }

    private async Task DeleteMessageSafelyAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить сообщение пользователя из блэклиста");
        }
    }



    private async Task UpdateBlacklistStatisticsAsync(Message message, Chat chat)
    {
        _statisticsService.IncrementBlacklistBan(message.Chat.Id);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
    }

    private async Task RemoveUserFromApprovedAsync(User user, Message message, Chat chat, CancellationToken cancellationToken)
    {
        if (_userCleanupService.RemoveUserFromGlobalApproval(user.Id, "Автобан по блэклисту"))
        {
            try
            {
                var removedData = new SimpleNotificationData(user, message.Chat, "удален из списка одобренных после автобана по блэклисту");
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.RemovedFromApproved, removedData, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось отправить уведомление об удалении из одобренных");
            }
        }
    }

    private async Task LogBlacklistBanSuccessAsync(User user, Chat chat)
    {
        _logger.LogInformation("✅ АВТОБАН ЗАВЕРШЕН: пользователь {User} (id={UserId}) забанен на 4 часа в чате '{ChatTitle}' (id={ChatId}) по блэклисту lols.bot",
            FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id);
    }

    // Вспомогательные методы для работы с enum BanTypeEnum
    private (TimeSpan? duration, string reason) GetBanConfiguration(BanTypeEnum banType, string? customReason)
    {
        return banType switch
        {
            BanTypeEnum.LongName => (null, customReason ?? "Длинное имя пользователя"),
            BanTypeEnum.Blacklist => (TimeSpan.FromMinutes(240), "Пользователь в блэклисте"),
            BanTypeEnum.AutoBan => (null, customReason ?? "Автоматический бан"),
            BanTypeEnum.ManualBan => (null, customReason ?? "Ручной бан"),
            BanTypeEnum.ProfileBan => (null, customReason ?? "Бан по профилю"),
            BanTypeEnum.ChannelBan => (null, customReason ?? "Бан канала"),
            BanTypeEnum.CaptchaBan => (TimeSpan.FromMinutes(10), customReason ?? "Неудачная капча"),
            BanTypeEnum.RepeatedViolation => (TimeSpan.FromMinutes(60), customReason ?? "Повторное нарушение"),
            _ => (null, customReason ?? "Неизвестный тип бана")
        };
    }

    private string GetBanTypeDescription(BanTypeEnum banType)
    {
        return banType switch
        {
            BanTypeEnum.LongName => "🚫 Перманентный бан",
            BanTypeEnum.Blacklist => "🚫 Бан из блэклиста",
            BanTypeEnum.AutoBan => "🚫 Автоматический бан",
            BanTypeEnum.ManualBan => "🚫 Ручной бан",
            BanTypeEnum.ProfileBan => "🚫 Бан по профилю",
            BanTypeEnum.ChannelBan => "🚫 Бан канала",
            BanTypeEnum.CaptchaBan => "Автобан на 10 минут",
            BanTypeEnum.RepeatedViolation => "Автобан на 1 час",
            _ => "🚫 Бан"
        };
    }

    private LogNotificationType GetNotificationType(BanTypeEnum banType)
    {
        return banType switch
        {
            BanTypeEnum.LongName => LogNotificationType.BanForLongName,
            BanTypeEnum.Blacklist => LogNotificationType.BanBlacklistedUser,
            BanTypeEnum.AutoBan => LogNotificationType.AutoBan,
            BanTypeEnum.ManualBan => LogNotificationType.ManualBan,
            BanTypeEnum.ProfileBan => LogNotificationType.ProfileBan,
            BanTypeEnum.ChannelBan => LogNotificationType.ChannelBan,
            BanTypeEnum.CaptchaBan => LogNotificationType.CaptchaBan,
            BanTypeEnum.RepeatedViolation => LogNotificationType.RepeatedViolation,
            _ => LogNotificationType.AutoBan
        };
    }

    public async Task DeleteMessageByIdAsync(long chatId, int messageId)
    {
        await DeleteMessageByIdAsync(chatId, messageId, withErrorHandling: true);
    }
}
