using System.Collections.Concurrent;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с капчей
/// </summary>
public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<CaptchaService> _logger;
    private readonly IMessageService _messageService;

    // Черный список имен для отображения
    private readonly List<string> _namesBlacklist = ["p0rn", "porn", "порн", "п0рн", "pоrn", "пoрн", "bot"];

    /// <summary>
    /// Создает экземпляр сервиса капчи.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="logger">Логгер для записи событий</param>
    public CaptchaService(ITelegramBotClientWrapper bot, ILogger<CaptchaService> logger, IMessageService messageService)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    }

    /// <summary>
    /// Создает капчу для нового пользователя в чате, либо возвращает null, если капча отключена для чата.
    /// </summary>
    /// <param name="chat">Чат, в котором создается капча</param>
    /// <param name="user">Пользователь, для которого создается капча</param>
    /// <param name="userJoinMessage">Сообщение о присоединении пользователя (опционально)</param>
    /// <returns>Информация о созданной капче или null, если капча отключена для чата</returns>
    /// <exception cref="ArgumentNullException">Если chat или user равны null</exception>
    public async Task<CaptchaInfo?> CreateCaptchaAsync(Chat chat, User user, Message? userJoinMessage = null)
    {
        if (chat == null) throw new ArgumentNullException(nameof(chat));
        if (user == null) throw new ArgumentNullException(nameof(user));

        // Отключение капчи для определённых групп
        if (Config.NoCaptchaGroups.Contains(chat.Id))
        {
            _logger.LogInformation($"[NO_CAPTCHA] Капча отключена для чата {chat.Id}");
            return null;
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
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) 
            { 
                CallbackData = $"cap_{user.Id}_{x}" 
            })
            .ToList();

        ReplyParameters? replyParams = null;
        if (userJoinMessage != null)
            replyParams = userJoinMessage;

        var fullNameForDisplay = Utils.FullName(user);
        var fullNameLower = fullNameForDisplay.ToLowerInvariant();
        var username = user.Username?.ToLower();
        
        if (_namesBlacklist.Any(fullNameLower.Contains) || 
            username?.Contains("porn") == true || 
            username?.Contains("p0rn") == true)
        {
            fullNameForDisplay = "новый участник чата";
        }

        var welcomeMessage = $"Привет, <a href=\"tg://user?id={user.Id}\">{System.Net.WebUtility.HtmlEncode(fullNameForDisplay)}</a>! " +
                            $"Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?";

        // Добавляем заглушку для рекламы если нужно
        var isNoAdGroup = IsNoAdGroup(chat.Id);
        var vpnAdHtml = isNoAdGroup ? "" : "\n\n 📍 Место для рекламы\n<i>...</i>";
        welcomeMessage += vpnAdHtml;

        Message captchaMessage;
        try
        {
            captchaMessage = await _messageService.SendCaptchaMessageAsync(
                chat,
                welcomeMessage,
                replyParams,
                new InlineKeyboardMarkup(keyboard),
                cancellationToken: default
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке капчи для пользователя {UserId} в чате {ChatId}", user.Id, chat.Id);
            throw;
        }

        var cts = new CancellationTokenSource();
        var captchaInfo = new CaptchaInfo(chat.Id, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, userJoinMessage);
        
        var key = GenerateKey(chat.Id, user.Id);
        _captchaNeededUsers.TryAdd(key, captchaInfo);

        // Автоматическое удаление капчи и бан через 1.2 минуты
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1.2), cts.Token);
                
                // Удаляем капчу из коллекции
                if (_captchaNeededUsers.TryRemove(key, out var expiredCaptcha))
                {
                    _logger.LogInformation("Пользователь {User} (id={UserId}) не прошёл капчу (таймаут) в группе '{ChatTitle}' (id={ChatId})", 
                        Utils.FullName(expiredCaptcha.User), expiredCaptcha.User.Id, expiredCaptcha.ChatTitle ?? "-", expiredCaptcha.ChatId);
                    
                    try
                    {
                        // Баним пользователя на 20 минут
                        await _bot.BanChatMemberAsync(expiredCaptcha.ChatId, expiredCaptcha.User.Id, 
                            untilDate: DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: true);
                        
                        // Удаляем сообщения
                        await _bot.DeleteMessageAsync(chat.Id, captchaMessage.MessageId);
                        if (userJoinMessage != null)
                            await _bot.DeleteMessageAsync(chat.Id, userJoinMessage.MessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при бане пользователя {UserId} за просроченную капчу", expiredCaptcha.User.Id);
                    }
                    
                    // Разбан через 20 минут
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(20));
                            await _bot.UnbanChatMemberAsync(expiredCaptcha.ChatId, expiredCaptcha.User.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", expiredCaptcha.User.Id);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при автоудалении капчи");
            }
        });

        return captchaInfo;
    }

    /// <summary>
    /// Проверяет ответ пользователя на капчу.
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <param name="answer">Ответ пользователя</param>
    /// <returns>true, если ответ правильный</returns>
    public async Task<bool> ValidateCaptchaAsync(string key, int answer)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (!_captchaNeededUsers.TryRemove(key, out var info))
        {
            _logger.LogWarning("Капча {Key} не найдена", key);
            return false;
        }

        try
        {
            await info.Cts.CancelAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при отмене токена капчи {Key}", key);
        }

        return info.CorrectAnswer == answer;
    }

    /// <summary>
    /// Получает информацию о капче по ключу.
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <returns>Информация о капче или null, если не найдена</returns>
    public CaptchaInfo? GetCaptchaInfo(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return _captchaNeededUsers.TryGetValue(key, out var info) ? info : null;
    }

    /// <summary>
    /// Удаляет капчу по ключу.
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <returns>true, если капча была удалена</returns>
    public bool RemoveCaptcha(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return _captchaNeededUsers.TryRemove(key, out _);
    }

    /// <summary>
    /// Генерирует ключ капчи для пользователя в чате.
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Уникальный ключ капчи</returns>
    public string GenerateKey(long chatId, long userId)
    {
        return $"{chatId}_{userId}";
    }

    /// <summary>
    /// Банит пользователей с просроченными капчами.
    /// </summary>
    public async Task BanExpiredCaptchaUsersAsync()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;

        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        
        foreach (var (key, captchaInfo) in users)
        {
            var minutes = (now - captchaInfo.Timestamp).TotalMinutes;
            if (minutes > 1.3)
            {
                _logger.LogInformation("Пользователь {User} (id={UserId}) не прошёл капчу (таймаут) в группе '{ChatTitle}' (id={ChatId})", 
                    Utils.FullName(captchaInfo.User), captchaInfo.User.Id, captchaInfo.ChatTitle ?? "-", captchaInfo.ChatId);

                _captchaNeededUsers.TryRemove(key, out _);
                
                try
                {
                    await _bot.BanChatMemberAsync(captchaInfo.ChatId, captchaInfo.User.Id, 
                        untilDate: now + TimeSpan.FromMinutes(20), revokeMessages: true);
                    
                    if (captchaInfo.UserJoinedMessage != null)
                        await _bot.DeleteMessageAsync(captchaInfo.ChatId, captchaInfo.UserJoinedMessage.MessageId);

                    // Разбан через некоторое время
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(20));
                            await _bot.UnbanChatMemberAsync(captchaInfo.ChatId, captchaInfo.User.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", captchaInfo.User.Id);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при бане пользователя за просроченную капчу");
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, является ли группа группой без рекламы VPN.
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>true, если группа без рекламы VPN</returns>
    private static bool IsNoAdGroup(long chatId)
    {
        return Config.NoVpnAdGroups.Contains(chatId);
    }
} 