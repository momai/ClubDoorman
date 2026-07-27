using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;
using Telegram.Bot;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.TextProcessing;
using ClubDoorman.Services;
using ClubDoorman.Services.Core.Configuration;
namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Политика модерации сообщений
/// <tags>moderation, policy, decisions, logic</tags>
/// </summary>
public class ModerationPolicy : IModerationPolicy
{
    private readonly ISpamHamClassifier _classifier;
    private readonly IMimicryClassifier _mimicryClassifier;
    private readonly IContentModerationPolicy _contentModerationPolicy;
    private readonly IUserManager _userManager;
    private readonly IAiChecks _aiChecks;
    private readonly ISuspiciousUsersStorage _suspiciousUsersStorage;
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageService _messageService;
    private readonly IUserBanService _userBanService;
    private readonly IUserCleanupService _userCleanupService;
    private readonly ILogger<ModerationPolicy> _logger;
    private readonly IAppConfig _appConfig;
    // Счетчики хороших сообщений для новой системы
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly ConcurrentDictionary<string, int> _groupGoodUserMessages = new();
    private readonly ConcurrentDictionary<long, DateTime> _warnedUsers = new();
    // Хранение первых сообщений пользователей для анализа мимикрии
    private readonly ConcurrentDictionary<long, List<string>> _userFirstMessages = new();
    private readonly ConcurrentDictionary<string, List<string>> _groupUserFirstMessages = new();

    // Счетчики сообщений для подозрительных пользователей
    private readonly ConcurrentDictionary<long, int> _suspiciousUserMessages = new();
    private readonly ConcurrentDictionary<string, int> _groupSuspiciousUserMessages = new();

    public ModerationPolicy(
        ISpamHamClassifier classifier,
        IMimicryClassifier mimicryClassifier,
        IContentModerationPolicy contentModerationPolicy,
        IUserManager userManager,
        IAiChecks aiChecks,
        ISuspiciousUsersStorage suspiciousUsersStorage,
        ITelegramBotClient botClient,
        IMessageService messageService,
        IUserBanService userBanService,
        IUserCleanupService userCleanupService,
        ILogger<ModerationPolicy> logger,
        IAppConfig appConfig)
    {
        _classifier = classifier;
        _mimicryClassifier = mimicryClassifier;
        _contentModerationPolicy = contentModerationPolicy;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _suspiciousUsersStorage = suspiciousUsersStorage;
        _botClient = botClient;
        _messageService = messageService;
        _userBanService = userBanService;
        _userCleanupService = userCleanupService;
        _logger = logger;
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

        // Логируем статус системы мимикрии
        if (_appConfig.SuspiciousDetectionEnabled)
        {
            _logger.LogInformation("🎭 Система мимикрии ВКЛЮЧЕНА: порог={Threshold:F1}, сообщений для одобрения={Count}",
                _appConfig.MimicryThreshold, _appConfig.SuspiciousToApprovedMessageCount);
        }
        else
        {
            _logger.LogWarning("🎭 Система мимикрии ОТКЛЮЧЕНА: установите DOORMAN_SUSPICIOUS_DETECTION_ENABLE=true для включения");
        }
    }

    /// <summary>
    /// Проверяет сообщение на соответствие правилам модерации
    /// </summary>
    /// <param name="message">Сообщение для проверки</param>
    /// <returns>Результат модерации с рекомендуемым действием и причиной</returns>
    /// <exception cref="ModerationException">Выбрасывается при ошибках во время модерации</exception>
    /// <exception cref="ArgumentNullException">Выбрасывается если message равен null</exception>
    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message), "Сообщение не может быть null");

        if (message.From == null)
            throw new ModerationException("Сообщение должно содержать информацию о пользователе");

        var user = message.From;
        var text = message.Text ?? message.Caption;
        var chat = message.Chat;

        // Кэшируем текст сообщения
        if (text != null)
        {
            MemoryCache.Default.Set(
                new CacheItem($"{chat.Id}_{user.Id}", text),
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
            );
        }

        // 1. Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            return new ModerationResult(ModerationAction.Ban, "Пользователь в блэклисте спамеров");
        }

        return await _contentModerationPolicy.CheckContentAsync(
            new ContentModerationInput(message, chat, text),
            CancellationToken.None);
    }

    /// <summary>
    /// Проверяет имя пользователя на соответствие правилам
    /// </summary>
    /// <param name="user">Пользователь для проверки</param>
    /// <returns>Результат проверки имени пользователя</returns>
    /// <exception cref="ModerationException">Выбрасывается при ошибках во время проверки</exception>
    /// <exception cref="ArgumentNullException">Выбрасывается если user равен null</exception>
    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "Пользователь не может быть null");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            throw new ModerationException("Имя пользователя не может быть пустым");

        var fullName = Utils.FullName(user);

        // Проверяем длину имени
        if (fullName.Length > 75)
        {
            return new ModerationResult(ModerationAction.Ban, $"Экстремально длинное имя ({fullName.Length} символов)");
        }

        if (fullName.Length > 40)
        {
            return new ModerationResult(ModerationAction.Report, $"Подозрительно длинное имя ({fullName.Length} символов)");
        }

        return new ModerationResult(ModerationAction.Allow, "Имя пользователя корректно");
    }

    public Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        // Эта логика пока остается в Worker.cs, будет вынесена позже
        throw new NotImplementedException("Логика выполнения действий будет вынесена в следующих итерациях");
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return _userManager.Approved(userId, chatId);
    }

    /// <summary>
    /// Увеличивает счетчик хороших сообщений пользователя и обрабатывает логику одобрения
    /// </summary>
    /// <param name="user">Пользователь, отправивший сообщение</param>
    /// <param name="chat">Чат, в котором было отправлено сообщение</param>
    /// <param name="messageText">Текст сообщения</param>
    /// <exception cref="ModerationException">Выбрасывается при ошибках во время обработки</exception>
    /// <exception cref="ArgumentNullException">Выбрасывается если user или chat равен null</exception>
    public async Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "Пользователь не может быть null");

        if (chat == null)
            throw new ArgumentNullException(nameof(chat), "Чат не может быть null");

        if (string.IsNullOrWhiteSpace(messageText))
            throw new ArgumentException("Текст сообщения не может быть пустым", nameof(messageText));

        // Проверяем, является ли пользователь подозрительным
        if (_suspiciousUsersStorage.IsSuspicious(user.Id, chat.Id))
        {
            _logger.LogDebug("🔶 Пользователь {User} уже подозрительный, обрабатываем как suspicious", Utils.FullName(user));
            await HandleSuspiciousUserMessage(user, chat);
            return;
        }

        _logger.LogDebug("📊 Система одобрения: GlobalMode={GlobalMode}, User={User}",
            _appConfig.GlobalApprovalMode, Utils.FullName(user));

    if (!_appConfig.GlobalApprovalMode)
        {
            // Групповой режим
            _logger.LogDebug("➡️ Направляем в групповой режим для {User}", Utils.FullName(user));
            await HandleGroupModeMessage(user, chat, messageText);
        }
        else
        {
            // Глобальный режим
            _logger.LogDebug("➡️ Направляем в глобальный режим для {User}", Utils.FullName(user));
            await HandleGlobalModeMessage(user, chat, messageText);
        }
    }

    private async Task HandleSuspiciousUserMessage(User user, Chat chat)
    {
        var groupUserKey = $"{chat.Id}_{user.Id}";
        var suspiciousCount = _groupSuspiciousUserMessages.AddOrUpdate(groupUserKey, 1, (_, oldValue) => oldValue + 1);

        // Обновляем счетчик в storage
        _suspiciousUsersStorage.UpdateMessageCount(user.Id, chat.Id, suspiciousCount);

    if (suspiciousCount >= _appConfig.SuspiciousToApprovedMessageCount)
        {
            _logger.LogInformation(
                "Suspicious user {FullName} behaved well for {Count} additional messages in group {GroupTitle}, approving",
                Utils.FullName(user),
                suspiciousCount,
                chat.Title ?? chat.Id.ToString()
            );

            // Переводим из подозрительных в одобренные
            _suspiciousUsersStorage.RemoveSuspicious(user.Id, chat.Id);
            await _userManager.Approve(user.Id, chat.Id);
            _groupSuspiciousUserMessages.TryRemove(groupUserKey, out _);
            _warnedUsers.TryRemove(user.Id, out _);
        }
    }

    private async Task HandleGroupModeMessage(User user, Chat chat, string messageText)
    {
        var groupUserKey = $"{chat.Id}_{user.Id}";

        // Сохраняем сообщение для анализа мимикрии
        if (!string.IsNullOrWhiteSpace(messageText))
        {
            var messages = _groupUserFirstMessages.GetOrAdd(groupUserKey, _ => new List<string>());
            if (messages.Count < 3)
            {
                messages.Add(messageText.Trim());
            }
        }

        var goodInteractions = _groupGoodUserMessages.AddOrUpdate(groupUserKey, 1, (_, oldValue) => oldValue + 1);

        if (goodInteractions >= 3)
        {
            // Анализируем подозрительность, если система включена
            _logger.LogDebug("🔍 Анализ подозрительности: система включена={SuspiciousEnabled}, пользователь={User}",
                _appConfig.SuspiciousDetectionEnabled, Utils.FullName(user));

            if (_appConfig.SuspiciousDetectionEnabled && await AnalyzeMimicryAndMarkSuspicious(user, chat, groupUserKey))
            {
                // Пользователь помечен как подозрительный
                _groupGoodUserMessages.TryRemove(groupUserKey, out _);
                _groupUserFirstMessages.TryRemove(groupUserKey, out _);
                return;
            }

            // Обычное одобрение
            _logger.LogInformation(
                "User {FullName} behaved well for the last {Count} messages in group {GroupTitle}, approving in this group",
                Utils.FullName(user),
                goodInteractions,
                chat.Title ?? chat.Id.ToString()
            );

            await _userManager.Approve(user.Id, chat.Id);
            _groupGoodUserMessages.TryRemove(groupUserKey, out _);
            _groupUserFirstMessages.TryRemove(groupUserKey, out _);
            _warnedUsers.TryRemove(user.Id, out _);
        }
    }

    private async Task HandleGlobalModeMessage(User user, Chat chat, string messageText)
    {
        // Сохраняем сообщение для анализа мимикрии
        if (!string.IsNullOrWhiteSpace(messageText))
        {
            var messages = _userFirstMessages.GetOrAdd(user.Id, _ => new List<string>());
            if (messages.Count < 3)
            {
                messages.Add(messageText.Trim());
            }
        }

        var goodInteractions = _goodUserMessages.AddOrUpdate(user.Id, 1, (_, oldValue) => oldValue + 1);

        if (goodInteractions >= 3)
        {
            // Анализируем подозрительность, если система включена
            _logger.LogDebug("🔍 Анализ подозрительности (глобальный): система включена={SuspiciousEnabled}, пользователь={User}",
                _appConfig.SuspiciousDetectionEnabled, Utils.FullName(user));

            if (_appConfig.SuspiciousDetectionEnabled && await AnalyzeMimicryAndMarkSuspicious(user, chat, user.Id.ToString()))
            {
                // Пользователь помечен как подозрительный
                _goodUserMessages.TryRemove(user.Id, out _);
                _userFirstMessages.TryRemove(user.Id, out _);
                return;
            }

            // Обычное одобрение
            _logger.LogInformation(
                "User {FullName} behaved well for the last {Count} messages, approving {Mode}",
                Utils.FullName(user),
                goodInteractions,
                _appConfig.GlobalApprovalMode ? "globally" : "in old system"
            );

            await _userManager.Approve(user.Id, _appConfig.GlobalApprovalMode ? null : chat.Id);
            _goodUserMessages.TryRemove(user.Id, out _);
            _userFirstMessages.TryRemove(user.Id, out _);
            _warnedUsers.TryRemove(user.Id, out _);
        }
    }

    /// <summary>
    /// Полностью удаляет пользователя из всех списков (подозрительных, одобренных, кэшей)
    /// </summary>
    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        try
        {
            // Удаляем из подозрительных
            _suspiciousUsersStorage.RemoveSuspicious(userId, chatId);

            // Удаляем из одобренных
            _userCleanupService.RemoveUserFromGroupApproval(userId, chatId, "Очистка из ModerationService");

            // Очищаем кэши сообщений
            var groupUserKey = $"{chatId}_{userId}";
            _goodUserMessages.TryRemove(userId, out _);
            _groupGoodUserMessages.TryRemove(groupUserKey, out _);
            _suspiciousUserMessages.TryRemove(userId, out _);
            _groupSuspiciousUserMessages.TryRemove(groupUserKey, out _);
            _warnedUsers.TryRemove(userId, out _);

            // Очищаем кэши первых сообщений
            _userFirstMessages.TryRemove(userId, out _);
            _groupUserFirstMessages.TryRemove(groupUserKey, out _);

            _logger.LogInformation("🧹 Пользователь {UserId} полностью очищен из всех списков для чата {ChatId}", userId, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке пользователя {UserId} из списков для чата {ChatId}", userId, chatId);
        }
    }

    /// <summary>
    /// Банит пользователя и удаляет его из всех списков
    /// </summary>
    public async Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        try
        {
            // Создаем объекты для UserBanService
            var user = new User { Id = userId };
            var chat = new Chat { Id = chatId };

            // Используем UserBanService для централизованного бана
            await _userBanService.BanUserAsync(chat, user, BanTypeEnum.AutoBan, "Автобан", null, CancellationToken.None);

            // Удаляем сообщение если указано (используем перегрузку с messageId)
            if (messageIdToDelete.HasValue)
            {
                try
                {
                    await _userBanService.DeleteMessageByIdAsync(chatId, messageIdToDelete.Value);
                    _logger.LogInformation("Удалено сообщение {MessageId} из чата {ChatId}", messageIdToDelete.Value, chatId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить сообщение {MessageId} из чата {ChatId}", messageIdToDelete.Value, chatId);
                }
            }

            _logger.LogInformation("🚫 Пользователь {UserId} забанен и очищен из всех списков для чата {ChatId}", userId, chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при бане пользователя {UserId} в чате {ChatId}", userId, chatId);
            return false;
        }
    }

    /// <summary>
    /// Снимает ограничения с пользователя и одобряет его
    /// </summary>
    public async Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        try
        {
            // Снимаем все ограничения
            await _botClient.RestrictChatMember(
                chatId: chatId,
                userId: userId,
                permissions: new global::Telegram.Bot.Types.ChatPermissions
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
                useIndependentChatPermissions: true
            );

            // Полностью очищаем из всех списков
            CleanupUserFromAllLists(userId, chatId);

            // Одобряем пользователя (после очистки)
            await _userManager.Approve(userId, chatId);

            _logger.LogInformation("🔓 Пользователь {UserId} разблокирован и одобрен в чате {ChatId}", userId, chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при разблокировке пользователя {UserId} в чате {ChatId}", userId, chatId);
            return false;
        }
    }

    private async Task<bool> AnalyzeMimicryAndMarkSuspicious(User user, Chat chat, string userKey)
    {
        try
        {
            // Получаем первые сообщения
            List<string> firstMessages;
            if (userKey.Contains("_"))
            {
                // Групповой режим
                firstMessages = _groupUserFirstMessages.GetValueOrDefault(userKey, new List<string>());
            }
            else
            {
                // Глобальный режим
                firstMessages = _userFirstMessages.GetValueOrDefault(user.Id, new List<string>());
            }

            _logger.LogDebug("🎭 Анализ мимикрии для {User}: собрано {Count} сообщений",
                Utils.FullName(user), firstMessages.Count);

            if (firstMessages.Count < 3)
            {
                _logger.LogDebug("🎭 Недостаточно сообщений для анализа мимикрии: {Count}/3 для {User}",
                    firstMessages.Count, Utils.FullName(user));
                return false;
            }

            // Анализируем мимикрию
            var mimicryScore = _mimicryClassifier.AnalyzeMessages(firstMessages);

            _logger.LogDebug("🎭 Результат анализа мимикрии для {User}: скор={Score:F2}, порог={Threshold:F2}",
                Utils.FullName(user), mimicryScore, _appConfig.MimicryThreshold);

            if (mimicryScore >= _appConfig.MimicryThreshold)
            {
                // Помечаем как подозрительного
                var suspiciousInfo = new SuspiciousUserInfo(
                    SuspiciousAt: DateTime.UtcNow,
                    FirstMessages: firstMessages,
                    MimicryScore: mimicryScore,
                    AiDetectEnabled: false,
                    MessagesSinceSuspicious: 0
                );

                _suspiciousUsersStorage.AddSuspicious(user.Id, chat.Id, suspiciousInfo);

                _logger.LogWarning(
                    "🎭🚨 User {FullName} marked as suspicious in chat {ChatTitle} with mimicry score {Score:F2}. First messages: [{Messages}]",
                    Utils.FullName(user),
                    chat.Title ?? chat.Id.ToString(),
                    mimicryScore,
                    string.Join(", ", firstMessages.Select(m => $"\"{m}\""))
                );

                // Уведомляем админов (будет реализовано позже)
                await NotifyAdminsAboutSuspiciousUser(user, chat, suspiciousInfo);

                return true;
            }

            _logger.LogDebug("🎭✅ Пользователь {User} прошел проверку мимикрии: скор={Score:F2} < порог={Threshold:F2}",
                Utils.FullName(user), mimicryScore, _appConfig.MimicryThreshold);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе мимикрии для пользователя {UserId}", user.Id);
            return false;
        }
    }

    private async Task NotifyAdminsAboutSuspiciousUser(User user, Chat chat, SuspiciousUserInfo info)
    {
        try
        {
            var userName = Utils.FullName(user);
            var chatName = chat.Title ?? chat.Id.ToString();
            var score = info.MimicryScore.ToString("F2");

            var messageText = $"🔍 *Подозрительный пользователь обнаружен*\n\n" +
                             $"👤 Пользователь: [{userName}](tg://user?id={user.Id})\n" +
                             $"🏠 Чат: *{chatName}*\n" +
                             $"📊 Оценка мимикрии: *{score}*\n" +
                             $"🕐 Помечен как подозрительный: {info.SuspiciousAt:yyyy-MM-dd HH:mm}\n\n" +
                             $"📝 Первые сообщения:\n";

            for (int i = 0; i < info.FirstMessages.Count; i++)
            {
                var msg = info.FirstMessages[i];
                if (msg.Length > 50)
                    msg = msg.Substring(0, 50) + "...";
                messageText += $"{i + 1}. `{msg}`\n";
            }

            messageText += $"\n✅ Для одобрения нужно ещё {_appConfig.SuspiciousToApprovedMessageCount} хороших сообщений";

            // Создаем кнопки управления
            var approveCallback = $"suspicious_approve_{user.Id}_{chat.Id}";
            var banCallback = $"suspicious_ban_{user.Id}_{chat.Id}";
            var aiCallback = $"suspicious_ai_{user.Id}_{chat.Id}";

            _logger.LogDebug("🎛️ Создаем кнопки: одобрить={Approve}, забанить={Ban}, AI={Ai}",
                approveCallback, banCallback, aiCallback);

            var keyboard = new global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                 new[]
                 {
                     global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("✅ Одобрить", approveCallback),
                     global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🚫 Забанить", banCallback)
                 },
                 new[]
                 {
                     global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🔍 AI анализ вкл/выкл", aiCallback)
                 }
             });

            await _botClient.SendMessage(
                chatId: _appConfig.LogAdminChatId,
                text: messageText,
                parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: default
            );

            _logger.LogInformation("Отправлено уведомление админам о подозрительном пользователе {UserId} в чате {ChatId}",
                user.Id, chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления админам о подозрительном пользователе {UserId}", user.Id);
        }
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return _suspiciousUsersStorage.SetAiDetectEnabled(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return _suspiciousUsersStorage.GetStats();
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return _suspiciousUsersStorage.GetAiDetectUsers();
    }

    /// <summary>
    /// Ограничивает пользователя на readonly на указанное время
    /// </summary>
    private async Task<bool> RestrictUserToReadOnly(User user, Chat chat, TimeSpan duration)
    {
        try
        {
            var until = DateTime.UtcNow.Add(duration);
            await _botClient.RestrictChatMember(
                chatId: chat.Id,
                userId: user.Id,
                permissions: new global::Telegram.Bot.Types.ChatPermissions
                {
                    CanSendMessages = false,
                    CanSendAudios = false,
                    CanSendDocuments = false,
                    CanSendPhotos = false,
                    CanSendVideos = false,
                    CanSendVideoNotes = false,
                    CanSendVoiceNotes = false,
                    CanSendPolls = false,
                    CanSendOtherMessages = false,
                    CanAddWebPagePreviews = false,
                    CanChangeInfo = false,
                    CanInviteUsers = false,
                    CanPinMessages = false,
                    CanManageTopics = false
                },
                untilDate: until,
                useIndependentChatPermissions: true
            );

            _logger.LogInformation("🔒 Пользователь {User} ограничен на readonly до {Until}",
                Utils.FullName(user), until);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при ограничении пользователя {User}", Utils.FullName(user));
            return false;
        }
    }

    public async Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        // Проверяем, включен ли AI детект для этого пользователя
        var suspiciousUsers = _suspiciousUsersStorage.GetAiDetectUsers();
        var userHasAiDetect = suspiciousUsers.Any(u => u.UserId == user.Id && u.ChatId == chat.Id);

        if (!userHasAiDetect)
            return false;

        try
        {
            var userName = Utils.FullName(user);
            var chatName = chat.Title ?? chat.Id.ToString();
            var messageText = message.Text ?? message.Caption ?? "";

            if (string.IsNullOrWhiteSpace(messageText))
            {
                _logger.LogDebug("🔍 AI детект: пропускаем медиа/стикер сообщение от {User}", userName);
                return false;
            }

            _logger.LogInformation("🔍🤖 Запускаем СПЕЦИАЛЬНЫЙ AI анализ для подозрительного пользователя {User}: '{Text}'",
                userName, messageText.Substring(0, Math.Min(messageText.Length, 100)));

            // Получаем данные подозрительного пользователя для контекстного анализа
            var suspiciousUser = _suspiciousUsersStorage.GetSuspiciousUser(user.Id, chat.Id);
            var firstMessages = suspiciousUser?.FirstMessages ?? new List<string>();
            var mimicryScore = suspiciousUser?.MimicryScore ?? 0.0;

            // Выключаем AI детект после анализа (одноразовый)
            _suspiciousUsersStorage.SetAiDetectEnabled(user.Id, chat.Id, false);

            // Запускаем СПЕЦИАЛЬНЫЙ AI анализ для подозрительных пользователей
            var aiResult = await _aiChecks.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(30));
            var (isSpamByMl, mlScore) = await _classifier.IsSpam(messageText).WaitAsync(TimeSpan.FromSeconds(15));
            var spamProbability = aiResult.Probability; // получаем double из SpamProbability

            var aiReason = aiResult.Reason ?? "Нет объяснения";
            var isDefiniteSpam = spamProbability > 0.8 || mlScore > 1.5; // Высокая уверенность в спаме
            var isUncertain = spamProbability > 0.4 || mlScore > -0.3;   // Подозрительно - требует внимания

            if (isDefiniteSpam)
            {
                // Автоматическое удаление + ограничение на 2 часа + уведомление
                await _botClient.DeleteMessage(chat.Id, message.MessageId);
                await RestrictUserToReadOnly(user, chat, TimeSpan.FromHours(2));

                var aiDetectData = new AiDetectNotificationData(
                    user, chat, "Автоудаление спама", mimicryScore, spamProbability, mlScore, aiReason, messageText, true, message.MessageId);

                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.AiDetectAutoDelete,
                    aiDetectData,
                    default
                );

                _logger.LogInformation("🔍🤖🚫 Специальный AI детект: автоудаление спама от {User}, мимикрия={MimicryScore}, AI={AiScore}, ML={MlScore}",
                    userName, mimicryScore, spamProbability, mlScore);

                return true; // Пользователь заблокирован
            }
            else if (isUncertain)
            {
                // Ограничение пользователя на 2 часа + уведомление с кнопками
                await RestrictUserToReadOnly(user, chat, TimeSpan.FromHours(2));

                var aiDetectData = new AiDetectNotificationData(
                    user, chat, "Подозрительное сообщение", mimicryScore, spamProbability, mlScore, aiReason, messageText, false, message.MessageId);

                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.AiDetectSuspicious,
                    aiDetectData,
                    default
                );

                _logger.LogInformation("🔍🤖❓ Специальный AI детект: ограничение пользователя {User}, мимикрия={MimicryScore}, AI={AiScore}, ML={MlScore}",
                    userName, mimicryScore, spamProbability, mlScore);

                return true; // Пользователь ограничен
            }
            else
            {
                // Сообщение чистое - разрешаем нормальную обработку
                _logger.LogInformation("🔍🤖✅ Специальный AI детект: сообщение от {User} признано чистым, мимикрия={MimicryScore}, AI={AiScore}, ML={MlScore}",
                    userName, mimicryScore, spamProbability, mlScore);

                return false; // Можно продолжать нормальную обработку
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при AI анализе сообщения от пользователя {UserId}", user.Id);

            // В случае ошибки выключаем AI детект
            _suspiciousUsersStorage.SetAiDetectEnabled(user.Id, chat.Id, false);
            return false; // В случае ошибки разрешаем нормальную обработку
        }
    }
}
