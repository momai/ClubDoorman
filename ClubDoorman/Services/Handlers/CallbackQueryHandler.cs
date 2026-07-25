using ClubDoorman.Services.Violation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;
using ClubDoorman.Features.AdminOps;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик callback запросов
/// </summary>
public class CallbackQueryHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ICaptchaService _captchaService;
    private readonly IStatisticsService _statisticsService;
    private readonly IMessageService _messageService;
    private readonly IViolationTracker _violationTracker;
    private readonly IUserBanService _userBanService;
    private readonly IAdminCallbackDispatcher _adminCallbackDispatcher;
    private readonly ILogger<CallbackQueryHandler> _logger;
    private readonly IGoldenMasterRecorder _recorder; // input capture
    private readonly IModerationEventPublisher _events; // semantics publisher
    private readonly IAppConfig _appConfig;

    public CallbackQueryHandler(
        ITelegramBotClientWrapper bot,
        ICaptchaService captchaService,
        IStatisticsService statisticsService,
        IMessageService messageService,
        IViolationTracker violationTracker,
        IUserBanService userBanService,
        IAdminCallbackDispatcher adminCallbackDispatcher,
        ILogger<CallbackQueryHandler> logger,
        IGoldenMasterRecorder recorder,
        IModerationEventPublisher eventsPublisher,
        IAppConfig appConfig)
    {
        _bot = bot;
        _captchaService = captchaService;
        _statisticsService = statisticsService;
        _messageService = messageService;
        _violationTracker = violationTracker;
        _userBanService = userBanService;
        _adminCallbackDispatcher = adminCallbackDispatcher;
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

        var message = callbackQuery.Message;
        if (message == null)
        {
            _logger.LogWarning("❌ Callback без сообщения");
            return;
        }

        if (message.Chat.Id == _appConfig.AdminChatId || message.Chat.Id == _appConfig.LogAdminChatId)
        {
            _logger.LogDebug("🔧 Обрабатываем админский callback: {Data}", cbData);
            await HandleAdminCallback(callbackQuery, cancellationToken);
            return;
        }

        if (string.IsNullOrEmpty(cbData))
        {
            _logger.LogWarning("❌ Пустой callback data");
            return;
        }

        try
        {
            _logger.LogDebug("🎯 Обрабатываем капча callback: {Data}", cbData);
            await HandleCaptchaCallback(callbackQuery, gmCorrelation, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
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
        AdminCallbackResult result;
        try
        {
            result = await _adminCallbackDispatcher.DispatchAsync(callbackQuery, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке админского callback {Data}", callbackQuery.Data);
            result = AdminCallbackResult.Handled("Ошибка при выполнении действия", true);
        }

        await _bot.AnswerCallbackQuery(
            callbackQuery.Id,
            result.AnswerText,
            result.ShowAlert ? true : null,
            cancellationToken: cancellationToken);
    }
}
