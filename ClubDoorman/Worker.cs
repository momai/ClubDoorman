using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal sealed class Worker(
    ILogger<Worker> logger,
    SpamHamClassifier classifier,
    UserManager userManager,
    BadMessageManager badMessageManager
) : BackgroundService
{
    private sealed record CaptchaInfo(
        long ChatId,
        string? ChatTitle,
        DateTime Timestamp,
        User User,
        int CorrectAnswer,
        CancellationTokenSource Cts,
        Message? UserJoinedMessage
    );

    private sealed class Stats(string? Title)
    {
        public string? ChatTitle = Title;
        public int StoppedCaptcha;
        public int BlacklistBanned;
        public int KnownBadMessage;
    }

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly TelegramBotClient _bot = new(Config.BotApi);
    private readonly ConcurrentDictionary<long, Stats> _stats = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    private readonly ILogger<Worker> _logger = logger;
    private readonly SpamHamClassifier _classifier = classifier;
    private readonly UserManager _userManager = userManager;
    private readonly BadMessageManager _badMessageManager = badMessageManager;
    private User _me = default!;
    private readonly PeriodicTimer _saveTimer = new(TimeSpan.FromMinutes(15));
    private const string MessageCountFilePath = "data/message_counts.txt";

    private async Task CaptchaLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
            _ = BanNoCaptchaUsers();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = CaptchaLoop(stoppingToken);
        _ = ReportStatistics(stoppingToken);
        _ = SaveMessageCounts(stoppingToken);
        _classifier.Touch();
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (System.IO.File.Exists(offsetPath))
        {
            var lines = await System.IO.File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                _logger.LogDebug("offset read ok");
        }

        if (System.IO.File.Exists(MessageCountFilePath))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(MessageCountFilePath, stoppingToken);
                var restoredData = JsonSerializer.Deserialize<ConcurrentDictionary<long, int>>(json);
                if (restoredData != null)
                {
                    foreach (var kvp in restoredData)
                    {
                        _goodUserMessages[kvp.Key] = kvp.Value;
                    }
                    _logger.LogInformation("Restored message counts from file.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to restore message counts from file.");
            }
        }

        _me = await _bot.GetMeAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, stoppingToken);
                if (offset % 100 == 0)
                    await System.IO.File.WriteAllTextAsync(offsetPath, offset.ToString(), stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<int> UpdateLoop(int offset, CancellationToken stoppingToken)
    {
        var updates = await _bot.GetUpdatesAsync(
            offset,
            limit: 100,
            timeout: 100,
            allowedUpdates: [UpdateType.Message, UpdateType.ChatMember, UpdateType.CallbackQuery],
            cancellationToken: stoppingToken
        );
        if (updates.Length == 0)
            return offset;
        offset = updates.Max(x => x.Id) + 1;
        string? mediaGroupId = null;
        foreach (var update in updates)
        {
            try
            {
                var prevMediaGroup = mediaGroupId;
                mediaGroupId = update.Message?.MediaGroupId;
                if (prevMediaGroup != null && prevMediaGroup == mediaGroupId)
                {
                    _logger.LogDebug("2+ message from an album, it could not have any text/caption, skip");
                    continue;
                }
                await HandleUpdate(update, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "UpdateLoop");
            }
        }
        return offset;
    }

    private async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
        if (update.CallbackQuery != null)
        {
            await HandleCallback(update);
            return;
        }
        if (update.ChatMember != null)
        {
            if (update.ChatMember.From.Id == _me.Id)
                return;
            await HandleChatMemberUpdated(update);
            return;
        }

        var message = update.Message;
        if (message == null)
            return;
        // Handle the new /approve command for adding users to approved-users.txt
        if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            if (message.Text != null && message.Text.StartsWith("/approve") && message.ReplyToMessage != null)
            {
                await HandleApproveCommand(message);
                return;
            }
        }

        var chat = message.Chat;
        if (message.NewChatMembers != null && chat.Id != Config.AdminChatId)
        {
            foreach (var newUser in message.NewChatMembers.Where(x => !x.IsBot))
                await IntroFlow(message, newUser);
            return;
        }

        if (chat.Id == Config.AdminChatId)
        {
            await AdminChatMessage(message);
            return;
        }

        var sentAsChannel = false;
        if (message.SenderChat != null)
        {
            sentAsChannel = true;
            // to get linked_chat_id we need ChatFullInfo
            var chatFull = await _bot.GetChatAsync(chat, stoppingToken);
            var linked = chatFull.LinkedChatId;
            if (linked != null && linked == message.SenderChat.Id)
                return;

            if (Config.ChannelAutoBan)
            {
                try
                {
                    var fwd = await _bot.ForwardMessageAsync(Config.AdminChatId, chat, message.MessageId, cancellationToken: stoppingToken);
                    await _bot.DeleteMessageAsync(chat, message.MessageId, stoppingToken);
                    await _bot.BanChatSenderChatAsync(chat, message.SenderChat.Id, stoppingToken);
                    await _bot.SendTextMessageAsync(
                        Config.AdminChatId,
                        $"Сообщение удалено, в чате {chat.Title} забанен канал {message.SenderChat.Title}",
                        replyToMessageId: fwd.MessageId,
                        cancellationToken: stoppingToken
                    );
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to ban");
                    await _bot.SendTextMessageAsync(
                        Config.AdminChatId,
                        $"Не могу удалить или забанить в чате {chat.Title} сообщение от имени канала {message.SenderChat.Title}. Не хватает могущества?",
                        cancellationToken: stoppingToken
                    );
                }
                return;
            }
        }

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        if (text != null)
            MemoryCache.Default.Set(
                new CacheItem($"{chat.Id}_{user.Id}", text),
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
            );

        var key = UserToKey(chat.Id, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            await _bot.DeleteMessageAsync(chat.Id, message.MessageId, stoppingToken);
            return;
        }

        if (_userManager.Approved(user.Id))
            return;

        _logger.LogDebug("First-time message, chat {Chat}, message {Message}", chat.Title, text);

        // At this point we are believing we see first-timers, and we need to check for spam
        var name = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(name))
        {
            _logger.LogDebug("User is {Name} from club", name);
            return;
        }
        if (await _userManager.InBanlist(user.Id))
        {
            if (Config.BlacklistAutoBan)
            {
                var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
                Interlocked.Increment(ref stats.BlacklistBanned);
                await _bot.BanChatMemberAsync(chat.Id, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
                await _bot.DeleteMessageAsync(chat.Id, message.MessageId, stoppingToken);
            }
            else
            {
                const string reason = "Пользователь в блеклисте спамеров";
                await DeleteAndReportMessage(message, user, reason, stoppingToken);
            }
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            await DontDeleteButReportMessage(message, user, stoppingToken);
            return;
        }
        if (_badMessageManager.KnownBadMessage(text))
        {
            await HandleBadMessage(message, user, stoppingToken);
            return;
        }
        if (SimpleFilters.TooManyEmojis(text))
        {
            const string reason = "В этом сообщении многовато эмоджи";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        var normalized = TextProcessor.NormalizeText(text);

        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 1)
        {
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        if (SimpleFilters.HasStopWords(normalized))
        {
            const string reason = "В этом сообщении есть стоп-слова";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }
        var (spam, score) = await _classifier.IsSpam(normalized);
        if (spam)
        {
            var reason = $"ML решил что это спам, скор {score}";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        if (sentAsChannel)
        {
            await DontDeleteButReportMessage(message, user, stoppingToken);
            return;
        }
        // else - ham
        if (score > -0.7 && Config.LowConfidenceHamForward)
        {
            var forward = await _bot.ForwardMessageAsync(Config.AdminChatId, chat.Id, message.MessageId, cancellationToken: stoppingToken);
            var postLink = LinkToMessage(chat, message.MessageId);
            await _bot.SendTextMessageAsync(
                Config.AdminChatId,
                $"Классифаер думает что это НЕ спам, но конфиденс низкий: скор {score}. Хорошая идея - добавить сообщение в датасет.{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {chat.Title}{Environment.NewLine}{postLink}",
                replyToMessageId: forward.MessageId,
                cancellationToken: stoppingToken
            );
        }
        _logger.LogDebug("Classifier thinks its ham, score {Score}", score);

        // Now we need a mechanism for users who have been writing non-spam for some time
        var goodInteractions = _goodUserMessages.AddOrUpdate(user.Id, 1, (_, oldValue) => oldValue + 1);
        if (goodInteractions >= 3)
        {
            _logger.LogInformation(
                "User {FullName} behaved well for the last {Count} messages, approving",
                FullName(user.FirstName, user.LastName),
                goodInteractions
            );
            await _userManager.Approve(user.Id);
            _goodUserMessages.TryRemove(user.Id, out _);
        }
    }
    private async Task HandleApproveCommand(Message message)
    {
        // Проверяем, существует ли сообщение, на которое был отправлен ответ
        var replyMessage = message.ReplyToMessage;
        if (replyMessage == null)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Не могу определить сообщение для одобрения. Попробуйте ответить на сообщение юзера.");
            return;
        }

        // Если сообщение было переслано ботом, ищем реального отправителя в поле ForwardFrom
        var userToApprove = replyMessage.ForwardFrom;
        if (userToApprove == null)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Не могу определить пользователя для одобрения. Убедитесь, что вы отвечаете на пересланное сообщение с валидным автором.");
            return;
        }

        // Добавляем пользователя в список одобренных
        await _userManager.Approve(userToApprove.Id);
        await _bot.SendTextMessageAsync(
            message.Chat.Id, 
            $"Пользователь {FullName(userToApprove.FirstName, userToApprove.LastName)} одобрен и добавлен в approved-users.txt.",
            replyToMessageId: message.MessageId
        );
        _logger.LogInformation("User {FullName} was approved by admin", FullName(userToApprove.FirstName, userToApprove.LastName));
    }
    private async Task SaveMessageCounts(CancellationToken token)
    {
        while (await _saveTimer.WaitForNextTickAsync(token))
        {
            try
            {
                var data = JsonSerializer.Serialize(_goodUserMessages);
                await System.IO.File.WriteAllTextAsync(MessageCountFilePath, data, token);
                _logger.LogInformation("Message counts saved to file.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to save message counts to file.");
            }
        }
    }
    private async Task HandleBadMessage(Message message, User user, CancellationToken stoppingToken)
    {
        try
        {
            var chat = message.Chat;
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.KnownBadMessage);
            await _bot.DeleteMessageAsync(chat, message.MessageId, stoppingToken);
            await _bot.BanChatMemberAsync(chat.Id, user.Id, cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
        }
    }

    private async Task HandleCallback(Update update)
    {
        var cb = update.CallbackQuery;
        Debug.Assert(cb != null);
        var cbData = cb.Data;
        if (cbData == null)
            return;
        var message = cb.Message;
        if (message == null || message.Chat.Id == Config.AdminChatId)
            await HandleAdminCallback(cbData, cb);
        else
            await HandleCaptchaCallback(update);
    }

    private async Task HandleCaptchaCallback(Update update)
    {
        var cb = update.CallbackQuery;
        Debug.Assert(cb != null);
        var cbData = cb.Data;
        if (cbData == null)
            return;
        var message = cb.Message;
        Debug.Assert(message != null);

        //$"cap_{user.Id}_{x}"
        var split = cbData.Split('_');
        if (split.Length < 3)
            return;
        if (split[0] != "cap")
            return;
        if (!long.TryParse(split[1], out var userId))
            return;
        if (!int.TryParse(split[2], out var chosen))
            return;
        // Prevent other people from ruining the flow
        if (cb.From.Id != userId)
        {
            await _bot.AnswerCallbackQueryAsync(cb.Id);
            return;
        }

        var chat = message.Chat;
        var key = UserToKey(chat.Id, cb.From);
        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await _bot.DeleteMessageAsync(chat, message.MessageId);
        if (!ok)
        {
            _logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        await info.Cts.CancelAsync();
        if (info.CorrectAnswer != chosen)
        {
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.StoppedCaptcha);
            await _bot.BanChatMemberAsync(chat, userId, DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: false);
            if (info.UserJoinedMessage != null)
                await _bot.DeleteMessageAsync(chat, info.UserJoinedMessage.MessageId);
            UnbanUserLater(chat, userId);
        }
    }

    private async ValueTask IntroFlow(Message? userJoinMessage, User user, Chat? chat = default)
    {
        _logger.LogDebug("Intro flow {@User}", user);
        if (_userManager.Approved(user.Id))
            return;
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
            return;

        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, userJoinMessage?.Chat ?? chat))
            return;

        var key = UserToKey(chatId, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            return;
        }

        const int challengeLength = 8;
        var correctAnswerIndex = Random.Shared.Next(challengeLength);
        var challenge = new List<int>(challengeLength);
        while (challenge.Count < challengeLength)
        {
            var rand = Random.Shared.Next(Captcha.CaptchaList.Count);
            if (!challenge.Contains(rand))
                challenge.Add(rand);
        }
        var correctAnswer = challenge[correctAnswerIndex];
        var keyboard = challenge
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) { CallbackData = $"cap_{user.Id}_{x}" })
            .ToList();

        var del =
            userJoinMessage != null
                ? await _bot.SendTextMessageAsync(
                    chatId,
                    $"Привет! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                    replyToMessageId: userJoinMessage.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                )
                : await _bot.SendTextMessageAsync(
                    chatId,
                    $"Привет {AtUserNameOrFirstLast()}! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                );

        var cts = new CancellationTokenSource();
        DeleteMessageLater(del, TimeSpan.FromMinutes(1.2), cts.Token);
        if (userJoinMessage != null)
        {
            DeleteMessageLater(userJoinMessage, TimeSpan.FromMinutes(1.2), cts.Token);
            _captchaNeededUsers.TryAdd(
                key,
                new CaptchaInfo(chatId, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, userJoinMessage)
            );
        }
        else
        {
            _captchaNeededUsers.TryAdd(key, new CaptchaInfo(chatId, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, null));
        }

        return;

        string AtUserNameOrFirstLast()
        {
            if (user.Username != null)
                return "@" + user.Username;
            return FullName(user.FirstName, user.LastName);
        }
    }

    private async Task ReportStatistics(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
        {
            if (DateTimeOffset.UtcNow.Hour != 12)
                continue;

            var report = _stats.ToArray();
            _stats.Clear();
            var sb = new StringBuilder();
            sb.Append("За последние 24 часа в чатах:");
            foreach (var (_, stats) in report.OrderBy(x => x.Value.ChatTitle))
            {
                sb.Append(Environment.NewLine);
                sb.Append("В ");
                sb.Append(stats.ChatTitle);
                var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha;
                sb.Append($": {sum} раза сработала защита автоматом{Environment.NewLine}");
                sb.Append(
                    $"По блеклистам известных аккаунтов спамеров забанено: {stats.BlacklistBanned}, не прошло капчу: {stats.StoppedCaptcha}, за известные спам сообщения забанено: {stats.KnownBadMessage}"
                );
            }

            try
            {
                await _bot.SendTextMessageAsync(Config.AdminChatId, sb.ToString(), cancellationToken: ct);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to sent report to admin chat");
            }
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat)
    {
        if (!Config.BlacklistAutoBan)
            return false;
        if (!await _userManager.InBanlist(user.Id))
            return false;

        try
        {
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.BlacklistBanned);
            await _bot.BanChatMemberAsync(chat.Id, user.Id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
            await _bot.SendTextMessageAsync(
                Config.AdminChatId,
                $"Не могу забанить юзера из блеклиста. Не хватает могущества? Сходите забаньте руками, чат {chat.Title}"
            );
        }

        return false;
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private async Task HandleAdminCallback(string cbData, CallbackQuery cb)
    {
        var split = cbData.Split('_').ToList();
        if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId) && long.TryParse(split[2], out var userId))
        {
            var userMessage = MemoryCache.Default.Remove(cbData) as Message;
            var text = userMessage?.Caption ?? userMessage?.Text;
            if (!string.IsNullOrWhiteSpace(text))
                await _badMessageManager.MarkAsBad(text);
            try
            {
                await _bot.BanChatMemberAsync(new ChatId(chatId), userId);
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"{FullName(cb.From.FirstName, cb.From.LastName)} забанил",
                    replyToMessageId: cb.Message?.MessageId
                );
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to ban");
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"Не могу забанить. Не хватает могущества? Сходите забаньте руками",
                    replyToMessageId: cb.Message?.MessageId
                );
            }
            try
            {
                if (userMessage != null)
                    await _bot.DeleteMessageAsync(userMessage.Chat, userMessage.MessageId);
            }
            catch { }
        }
        var msg = cb.Message;
        if (msg != null)
            await _bot.EditMessageReplyMarkupAsync(msg.Chat.Id, msg.MessageId);
    }

    private async Task HandleChatMemberUpdated(Update update)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        var newChatMember = chatMember.NewChatMember;
        switch (newChatMember.Status)
        {
            case ChatMemberStatus.Member:
            {
                _logger.LogDebug("New chat member new {@New} old {@Old}", newChatMember, chatMember.OldChatMember);
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    // The reason we need to wait here is that we need to get message that user joined to have a chance to be processed first,
                    // this is not mandatory but looks nicer, however sometimes Telegram doesn't send it at all so consider this a fallback.
                    // There is no way real human would be able to solve this captcha in under 2 seconds so it's fine.
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        await IntroFlow(null, newChatMember.User, chatMember.Chat);
                    });
                }

                break;
            }
            case ChatMemberStatus.Kicked
            or ChatMemberStatus.Restricted:
                var user = newChatMember.User;
                var key = $"{chatMember.Chat.Id}_{user.Id}";
                var lastMessage = MemoryCache.Default.Get(key) as string;
                var tailMessage = string.IsNullOrWhiteSpace(lastMessage)
                    ? ""
                    : $" Его/её последним сообщением было:{Environment.NewLine}{lastMessage}";
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"В чате {chatMember.Chat.Title} юзеру {FullName(user.FirstName, user.LastName)} tg://user?id={user.Id} дали ридонли или забанили, посмотрите в Recent actions, возможно ML пропустил спам. Если это так - кидайте его сюда.{tailMessage}"
                );
                break;
        }
    }

    private async Task DontDeleteButReportMessage(Message message, User user, CancellationToken stoppingToken)
    {
        var forward = await _bot.ForwardMessageAsync(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        var callbackData = $"ban_{message.Chat.Id}_{user.Id}";
        MemoryCache.Default.Add(callbackData, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
        await _bot.SendTextMessageAsync(
            new ChatId(Config.AdminChatId),
            $"Это подозрительное сообщение - например, картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, или сообщение от канала. Сообщение НЕ удалено.{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}",
            replyToMessageId: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                [
                    new InlineKeyboardButton("🤖 ban") { CallbackData = callbackData },
                    new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }
                ]
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task DeleteAndReportMessage(Message message, User user, string reason, CancellationToken stoppingToken)
    {
        var forward = await _bot.ForwardMessageAsync(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        var deletionMessagePart = $"{reason}";
        try
        {
            await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            deletionMessagePart += ", сообщение удалено.";
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete");
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }

        var callbackData = $"ban_{message.Chat.Id}_{user.Id}";
        MemoryCache.Default.Add(callbackData, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
        var postLink = LinkToMessage(message.Chat, message.MessageId);

        await _bot.SendTextMessageAsync(
            new ChatId(Config.AdminChatId),
            $"{deletionMessagePart}{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}",
            replyToMessageId: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                [
                    new InlineKeyboardButton("🤖 ban") { CallbackData = callbackData },
                    new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }
                ]
            ),
            cancellationToken: stoppingToken
        );
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup
            ? LinkToSuperGroupMessage(chat, messageId)
            : chat.Username == null
                ? ""
                : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    private async Task AdminChatMessage(Message message)
    {
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardFrom == null && replyToMessage.ForwardSenderName == null)
            {
                await _bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Похоже, что вы промахнулись и реплайнули на сообщение бота, а не форвард",
                    replyToMessageId: replyToMessage.MessageId
                );
                return;
            }

            var text = replyToMessage.Text ?? replyToMessage.Caption;
            if (!string.IsNullOrWhiteSpace(text))
            {
                switch (message.Text)
                {
                    case "/check":
                    {
                        var emojis = SimpleFilters.TooManyEmojis(text);
                        var normalized = TextProcessor.NormalizeText(text);
                        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
                        var hasStopWords = SimpleFilters.HasStopWords(normalized);
                        var (spam, score) = await _classifier.IsSpam(normalized);
                        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
                        var msg =
                            $"Результат:{Environment.NewLine}"
                            + $"Много эмодзи: {emojis}{Environment.NewLine}"
                            + $"Найдены стоп-слова: {hasStopWords}{Environment.NewLine}"
                            + $"Маскирующиеся слова: {lookAlikeMsg}{Environment.NewLine}"
                            + $"ML классификатор: спам {spam}, скор {score}{Environment.NewLine}{Environment.NewLine}"
                            + $"Если простые фильтры отработали, то в датасет добавлять не нужно";
                        await _bot.SendTextMessageAsync(message.Chat.Id, msg);
                        break;
                    }
                    case "/spam":
                        await _classifier.AddSpam(text);
                        await _badMessageManager.MarkAsBad(text);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример спама в датасет",
                            replyToMessageId: replyToMessage.MessageId
                        );
                        break;
                    case "/ham":
                        await _classifier.AddHam(text);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример НЕ-спама в датасет",
                            replyToMessageId: replyToMessage.MessageId
                        );
                        break;
                }
            }
        }
        else if (message.Text == "/approve" && message.ReplyToMessage != null)
        {
            var originalMessage = message.ReplyToMessage;

            if (originalMessage.ForwardFrom != null && originalMessage.ForwardFrom.Id != _me.Id)
            {
                var userToApprove = originalMessage.ForwardFrom;

                await _userManager.Approve(userToApprove.Id);
                await _bot.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Пользователь {FullName(userToApprove.FirstName, userToApprove.LastName)} одобрен и добавлен в approved-users.txt.",
                    replyToMessageId: originalMessage.MessageId
                );
                _logger.LogInformation("User {FullName} was approved by admin", FullName(userToApprove.FirstName, userToApprove.LastName));
            }
            else
            {
                await _bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Не могу определить пользователя для одобрения. Убедитесь, что команда /approve используется в ответ на пересланное сообщение с валидным автором.",
                    replyToMessageId: message.ReplyToMessage.MessageId
                );
            }
        }
    }

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";

    private async Task BanNoCaptchaUsers()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;
        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        foreach (var (key, (chatId, title, timestamp, user, _, _, _)) in users)
        {
            var minutes = (now - timestamp).TotalMinutes;
            if (minutes > 1)
            {
                var stats = _stats.GetOrAdd(chatId, new Stats(title));
                Interlocked.Increment(ref stats.StoppedCaptcha);
                _captchaNeededUsers.TryRemove(key, out _);
                await _bot.BanChatMemberAsync(chatId, user.Id, now + TimeSpan.FromMinutes(20), revokeMessages: false);
                UnbanUserLater(chatId, user.Id);
            }
        }
    }

    private class CaptchaAttempts
    {
        public int Attempts { get; set; }
    }

    private void UnbanUserLater(ChatId chatId, long userId)
    {
        var key = $"captcha_{userId}";
        var cache = MemoryCache.Default.AddOrGetExisting(
            new CacheItem(key, new CaptchaAttempts()),
            new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(4) }
        );
        var attempts = (CaptchaAttempts)cache.Value;
        attempts.Attempts++;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Exp(attempts.Attempts)));
                await _bot.UnbanChatMemberAsync(chatId, userId);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, nameof(UnbanUserLater));
            }
        });
    }

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
                    await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessageAsync");
                }
            },
            cancellationToken
        );
    }
}
