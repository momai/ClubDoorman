using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Text;
using ClubDoorman.Handlers.Commands;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Handlers;

/// <summary>
/// Обработчик сообщений
/// </summary>
public class MessageHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationService _moderationService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly ISpamHamClassifier _classifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IAiChecks _aiChecks;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IMessageService _messageService;
    private readonly IChatLinkFormatter _chatLinkFormatter;
    private readonly IBotPermissionsService _botPermissionsService;

    // Флаги присоединившихся пользователей (временные)
    private static readonly ConcurrentDictionary<string, byte> _joinedUserFlags = new();

    /// <summary>
    /// Создает экземпляр обработчика сообщений.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="moderationService">Сервис модерации</param>
    /// <param name="captchaService">Сервис капчи</param>
    /// <param name="userManager">Менеджер пользователей</param>
    /// <param name="classifier">Классификатор спама</param>
    /// <param name="badMessageManager">Менеджер плохих сообщений</param>
    /// <param name="aiChecks">AI проверки</param>
    /// <param name="globalStatsManager">Менеджер глобальной статистики</param>
    /// <param name="statisticsService">Сервис статистики</param>
    /// <param name="serviceProvider">Провайдер сервисов</param>
    /// <param name="userFlowLogger">Логгер пользовательского флоу</param>
    /// <param name="messageService">Сервис уведомлений</param>
    /// <param name="chatLinkFormatter">Форматтер ссылок на чаты</param>
    /// <param name="botPermissionsService">Сервис проверки прав бота</param>
    /// <param name="logger">Логгер</param>
    /// <exception cref="ArgumentNullException">Если любой из параметров равен null</exception>
    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IModerationService moderationService,
        ICaptchaService captchaService,
        IUserManager userManager,
        ISpamHamClassifier classifier,
        IBadMessageManager badMessageManager,
        IAiChecks aiChecks,
        GlobalStatsManager globalStatsManager,
        IStatisticsService statisticsService,
        IServiceProvider serviceProvider,
        IUserFlowLogger userFlowLogger,
        IMessageService messageService,
        IChatLinkFormatter chatLinkFormatter,
        IBotPermissionsService botPermissionsService,
        ILogger<MessageHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _badMessageManager = badMessageManager ?? throw new ArgumentNullException(nameof(badMessageManager));
        _aiChecks = aiChecks ?? throw new ArgumentNullException(nameof(aiChecks));
        _globalStatsManager = globalStatsManager ?? throw new ArgumentNullException(nameof(globalStatsManager));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _chatLinkFormatter = chatLinkFormatter ?? throw new ArgumentNullException(nameof(chatLinkFormatter));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Проверяет, может ли обработчик обработать данное обновление.
    /// </summary>
    /// <param name="update">Обновление для проверки</param>
    /// <returns>true, если обновление содержит сообщение</returns>
    public bool CanHandle(Update update)
    {
        return update?.Message != null || update?.EditedMessage != null;
    }

    /// <summary>
    /// Обрабатывает обновление, содержащее сообщение.
    /// </summary>
    /// <param name="update">Обновление для обработки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <exception cref="ArgumentNullException">Если update равен null</exception>
    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (update.Message == null && update.EditedMessage == null) 
            throw new ArgumentNullException(nameof(update.Message));

        var message = update.EditedMessage ?? update.Message!;
        var chat = message.Chat;

        _logger.LogDebug("MessageHandler получил сообщение {MessageId} в чате {ChatId} от пользователя {UserId}", 
            message.MessageId, chat.Id, message.From?.Id);

        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        // ИСКЛЮЧЕНИЕ: админ-чаты всегда обрабатываются (для команд /spam, /ham и т.д.)
        var isAdminChat = chat.Id == Config.AdminChatId || chat.Id == Config.LogAdminChatId;
        
        if (!Config.IsChatAllowed(chat.Id) && !isAdminChat)
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем", chat.Id, chat.Title);
            return;
        }

        // Игнорировать полностью отключённые чаты
        if (Config.DisabledChats.Contains(chat.Id))
            return;

        // Проверяем тихий режим (бот без прав администратора)
        var isSilentMode = await _botPermissionsService.IsSilentModeAsync(chat.Id, cancellationToken);
        if (isSilentMode)
        {
            _logger.LogInformation("🔇 Тихий режим в чате {ChatId} ({ChatTitle}) - бот без прав администратора", chat.Id, chat.Title);
        }

        // Автоматически добавляем чат в конфиг
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        // Обработка команд
        if (message.Text?.StartsWith("/") == true)
        {
            await HandleCommandAsync(message, cancellationToken);
            return;
        }

        // Для приватных чатов обрабатываем только команды, остальное игнорируем
        if (chat.Type == ChatType.Private)
        {
            _logger.LogDebug("Приватный чат {ChatId} - обрабатываем только команды", chat.Id);
            return;
        }

        // Обработка новых участников
        if (message.NewChatMembers != null && chat.Id != Config.AdminChatId)
        {
            await HandleNewMembersAsync(message, cancellationToken);
            return;
        }

        // Удаление сообщений о бане ботом
        if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
        {
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Удалено сообщение о бане/исключении пользователя");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении");
            }
            return;
        }

        // Сообщения от каналов
        if (message.SenderChat != null)
        {
            await HandleChannelMessageAsync(message, cancellationToken);
            return;
        }

        // Обычные сообщения пользователей
        await HandleUserMessageAsync(message, isSilentMode, cancellationToken);
    }

    private async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var commandText = message.Text!.Split(' ')[0].ToLower();
        var command = commandText.StartsWith("/") ? commandText.Substring(1) : commandText;

        // Обработка команды /start
        if (command == "start")
        {
            // Получаем StartCommandHandler из DI и делегируем обработку
            var startHandler = _serviceProvider.GetRequiredService<StartCommandHandler>();
            await startHandler.HandleAsync(message, cancellationToken);
            return;
        }

        // Обработка команды /suspicious
        if (command == "suspicious")
        {
            // Получаем SuspiciousCommandHandler из DI и делегируем обработку
            var suspiciousHandler = _serviceProvider.GetRequiredService<SuspiciousCommandHandler>();
            await suspiciousHandler.HandleAsync(message, cancellationToken);
            return;
        }

        // Админские команды (/spam, /ham, /check) - только в админ-чатах
        var isAdminChat = message.Chat.Id == Config.AdminChatId || message.Chat.Id == Config.LogAdminChatId;
        if (isAdminChat && message.ReplyToMessage != null && (command == "spam" || command == "ham" || command == "check"))
        {
            await HandleAdminCommandAsync(message, command, cancellationToken);
        }
        
        // Команда статистики по группам (/stat, /stats) - только в админ-чатах
        if (isAdminChat && (command == "stat" || command == "stats"))
        {
            await HandleStatsCommandAsync(message, cancellationToken);
        }
        
        // Команда отправки сообщения (/say) - только в админ-чатах
        if (isAdminChat && command == "say")
        {
            await HandleSayCommandAsync(message, cancellationToken);
        }
    }

    private async Task HandleAdminCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        var replyToMessage = message.ReplyToMessage!;
        
        // Проверяем, что реплай не на сообщение бота (кроме форвардов)
        if (replyToMessage.From?.Id == _bot.BotId && replyToMessage.ForwardDate == null)
        {
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Реплай на сообщение бота"), 
                cancellationToken);
            return;
        }

        var text = replyToMessage.Text ?? replyToMessage.Caption;
        _logger.LogDebug("Админская команда /{Command}: извлечен текст='{Text}' (длина={Length})", 
            command, string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text, 
            text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("❌ Команда /{Command} не выполнена: текст сообщения пустой или отсутствует", command);
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Сообщение не содержит текста"), 
                cancellationToken);
            return;
        }

        switch (command)
        {
            case "check":
                await HandleCheckCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
            case "spam":
                await HandleSpamCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
            case "ham":
                await HandleHamCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
        }
    }

    private async Task HandleCheckCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        var emojis = SimpleFilters.TooManyEmojis(text);
        var normalized = TextProcessor.NormalizeText(text);
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        var hasStopWords = SimpleFilters.HasStopWords(normalized);
        var (spam, score) = await _classifier.IsSpam(normalized);
        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
        var msg =
            $"*Результат проверки:*\n"
            + $"• Много эмодзи: *{emojis}*\n"
            + $"• Найдены стоп-слова: *{hasStopWords}*\n"
            + $"• Маскирующиеся слова: *{lookAlikeMsg}*\n"
            + $"• ML классификатор: спам *{spam}*, скор *{score}*\n\n"
            + $"_Если простые фильтры отработали, то в датасет добавлять не нужно_";
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.SystemInfo, 
            new SimpleNotificationData(message.From!, message.Chat, msg), 
            cancellationToken);
    }

    private async Task HandleSpamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔥 Обрабатываем команду /spam для текста: '{Text}'", text);
        await _classifier.AddSpam(text);
        await _badMessageManager.MarkAsBad(text);
        
        // Уведомление пользователю
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример спама"), 
            cancellationToken);
            
        // Уведомление в админку о добавлении спам примера
        var adminData = new SimpleNotificationData(
            message.From!, 
            message.Chat, 
            $"Администратор {Utils.FullName(message.From!)} добавил сообщение как пример СПАМА:\n\n`{text.Substring(0, Math.Min(text.Length, 200))}{(text.Length > 200 ? "..." : "")}`"
        );
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemInfo, adminData, cancellationToken);
        
        _logger.LogInformation("✅ Команда /spam успешно выполнена");
    }

    private async Task HandleHamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("✅ Обрабатываем команду /ham для текста: '{Text}'", text);
        await _classifier.AddHam(text);
        
        // Уведомление пользователю (исправляем Markdown ошибку)
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример НЕ\\-спама"), 
            cancellationToken);
            
        // Уведомление в админку о добавлении НЕ-спам примера
        var adminData = new SimpleNotificationData(
            message.From!, 
            message.Chat, 
            $"Администратор {Utils.FullName(message.From!)} добавил сообщение как пример НЕ-спама:\n\n`{text.Substring(0, Math.Min(text.Length, 200))}{(text.Length > 200 ? "..." : "")}`"
        );
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemInfo, adminData, cancellationToken);
        
        _logger.LogInformation("✅ Команда /ham успешно выполнена");
    }

    private async Task HandleStatsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var report = _statisticsService.GetAllStats();
        var sb = new StringBuilder();
        sb.AppendLine("📊 *Статистика по группам:*\n");
        foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
        {
            var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
            if (sum == 0) continue;
            Chat? chat = null;
            try { chat = await _bot.GetChat(chatId); } catch { }
            sb.AppendLine();
            if (chat != null)
                sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chat)} (`{chat.Id}`) [{ChatSettingsManager.GetChatType(chat.Id)}]:");
            else
                sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chatId, stats.ChatTitle)} (`{chatId}`) [{ChatSettingsManager.GetChatType(chatId)}]:");
            sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
            if (stats.BlacklistBanned > 0)
                sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
            if (stats.StoppedCaptcha > 0)
                sb.AppendLine($"▫️ Не прошли капчу: *{stats.StoppedCaptcha}*");
            if (stats.KnownBadMessage > 0)
                sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
            if (stats.LongNameBanned > 0)
                sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
        }
        if (sb.Length <= 35)
            sb.AppendLine("\nНичего интересного не произошло 🎉");
        
        await _messageService.SendUserNotificationAsync(
            message.From!,
            message.Chat,
            UserNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, sb.ToString()),
            cancellationToken
        );
    }

    private async Task HandleSayCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var parts = message.Text!.Split(' ', 3);
        if (parts.Length < 3)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Формат: /say @username сообщение или /say user_id сообщение"),
                cancellationToken
            );
            return;
        }
        
        var target = parts[1];
        var textToSend = parts[2];
        long? userId = null;
        
        if (target.StartsWith("@"))
        {
            // Пробуем найти userId по username среди недавних пользователей (по кэшу)
            userId = TryFindUserIdByUsername(target.Substring(1));
        }
        else if (long.TryParse(target, out var id))
        {
            userId = id;
        }
        
        if (userId == null)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, $"Не удалось найти пользователя {target}. Сообщение не отправлено."),
                cancellationToken
            );
            return;
        }

        try
        {
            await _bot.SendMessage(userId.Value, textToSend, parseMode: ParseMode.Markdown);
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Success,
                new SimpleNotificationData(message.From!, message.Chat, $"Сообщение отправлено пользователю {target}"),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, $"Не удалось доставить сообщение пользователю {target}: {ex.Message}"),
                cancellationToken
            );
        }
    }

    // Вспомогательная функция для поиска userId по username среди недавних пользователей (по кэшу)
    private long? TryFindUserIdByUsername(string username)
    {
        // Можно использовать MemoryCache или другой кэш, если он есть
        // Здесь пример с MemoryCache: ищем по ключам, где username встречался
        foreach (var item in MemoryCache.Default)
        {
            if (item.Value is string text && text.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                // Ключи вида chatId_userId
                var parts = item.Key.ToString().Split('_');
                if (parts.Length == 2 && long.TryParse(parts[1], out var uid))
                    return uid;
            }
        }
        return null;
    }

    private async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null)
        {
            _logger.LogDebug("Сообщение о новых участниках не содержит данных о пользователях");
            return;
        }

        foreach (var newUser in message.NewChatMembers.Where(x => x != null && !x.IsBot))
        {
            var joinKey = $"joined_{message.Chat.Id}_{newUser.Id}";
            if (!_joinedUserFlags.ContainsKey(joinKey))
            {
                _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\n" +
                    "Пользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n" +
                    "========================================================", 
                    Utils.FullName(newUser), newUser.Id, newUser.Username ?? "-", message.Chat.Title ?? "-", message.Chat.Id);

                _joinedUserFlags.TryAdd(joinKey, 1);
                _ = Task.Run(async () => { 
                    await Task.Delay(15000); 
                    _joinedUserFlags.TryRemove(joinKey, out _); 
                });
            }

            await ProcessNewUserAsync(message, newUser, cancellationToken);
        }
    }

    private async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null пользователем");
            return;
        }

        if (userJoinMessage?.Chat == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null сообщением или чатом");
            return;
        }

        var chat = userJoinMessage.Chat;

        // Проверка имени пользователя
        if (_moderationService == null)
        {
            _logger.LogError("_moderationService равен null в ProcessNewUserAsync");
            return;
        }

        var nameResult = await _moderationService.CheckUserNameAsync(user);
        if (nameResult.Action == ModerationAction.Ban)
        {
            await BanUserForLongName(userJoinMessage, user, nameResult.Reason, null, cancellationToken);
            return;
        }
        if (nameResult.Action == ModerationAction.Report)
        {
            await BanUserForLongName(userJoinMessage, user, nameResult.Reason, TimeSpan.FromMinutes(10), cancellationToken);
            return;
        }

        // Проверка клубного пользователя
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        // Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            await BanBlacklistedUser(userJoinMessage, user, cancellationToken);
            return;
        }

        // Проверяем, не находится ли пользователь уже в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            _logger.LogDebug("Пользователь уже проходит капчу");
            return;
        }

        // Создаем капчу
        var captchaInfo = await _captchaService.CreateCaptchaAsync(chat, user, userJoinMessage);
        if (captchaInfo == null)
        {
            _logger.LogInformation($"[NO_CAPTCHA] Капча не требуется для чата {chat.Id}");
            return;
        }
    }

    private async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat!;

        // Разрешаем сообщения от самого чата
        if (senderChat.Id == chat.Id)
            return;

        // Разрешаем в announcement чатах
        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
            return;

        // Проверяем связанный чат
        try
        {
            var chatFull = await _bot.GetChat(chat, cancellationToken);
            // Проверяем, является ли это обсуждением канала
            if (chat.Type == ChatType.Supergroup && message.IsAutomaticForward)
                return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить информацию о чате {ChatId}", chat.Id);
        }

        // Автобан каналов если включен
        if (Config.ChannelAutoBan)
        {
            await AutoBanChannel(message, cancellationToken);
        }
        else
        {
            // Просто репортим
            _logger.LogInformation("Сообщение от канала {ChannelTitle} в чате {ChatTitle} - репорт в админ-чат", 
                senderChat.Title, chat.Title);
        }
    }

    private async Task HandleUserMessageAsync(Message message, bool isSilentMode, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;

        // Игнорируем сообщения без пользователя (системные сообщения)
        if (user == null)
        {
            _logger.LogDebug("Игнорируем системное сообщение без пользователя");
            return;
        }

        // Игнорируем сообщения от ботов
        if (user.IsBot)
        {
            _logger.LogDebug("Игнорируем сообщение от бота {BotId}", user.Id);
            return;
        }

        // Игнорируем системные сообщения (выход пользователей и т.д.)
        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Игнорируем системное сообщение о выходе пользователя");
            return;
        }

        // Проверяем, не находится ли пользователь в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            // Удаляем сообщение от пользователя, который должен пройти капчу
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить сообщение от пользователя проходящего капчу");
            }
            return;
        }

        // ПРИОРИТЕТНАЯ проверка блэклиста lols.bot (выполняется даже для одобренных)
        _logger.LogDebug("🔍 Проверяем пользователя {UserId} по блэклисту lols.bot", user.Id);
        if (await _userManager.InBanlist(user.Id))
        {
            await HandleBlacklistBan(message, user, chat, cancellationToken);
            return;
        }
        _logger.LogDebug("✅ Пользователь {UserId} не найден в блэклисте", user.Id);

        // Проверяем, одобрен ли пользователь
        if (_moderationService.IsUserApproved(user.Id, chat.Id))
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию", user.Id, chat.Id);
            return;
        }

        // Логируем сообщения от неодобренных пользователей для анализа
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        // Определяем тип пользователя
        var isChannelDiscussion = await IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";
        
        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================", 
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id, 
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        // Проверка на пересланные сообщения от новичков
        if (Config.DeleteForwardedMessages && message.ForwardOrigin != null)
        {
            _logger.LogInformation("🔄 Удаление пересланного сообщения от новичка {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
                Utils.FullName(user), user.Id, chat.Title ?? "-", chat.Id);
            
            // Удаляем сообщение
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                
                // Отправляем предупреждение пользователю с автоудалением
                var notificationMessage = await _messageService.SendUserNotificationWithReplyAsync(user, chat, UserNotificationType.MessageDeleted, 
                    new SimpleNotificationData(user, chat, "пересланные сообщения от новичков не разрешены"), 
                    cancellationToken);
                
                // Удаляем уведомление через 10 секунд
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        await _bot.DeleteMessage(chat.Id, notificationMessage.MessageId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить уведомление пользователю");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить пересланное сообщение от новичка");
            }
            return;
        }

        // Проверка на клубного пользователя
        var clubName = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club", clubName);
            return;
        }

        // Модерация сообщения (базовые проверки сначала)
        _userFlowLogger.LogModerationStarted(user, chat, messageText);
        var moderationResult = await _moderationService.CheckMessageAsync(message);
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);
        
        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                _logger.LogDebug("Сообщение разрешено: {Reason}", moderationResult.Reason);
                var allowedMessageText = message.Text ?? message.Caption ?? "";
                
                // AI анализ профиля при первом сообщении (только если базовые проверки пройдены)
                var profileAnalysisResult = await PerformAiProfileAnalysis(message, user, chat, cancellationToken);
                if (profileAnalysisResult)
                {
                    // Пользователь получил ограничения за подозрительный профиль, не засчитываем сообщение
                    return;
                }
                
                // Проверяем AI детект для подозрительных пользователей
                var aiDetectBlocked = await _moderationService.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
                
                // Засчитываем хорошее сообщение только если пользователь не был заблокирован AI детектом
                if (!aiDetectBlocked)
                {
                    await _moderationService.IncrementGoodMessageCountAsync(user, chat, allowedMessageText);
                }
                break;
            
            case ModerationAction.Ban:
                _userFlowLogger.LogUserBanned(user, chat, moderationResult.Reason);
                await AutoBan(message, moderationResult.Reason, cancellationToken);
                break;
            
            case ModerationAction.Delete:
                _logger.LogInformation("Удаление сообщения: {Reason}", moderationResult.Reason);
                try
                {
                    await DeleteAndReportMessage(message, moderationResult.Reason, isSilentMode, cancellationToken);
                    _logger.LogInformation("Сообщение успешно обработано для удаления");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при удалении сообщения: {Reason}", moderationResult.Reason);
                }
                break;
            
            case ModerationAction.Report:
                _logger.LogInformation("Отправка в админ-чат: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;
            
            case ModerationAction.RequireManualReview:
                _logger.LogInformation("Требует ручной проверки: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;
        }
    }

    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            if (chat.Type != ChatType.Supergroup)
                return false;

            // Проверяем, является ли это автоматическим пересыланием из канала
            var isAutoForward = message.IsAutomaticForward;
            
            if (isAutoForward)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, autoForward={AutoForward}", 
                    chat.Id, message.IsAutomaticForward);
            }
            
            return isAutoForward;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return false;
        }
    }

    private async Task BanUserForLongName(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage?.Chat!;
            
            // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
            if (chat.Type == ChatType.Private)
            {
                _logger.LogWarning("Попытка бана за длинное имя в приватном чате {ChatId} - операция невозможна", chat.Id);
                var errorData = new ErrorNotificationData(
                    new InvalidOperationException("Попытка бана в приватном чате"),
                    "Бан за длинное имя",
                    user,
                    chat
                );
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
                return;
            }
            
            await _bot.BanChatMember(
                chat.Id, 
                user.Id,
                banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null,
                revokeMessages: true
            );
            
            if (userJoinMessage != null)
            {
                await _bot.DeleteMessage(userJoinMessage.Chat.Id, userJoinMessage.MessageId, cancellationToken);
            }

            var banType = banDuration.HasValue ? "Автобан на 10 минут" : "🚫 Перманентный бан";
            var banData = new AutoBanNotificationData(user, chat, banType, reason, userJoinMessage?.MessageId);
            
            // Отправляем уведомление только в лог-чат
            if (userJoinMessage != null)
            {
                await _messageService.ForwardToLogWithNotificationAsync(userJoinMessage, LogNotificationType.BanForLongName, banData, cancellationToken);
            }
            else
            {
                await _messageService.SendLogNotificationAsync(LogNotificationType.BanForLongName, banData, cancellationToken);
            }
            
            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя за длинное имя");
        }
    }

    private async Task BanBlacklistedUser(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage.Chat;
            
            // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
            if (chat.Type == ChatType.Private)
            {
                _logger.LogWarning("Попытка бана из блэклиста в приватном чате {ChatId} - операция невозможна", chat.Id);
                var errorData = new ErrorNotificationData(
                    new InvalidOperationException("Попытка бана в приватном чате"),
                    "Бан из блэклиста",
                    user,
                    chat
                );
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
                return;
            }
            
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: true, cancellationToken: cancellationToken);
            
            await _bot.DeleteMessage(chat.Id, userJoinMessage.MessageId, cancellationToken);
            
            _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
    }

    private async Task AutoBanChannel(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var chat = message.Chat;
            var senderChat = message.SenderChat!;
            
            await _bot.DeleteMessage(chat, message.MessageId, cancellationToken);
            await _bot.BanChatSenderChat(chat, senderChat.Id, cancellationToken);
            
            var channelData = new ChannelMessageNotificationData(senderChat, chat, message.Text ?? "[медиа]");
            await _messageService.ForwardToAdminWithNotificationAsync(message, AdminNotificationType.ChannelMessage, channelData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить канал");
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Не удалось забанить канал"),
                "Не хватает могущества",
                null,
                message.Chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.ChannelError, errorData, cancellationToken);
        }
    }

    private async Task AutoBan(Message message, string reason, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;
        
        // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
        if (chat.Type == ChatType.Private)
        {
            _logger.LogWarning("Попытка бана в приватном чате {ChatId} - операция невозможна", chat.Id);
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Попытка бана в приватном чате"),
                reason,
                user,
                chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
            return;
        }
        
        // Форвардим сообщение в лог-чат с уведомлением
        var autoBanData = new AutoBanNotificationData(
            user, 
            message.Chat, 
            "Автобан", 
            reason, 
            message.MessageId, 
            LinkToMessage(message.Chat, message.MessageId)
        );
        
        // Выбираем правильный тип уведомления в зависимости от причины
        var logNotificationType = reason.Contains("Известное спам-сообщение") 
            ? LogNotificationType.AutoBanKnownSpam 
            : LogNotificationType.AutoBanBlacklist;
            
        await _messageService.ForwardToLogWithNotificationAsync(message, logNotificationType, autoBanData, cancellationToken);
        
        await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: cancellationToken);
        
        // Полностью очищаем пользователя из всех списков
        _moderationService.CleanupUserFromAllLists(user.Id, message.Chat.Id);
    }

    private async Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Начинаем DeleteAndReportMessage для сообщения {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
        
        var user = message.From;
        var deletionMessagePart = $"{reason}";

        try
        {
            // Создаем кнопки реакции для админ-чата
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }
            });

            var deletionData = new AutoBanNotificationData(
                user, 
                message.Chat, 
                deletionMessagePart, 
                reason, 
                message.MessageId, 
                LinkToMessage(message.Chat, message.MessageId)
            );
            
            // Получаем шаблон и форматируем сообщение
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.AutoBan);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, deletionData);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 **Тихий режим**\n\n{messageText}";
            }
            
            // Пересылаем сообщение
            await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            
            // Отправляем уведомление с кнопками
            await _bot.SendMessage(
                Config.AdminChatId,
                messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Уведомление с кнопками успешно отправлено в админ-чат");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить уведомление в админ-чат");
            // Fallback убран - автобаны теперь идут только в лог-чат
            _logger.LogDebug("Не удалось отправить уведомление в админ-чат, но это ожидаемое поведение для автобанов");
        }
        
        try
        {
            _logger.LogDebug("Пытаемся удалить сообщение {MessageId} из чата {ChatId}", message.MessageId, message.Chat.Id);
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
            deletionMessagePart += ", сообщение удалено.";
            _logger.LogDebug("Сообщение успешно удалено");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete message {MessageId} from chat {ChatId}", message.MessageId, message.Chat.Id);
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }

        // Отправляем предупреждение пользователю (только если не было отправлено недавно и не тихий режим)
        if (!isSilentMode)
        {
            var warningKey = $"warning_{message.Chat.Id}_{user.Id}";
            var existingWarning = MemoryCache.Default.Get(warningKey);
            
            if (existingWarning == null)
            {
                try
                {
                    var warningData = new SimpleNotificationData(user, message.Chat, "новичок в этом чате");
                    var sentWarn = await _messageService.SendUserNotificationWithReplyAsync(
                        user, 
                        message.Chat, 
                        UserNotificationType.ModerationWarning, 
                        warningData, 
                        cancellationToken
                    );
                    
                    // Сохраняем ID предупреждающего сообщения в кэше (на 10 минут, чтобы не спамить)
                    MemoryCache.Default.Add(warningKey, sentWarn.MessageId, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                    
                    DeleteMessageLater(sentWarn, TimeSpan.FromSeconds(40), cancellationToken);
                    _logger.LogDebug("Предупреждение отправлено пользователю и будет удалено через 40 секунд");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось отправить предупреждение пользователю");
                }
            }
            else
            {
                _logger.LogDebug("Предупреждение пользователю {UserId} в чате {ChatId} уже было отправлено недавно, пропускаем", user.Id, message.Chat.Id);
            }
        }
    }

    private async Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken)
    {
        try
        {
            var suspiciousData = new SuspiciousMessageNotificationData(
                user, 
                message.Chat, 
                message.Text ?? message.Caption ?? "[медиа]", 
                message.MessageId
            );
            
            // Отправляем уведомление с кнопками для подозрительного сообщения (форвард делается внутри)
            await SendSuspiciousMessageWithButtons(message, user, suspiciousData, isSilentMode, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при пересылке сообщения");
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Не удалось переслать подозрительное сообщение"),
                "Ошибка пересылки",
                user,
                message.Chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemError, errorData, cancellationToken);
        }
    }
    
    private async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        try
        {
            // Сначала пересылаем оригинальное сообщение
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.SuspiciousMessage);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, data);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 **Тихий режим**\n\n{messageText}";
            }
            
            // Создаем кнопки реакции для админ-чата (стандартные кнопки: бан, пропуск, свой)
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }
            });
            
            // Отправляем уведомление с кнопками как ответ на форвард
            await _bot.SendMessage(
                Config.AdminChatId,
                messageText,
                parseMode: ParseMode.Markdown,
                replyParameters: forward,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено подозрительное сообщение с кнопками для пользователя {User} в чате {Chat}", 
                Utils.FullName(user), message.Chat.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке подозрительного сообщения с кнопками");
            // Fallback: отправляем без кнопок
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, data, cancellationToken);
        }
    }

    /// <summary>
    /// Обработка бана пользователя по блэклисту lols.bot при первом сообщении
    /// </summary>
    private async Task HandleBlacklistBan(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _logger.LogWarning("🚫 БЛЭКЛИСТ LOLS.BOT: {UserName} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) написал: {MessageText}", 
            FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id, 
            messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText);
        
        _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot");
        
        // Пересылаем сообщение в лог-чат с уведомлением
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
            await _messageService.ForwardToLogWithNotificationAsync(message, LogNotificationType.AutoBanBlacklist, blacklistData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение в лог-чат");
        }
        
        // Удаляем сообщение
        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить сообщение пользователя из блэклиста");
        }
        
        // Баним пользователя на 4 часа (как в IntroFlowService)
        try
        {
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, untilDate: banUntil, revokeMessages: true, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
        
        // Обновляем статистику
        _statisticsService.IncrementBlacklistBan(message.Chat.Id);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
        
        // Удаляем из списка одобренных
        if (_userManager.RemoveApproval(user.Id))
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
        
        _logger.LogInformation("✅ АВТОБАН ЗАВЕРШЕН: пользователь {User} (id={UserId}) забанен на 4 часа в чате '{ChatTitle}' (id={ChatId}) по блэклисту lols.bot", 
            FullName(user.FirstName, user.LastName), user.Id, message.Chat.Title, message.Chat.Id);
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    private void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Выполняет AI анализ профиля пользователя при первом сообщении
    /// </summary>
    /// <returns>true если пользователь получил ограничения за подозрительный профиль</returns>
    private async Task<bool> PerformAiProfileAnalysis(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🤖 Запускаем AI анализ профиля пользователя {UserId} ({UserName})", 
            user.Id, FullName(user.FirstName, user.LastName));
        _logger.LogDebug("🔍 TRACE: PerformAiProfileAnalysis начат для пользователя {UserId}", user.Id);
        
        try
        {
            var messageText = message.Text ?? message.Caption ?? "";
            var result = await _aiChecks.GetAttentionBaitProbability(user, messageText);
            _logger.LogDebug("🔍 TRACE: AiChecks.GetAttentionBaitProbability завершен для пользователя {UserId}", user.Id);
            _logger.LogInformation("🤖 AI анализ профиля: пользователь {UserId}, вероятность={Probability}, причина={Reason}", 
                user.Id, result.SpamProbability.Probability, result.SpamProbability.Reason);

            // Проверяем пороги вероятности спама
            if (result.SpamProbability.Probability >= Consts.LlmLowProbability) // >= 0.75
            {
                _logger.LogWarning("🚫 AI определил подозрительный профиль: пользователь {UserId}, вероятность={Probability}", 
                    user.Id, result.SpamProbability.Probability);

                // РЕФАКТОРИНГ: Определяем нужно ли удалять сообщение, но НЕ удаляем его сразу
                // Сначала отправляем AI анализ с пересылкой, потом удаляем
                // НОВАЯ ЛОГИКА: удаляем при высокой вероятности ИЛИ при средней вероятности + банальное приветствие
                var isBoringGreeting = AiChecks.IsBoringGreeting(message.Text ?? message.Caption);
                var shouldDeleteMessage = result.SpamProbability.Probability >= Consts.LlmHighProbability || // >= 0.9
                                         (result.SpamProbability.Probability >= Consts.LlmLowProbability && isBoringGreeting); // >= 0.75 + банальное приветствие

                // Даем ридонли на 10 минут в любом случае
                try
                {
                    var untilDate = DateTime.UtcNow.AddMinutes(10);
                    await _bot.RestrictChatMember(
                        chat.Id, 
                        user.Id, 
                        new ChatPermissions
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
                        untilDate: (DateTime?)untilDate,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось дать ридонли пользователю");
                }

                // Отправляем красивое уведомление в админ-чат
                var automaticAction = shouldDeleteMessage 
                    ? (result.SpamProbability.Probability >= Consts.LlmHighProbability 
                        ? "🗑️ Сообщение удалено (высокая вероятность спама) + 🔇 Read-Only на 10 минут"
                        : "🗑️ Сообщение удалено (банальное приветствие от подозрительного профиля) + 🔇 Read-Only на 10 минут")
                    : "🔇 Read-Only на 10 минут (сообщение оставлено)";
                    
                var aiProfileData = new AiProfileAnalysisData(
            user, 
            chat, 
            result.SpamProbability.Probability, 
            result.SpamProbability.Reason, 
            result.NameBio, 
            message.Text ?? message.Caption ?? "[медиа]", 
            result.Photo, 
            message.MessageId,
            automaticAction
        );
        await _messageService.SendAiProfileAnalysisAsync(aiProfileData, cancellationToken);

                // РЕФАКТОРИНГ: Теперь удаляем сообщение ПОСЛЕ отправки AI анализа (чтобы пересылка работала)
                if (shouldDeleteMessage)
                {
                    try
                    {
                        await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                        if (result.SpamProbability.Probability >= Consts.LlmHighProbability)
                        {
                            _logger.LogInformation("🗑️ Сообщение удалено из-за высокой вероятности спама: {Probability:F2}", result.SpamProbability.Probability);
                        }
                        else
                        {
                            _logger.LogInformation("🗑️ Сообщение удалено: банальное приветствие от подозрительного профиля (вероятность: {Probability:F2}, приветствие: '{Message}')", 
                                result.SpamProbability.Probability, message.Text ?? message.Caption ?? "[медиа]");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить сообщение при AI анализе");
                    }
                }
                else
                {
                    _logger.LogInformation("💬 Сообщение НЕ удалено (средняя вероятность): {Probability:F2}", result.SpamProbability.Probability);
                }

                _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
                _userFlowLogger.LogUserRestricted(user, chat, $"AI анализ профиля: {result.SpamProbability.Reason}", TimeSpan.FromMinutes(10));
                return true; // Возвращаем true - пользователь получил ограничения
            }
            else
            {
                _logger.LogDebug("✅ AI анализ: профиль пользователя {UserId} выглядит безопасно (вероятность={Probability})", 
                    user.Id, result.SpamProbability.Probability);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
            // Продолжаем выполнение даже при ошибке AI анализа
        }

        return false; // Возвращаем false - профиль безопасен, продолжаем модерацию
    }


} 