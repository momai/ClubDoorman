using ClubDoorman.Services.Violation;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик callback запросов
/// </summary>
public class CallbackQueryHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IStatisticsService _statisticsService;
    private readonly IAiChecks _aiChecks;
    private readonly IModerationService _moderationService;
    private readonly IMessageService _messageService;
    private readonly IViolationTracker _violationTracker;
    private readonly IUserBanService _userBanService;
    private readonly ILogChatService _logChatService;
    private readonly IAdminActionStore _adminActionStore;
    private readonly ILogger<CallbackQueryHandler> _logger;
    private readonly IGoldenMasterRecorder _recorder; // input capture
    private readonly IModerationEventPublisher _events; // semantics publisher
    private readonly IAppConfig _appConfig;

    public CallbackQueryHandler(
        ITelegramBotClientWrapper bot,
        ICaptchaService captchaService,
        IUserManager userManager,
        IBadMessageManager badMessageManager,
        IStatisticsService statisticsService,
        IAiChecks aiChecks,
        IModerationService moderationService,
        IMessageService messageService,
        IViolationTracker violationTracker,
        IUserBanService userBanService,
        ILogChatService logChatService,
        IAdminActionStore adminActionStore,
    ILogger<CallbackQueryHandler> logger,
    IGoldenMasterRecorder recorder,
    IModerationEventPublisher eventsPublisher,
    IAppConfig appConfig)
    {
        _bot = bot;
        _captchaService = captchaService;
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _statisticsService = statisticsService;
        _aiChecks = aiChecks;
        _moderationService = moderationService;
        _messageService = messageService;
        _violationTracker = violationTracker;
        _userBanService = userBanService;
        _logChatService = logChatService;
        _adminActionStore = adminActionStore ?? throw new ArgumentNullException(nameof(adminActionStore));
    _logger = logger;
    _recorder = recorder;
    _events = eventsPublisher ?? throw new ArgumentNullException(nameof(eventsPublisher));
    _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    }

    public bool CanHandle(Update update)
    {
        return update.CallbackQuery != null;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var callbackQuery = update.CallbackQuery!;
        var cbData = callbackQuery.Data;

            string? gmCorrelation = null;
            try
            {
                gmCorrelation = _recorder.TryRecordInput(update, nameof(CallbackQueryHandler), callbackQuery.Message?.Chat.Id, callbackQuery.From.Id);
                if (gmCorrelation != null)
                {
                    using var corrScope = _logger.BeginScope(new Dictionary<string, object?> { ["gmCorrelation"] = gmCorrelation, ["chatId"] = callbackQuery.Message?.Chat.Id, ["userId"] = callbackQuery.From.Id, ["messageId"] = callbackQuery.Message?.MessageId });
                    _logger.LogTrace("CallbackQueryHandler: GoldenMaster correlation {Correlation} established", gmCorrelation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "CallbackQueryHandler: failed to record GM input");
            }

        _logger.LogDebug("📞 Получен callback: {Data} от пользователя {User} в чате {Chat}",
            cbData, callbackQuery.From.Username ?? callbackQuery.From.FirstName, callbackQuery.Message?.Chat.Id);

        if (string.IsNullOrEmpty(cbData))
        {
            _logger.LogWarning("❌ Пустой callback data");
            return;
        }

        var message = callbackQuery.Message;
        if (message == null)
        {
            _logger.LogWarning("❌ Callback без сообщения");
            return;
        }

        try
        {
            if (message.Chat.Id == _appConfig.AdminChatId || message.Chat.Id == _appConfig.LogAdminChatId)
            {
                _logger.LogDebug("🔧 Обрабатываем админский callback: {Data}", cbData);
                await HandleAdminCallback(callbackQuery, cancellationToken);
            }
            else
            {
                _logger.LogDebug("🎯 Обрабатываем капча callback: {Data}", cbData);
                await HandleCaptchaCallback(callbackQuery, gmCorrelation, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке callback {Data}", cbData);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCaptchaCallback(CallbackQuery callbackQuery, string? gmCorrelation, CancellationToken cancellationToken)
    {
        var cbData = callbackQuery.Data!;
        var message = callbackQuery.Message!;
        var chat = message.Chat;

        // Парсим данные капчи: cap_{user.Id}_{x}
        var split = cbData.Split('_');
        if (split.Length < 3 || split[0] != "cap")
            return;

        if (!long.TryParse(split[1], out var userId) || !int.TryParse(split[2], out var chosen))
            return;

        // Проверяем, что callback от того же пользователя
        if (callbackQuery.From.Id != userId)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        var key = _captchaService.GenerateKey(chat.Id, userId);
        var captchaInfo = _captchaService.GetCaptchaInfo(key);

        if (captchaInfo == null)
        {
            _logger.LogWarning("Капча {Key} не найдена в словаре", key);
            await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            return;
        }

        // Удаляем сообщение с капчей
        await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);

        // Проверяем правильность ответа
        var isCorrect = await _captchaService.ValidateCaptchaAsync(key, chosen);

        if (!isCorrect)
        {
            await HandleFailedCaptcha(captchaInfo, gmCorrelation, cancellationToken);
        }
        else
        {
            await HandleSuccessfulCaptcha(callbackQuery.From, chat, captchaInfo, gmCorrelation, cancellationToken);
        }
    }

    private async Task HandleFailedCaptcha(Models.CaptchaInfo captchaInfo, string? gmCorrelation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("==================== КАПЧА НЕ ПРОЙДЕНА ====================\n" +
            "Пользователь {User} (id={UserId}) не прошёл капчу в группе '{ChatTitle}' (id={ChatId})\n" +
            "===========================================================",
            Utils.FullName(captchaInfo.User), captchaInfo.User.Id, captchaInfo.ChatTitle ?? "-", captchaInfo.ChatId);

        _statisticsService.IncrementCaptcha(captchaInfo.ChatId);

        // Регистрируем нарушение за непройденную капчу
        var shouldBan = _violationTracker.RegisterViolation(captchaInfo.User.Id, captchaInfo.ChatId, ViolationType.CaptchaFailed);

        if (shouldBan)
        {
            _logger.LogWarning("Пользователь {User} (id={UserId}) достиг лимита непройденных капч в группе '{ChatTitle}' (id={ChatId}) - бан навсегда",
                Utils.FullName(captchaInfo.User), captchaInfo.User.Id, captchaInfo.ChatTitle ?? "-", captchaInfo.ChatId);
        }

        try
        {
            // Если достигнут лимит нарушений - бан навсегда, иначе временный бан на 20 минут
            var banUntilDate = shouldBan ? null : (DateTime?)DateTime.UtcNow.AddMinutes(20);

            await _bot.BanChatMember(
                captchaInfo.ChatId,
                captchaInfo.User.Id,
                banUntilDate,
                revokeMessages: false,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение о входе
            if (captchaInfo.UserJoinedMessage != null)
            {
                await _bot.DeleteMessage(captchaInfo.ChatId, captchaInfo.UserJoinedMessage.MessageId, cancellationToken);
            }

            // Отправляем уведомление через централизованную систему
            if (shouldBan && captchaInfo.UserJoinedMessage != null)
            {
                await _userBanService.TrackViolationAndBanIfNeededAsync(captchaInfo.UserJoinedMessage, captchaInfo.User, "непройденная капча", cancellationToken);
            }

            // Планируем разбан через 20 минут только если это не постоянный бан
            if (!shouldBan)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(20), cancellationToken);
                        await _bot.UnbanChatMember(captchaInfo.ChatId, captchaInfo.User.Id, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", captchaInfo.User.Id);
                    }
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось забанить пользователя за неправильную капчу");
        }
        if (gmCorrelation != null)
        {
            _events.Publish(gmCorrelation, new ModerationEvent("captcha_fail", RuleCode: Models.Logging.RuleCode.CaptchaFail));
        }
    }

    private async Task HandleSuccessfulCaptcha(User user, Chat chat, Models.CaptchaInfo captchaInfo, string? gmCorrelation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("==================== КАПЧА ПРОЙДЕНА ====================\n" +
            "Пользователь {User} (id={UserId}) успешно прошёл капчу в группе '{ChatTitle}' (id={ChatId})\n" +
            "========================================================",
            Utils.FullName(user), user.Id, chat.Title ?? "-", chat.Id);

        // Отправляем приветствие если они не отключены
    if (_appConfig.DisableWelcome)
        {
            _logger.LogInformation("Приветствие после капчи пропущено - приветствия отключены (DOORMAN_DISABLE_WELCOME=true)");
        }
        else
        {
            _logger.LogInformation("Отправляем приветствие после успешного прохождения капчи");
            await _messageService.SendWelcomeMessageAsync(user, chat, "приветствие после капчи", cancellationToken);
        }
        if (gmCorrelation != null)
        {
            _events.Publish(gmCorrelation, new ModerationEvent("captcha_success", RuleCode: Models.Logging.RuleCode.CaptchaSuccess));
        }
    }

    private async Task HandleAdminCallback(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var cbData = callbackQuery.Data!;
        var split = cbData.Split('_').ToList();

        _logger.LogDebug("🎛️ Админский callback: {Data}, split: [{Parts}]", cbData, string.Join(", ", split));

        try
        {
            if (split.Count > 1 && split[0] == "approve" && long.TryParse(split[1], out var approveUserId))
            {
                await HandleApproveUser(callbackQuery, approveUserId, cancellationToken);
            }
            else if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId) && long.TryParse(split[2], out var userId))
            {
                await HandleBanUser(callbackQuery, chatId, userId, cancellationToken);
            }
            else if (split.Count > 2 && split[0] == "logban" && long.TryParse(split[1], out var logChatId) && long.TryParse(split[2], out var logUserId))
            {
                await HandleLogBanUser(callbackQuery, logChatId, logUserId, cancellationToken);
            }
            else if (cbData.StartsWith("banprofile_", StringComparison.Ordinal))
            {
                var token = cbData["banprofile_".Length..];
                if (!await HandleBanUserByProfile(callbackQuery, token, cancellationToken))
                    return;
            }
            else if (split.Count > 2 && split[0] == "suspicious")
            {
                _logger.LogDebug("🔍 Обрабатываем suspicious callback: {Data}", cbData);
                await HandleSuspiciousUserCallback(callbackQuery, split, cancellationToken);
            }
            else if (split.Count > 1 && split[0] == "aiOk")
            {
                if (split.Count == 2 && long.TryParse(split[1], out var aiOkUserIdOld))
                {
                    // Старый формат aiOk_{userId} - только кеширование
                    await HandleAiOkUser(callbackQuery, null, aiOkUserIdOld, cancellationToken);
                }
                else if (split.Count == 3 && long.TryParse(split[1], out var aiOkChatId) && long.TryParse(split[2], out var aiOkUserId))
                {
                    // Новый формат aiOk_{chatId}_{userId} - снятие ограничений + кеширование
                    await HandleAiOkUser(callbackQuery, aiOkChatId, aiOkUserId, cancellationToken);
                }
            }
            else if (cbData == "noop")
            {
                // Ничего не делаем, просто убираем кнопки
                await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
            }


            await _bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке админского callback {Data}", cbData);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Ошибка при выполнении действия", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleApproveUser(CallbackQuery callbackQuery, long userId, CancellationToken cancellationToken)
    {
        // Админ одобряет пользователя - всегда глобально
        await _userManager.Approve(userId);

        var adminName = GetAdminDisplayName(callbackQuery.From);

        // Обновляем сообщение с результатом действия
    var approveMessage = $"{callbackQuery.Message!.Text}\n\n✅ Одобрен администратором {adminName}\n👤 Пользователь добавлен в список доверенных";

        await _bot.EditMessageText(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            approveMessage,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Пользователь {UserId} одобрен администратором {AdminName}", userId, adminName);
    }

    private async Task HandleBanUser(CallbackQuery callbackQuery, long chatId, long userId, CancellationToken cancellationToken)
    {
        var callbackDataBan = $"ban_{chatId}_{userId}";
        var userMessage = MemoryCache.Default.Remove(callbackDataBan) as Message;
        var adminName = GetAdminDisplayName(callbackQuery.From);

        // Добавляем текст в список плохих сообщений
        var text = userMessage?.Caption ?? userMessage?.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _badMessageManager.MarkAsBad(text);
        }

        try
        {
            // Создаем объекты для UserBanService
            var user = new User { Id = userId };
            var chat = new Chat { Id = chatId };

            // Используем UserBanService для централизованного бана
            await _userBanService.BanUserAsync(chat, user, BanTypeEnum.ManualBan, "Ручной бан", userMessage, cancellationToken);

            // Обновляем сообщение с результатом действия
            var banMessage = $"{callbackQuery.Message!.Text}\n\n🚫 Забанен администратором {adminName}\n🧹 Пользователь очищен из всех списков\n📝 Сообщение добавлено в список авто-бана";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                banMessage,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Пользователь {UserId} забанен администратором {AdminName}", userId, adminName);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя через админский callback");

            // Обновляем сообщение с ошибкой
            var errorMessage = $"{callbackQuery.Message!.Text}\n\n❌ Ошибка при бане администратором {adminName}\nНе могу забанить. Не хватает могущества? Сходите забаньте руками";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                errorMessage,
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task HandleLogBanUser(CallbackQuery callbackQuery, long chatId, long userId, CancellationToken cancellationToken)
    {
        var adminName = GetAdminDisplayName(callbackQuery.From);

        try
        {
            // Используем сервис лог-чата для обработки бана
            await _logChatService.HandleLogBanAsync(chatId, userId, adminName, cancellationToken);

            // Обновляем сообщение с результатом действия (без упоминания автобана)
            var banMessage = $"{callbackQuery.Message!.Text}\n\n🚫 Забанен администратором {adminName}\n🧹 Пользователь очищен из всех списков";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                banMessage,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Пользователь {UserId} забанен из лог-чата администратором {AdminName}", userId, adminName);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя через лог-чат callback");

            // Обновляем сообщение с ошибкой
            var errorMessage = $"{callbackQuery.Message!.Text}\n\n❌ Ошибка при бане администратором {adminName}\nНе могу забанить. Не хватает могущества? Сходите забаньте руками";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                errorMessage,
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task<bool> HandleBanUserByProfile(CallbackQuery callbackQuery, string token, CancellationToken cancellationToken)
    {
        var reviewState = _adminActionStore.TakeProfileReview(token);
        if (reviewState == null)
        {
            await _bot.AnswerCallbackQuery(
                callbackQuery.Id,
                "Действие устарело или уже выполнено",
                showAlert: true,
                cancellationToken: cancellationToken);
            return false;
        }

        var chatId = reviewState.ChatId;
        var userId = reviewState.UserId;
        var adminName = GetAdminDisplayName(callbackQuery.From);

        // При бане по профилю НЕ добавляем сообщение в автобан - проблема в профиле, а не в сообщении
        _logger.LogInformation("🚫👤 Бан по профилю - сообщение НЕ добавляется в автобан для пользователя {UserId}", userId);

        try
        {
            // Создаем объекты для UserBanService
            var user = new User { Id = userId };
            var chat = new Chat { Id = chatId };

            // Используем UserBanService для централизованного бана по профилю с удалением сообщения по ID
            await _userBanService.BanUserAsync(
                chat,
                user,
                BanTypeEnum.ProfileBan,
                "Бан по профилю",
                reviewState.MessageId,
                reviewState.ChatId,
                cancellationToken
            );

            // ФИКС: ВСЕГДА пытаемся переслать сообщение при ручном бане
            // Проверка на удаление происходит в try-catch - если удалено, получим ошибку
            if (reviewState.MessageId != null)
            {
                try
                {
                    await _bot.ForwardMessage(
                        chatId: _appConfig.AdminChatId,
                        fromChatId: reviewState.ChatId,
                        messageId: (int)reviewState.MessageId.Value,
                        cancellationToken: cancellationToken
                    );
                    _logger.LogDebug("🤖 При ручном бане переслано сообщение пользователя {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось переслать сообщение пользователя {UserId} при ручном бане - вероятно, уже удалено", userId);
                }
            }
            else
            {
                _logger.LogDebug("🤖 При ручном бане сообщение пользователя {UserId} не пересылается - MessageId отсутствует", userId);
            }

            // Обновляем сообщение с результатом действия
            var banMessage = $"{callbackQuery.Message!.Text}\n\n🚫 Забанен за спам-профиль администратором {adminName}\n🧹 Пользователь очищен из всех списков\n⚠️ Сообщение НЕ добавлено в автобан (проблема в профиле)";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                banMessage,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Пользователь {UserId} забанен за спам-профиль администратором {AdminName}", userId, adminName);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя через админский callback (бан по профилю)");

            // Обновляем сообщение с ошибкой
            var errorMessage = $"{callbackQuery.Message!.Text}\n\n❌ Ошибка при бане администратором {adminName}\nНе могу забанить. Не хватает могущества? Сходите забаньте руками";

            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                errorMessage,
                cancellationToken: cancellationToken
            );
        }

        return true;
    }

    private async Task HandleAiOkUser(CallbackQuery callbackQuery, long? chatId, long userId, CancellationToken cancellationToken)
    {
        // Помечаем AI проверку как безопасную
        _aiChecks.MarkUserOkay(userId);

        var adminName = GetAdminDisplayName(callbackQuery.From);
        var message = $"✅ {adminName} отметил профиль как безопасный - AI проверки отключены для этого пользователя";

        // Если передан chatId - пытаемся снять ограничения с пользователя
        if (chatId.HasValue)
        {
            try
            {
                // Снимаем все ограничения (возвращаем полные права)
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
                        CanChangeInfo = false, // Эти права обычно не даются обычным пользователям
                        CanInviteUsers = false,
                        CanPinMessages = false,
                        CanManageTopics = false
                    },
                    cancellationToken: cancellationToken
                );

                message += " + ограничения сняты";
                _logger.LogInformation("Ограничения сняты с пользователя {UserId} в чате {ChatId} администратором {AdminName}",
                    userId, chatId.Value, adminName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось снять ограничения с пользователя {UserId} в чате {ChatId}", userId, chatId.Value);
                message += " (не удалось снять ограничения - возможно, недостаточно прав)";
            }
        }

        // Редактируем сообщение с результатом вместо отправки нового
        try
        {
            await _bot.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                $"{callbackQuery.Message.Text}\n\n{message}",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отредактировать сообщение с результатом AI анализа");

            // Если не получилось отредактировать - отправляем новое и убираем кнопки
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.UserApproved,
                new SimpleNotificationData(callbackQuery.From, callbackQuery.Message!.Chat, message),
                cancellationToken
            );
            await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
        }
    }

    private async Task HandleSuspiciousUserCallback(CallbackQuery callbackQuery, List<string> split, CancellationToken cancellationToken)
    {
        if (split.Count < 5)
            return;

        var action = split[1]; // approve, ban, ai
        if (!long.TryParse(split[2], out var userId) || !long.TryParse(split[3], out var chatId) || !long.TryParse(split[4], out var messageId))
            return;

        var adminName = GetAdminDisplayName(callbackQuery.From);

        try
        {
            switch (action)
            {
                case "approve":
                    // Снимаем ограничения и одобряем пользователя
                    var success = await _moderationService.UnrestrictAndApproveUserAsync(userId, chatId);

                    var statusMessage = success
                        ? $"{callbackQuery.Message!.Text}\n\n✅ *Разблокирован и одобрен администратором {adminName}*"
                        : $"{callbackQuery.Message!.Text}\n\n⚠️ *Одобрен администратором {adminName}* (возможны проблемы с разблокировкой)";

                    await _bot.EditMessageText(
                        callbackQuery.Message!.Chat.Id,
                        callbackQuery.Message.MessageId,
                        statusMessage,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    _logger.LogInformation("Подозрительный пользователь {UserId} разблокирован и одобрен администратором {AdminName}", userId, adminName);
                    break;

                case "ban":
                    try
                    {
                        // Банируем пользователя и очищаем из всех списков
                        var banSuccess = await _moderationService.BanAndCleanupUserAsync(userId, chatId);

                        // Удаляем пересланное сообщение пользователя (если есть)
                        var replyToMessage = callbackQuery.Message!.ReplyToMessage;
                        if (replyToMessage != null)
                        {
                            try
                            {
                                await _bot.DeleteMessage(replyToMessage.Chat.Id, replyToMessage.MessageId, cancellationToken);
                                _logger.LogDebug("Удалено пересланное сообщение пользователя {UserId} из чата {ChatId}", userId, chatId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Не удалось удалить пересланное сообщение пользователя {UserId} из чата {ChatId}", userId, chatId);
                            }
                        }

                        // Удаляем оригинальное сообщение из чата пользователя
                        try
                        {
                            await _bot.DeleteMessage(chatId, (int)messageId, cancellationToken);
                            _logger.LogDebug("Удалено оригинальное сообщение пользователя {UserId} из чата {ChatId}", userId, chatId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не удалось удалить оригинальное сообщение пользователя {UserId} из чата {ChatId}", userId, chatId);
                        }

                        var banMessage = banSuccess
                            ? $"{callbackQuery.Message.Text}\n\n🚫 Забанен и очищен администратором {adminName}"
                            : $"{callbackQuery.Message.Text}\n\n⚠️ Обработан администратором {adminName} (возможны проблемы с баном)";

                        _logger.LogInformation("Подозрительный пользователь {UserId} забанен и очищен администратором {AdminName}", userId, adminName);

                        await _bot.EditMessageText(
                            callbackQuery.Message!.Chat.Id,
                            callbackQuery.Message.MessageId,
                            banMessage,
                            cancellationToken: cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось выполнить действие для пользователя {UserId}", userId);
                        await _bot.AnswerCallbackQuery(callbackQuery.Id, "❌ Не удалось выполнить действие", showAlert: true, cancellationToken: cancellationToken);
                        return;
                    }
                    break;

                case "ai":
                    // Переключаем состояние AI детекта
                    var aiDetectUsers = _moderationService.GetAiDetectUsers();
                    var isCurrentlyEnabled = aiDetectUsers.Any(u => u.UserId == userId && u.ChatId == chatId);
                    var newStatus = _moderationService.SetAiDetectForSuspiciousUser(userId, chatId, !isCurrentlyEnabled);

                    var statusText = newStatus ? "включен" : "выключен";
                    var statusEmoji = newStatus ? "🔍✅" : "🔍❌";

                    await _bot.EditMessageText(
                        callbackQuery.Message!.Chat.Id,
                        callbackQuery.Message.MessageId,
                        $"{callbackQuery.Message.Text}\n\n{statusEmoji} AI детект {statusText} администратором {adminName}",
                        cancellationToken: cancellationToken
                    );

                    _logger.LogInformation("AI детект для подозрительного пользователя {UserId} {Status} администратором {AdminName}",
                        userId, statusText, adminName);
                    break;

                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке callback для подозрительного пользователя {UserId}", userId);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "❌ Произошла ошибка", showAlert: true, cancellationToken: cancellationToken);
        }
    }



    private static string GetAdminDisplayName(User user)
    {
        return !string.IsNullOrEmpty(user.Username)
            ? user.Username
            : Utils.FullName(user);
    }


}
