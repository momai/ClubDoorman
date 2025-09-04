using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для работы с лог-чатом
/// </summary>
public class LogChatService : ILogChatService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IUserBanService _userBanService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<LogChatService> _logger;

    public LogChatService(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IUserBanService userBanService,
        IAppConfig appConfig,
        ILogger<LogChatService> logger)
    {
        _bot = bot;
        _messageService = messageService;
        _userBanService = userBanService;
        _appConfig = appConfig;
        _logger = logger;
    }

    public async Task SendLogNotificationAsync(Message message, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Начинаем отправку уведомления в лог-чат для сообщения {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);

        var user = message.From;

        try
        {
            // Создаем кнопки реакции для лог-чата (без добавления в автобан)
            var callbackDataBan = $"logban_{message.Chat.Id}_{user.Id}";
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

            // Определяем заголовок в зависимости от причины
            var actionDescription = reason switch
            {
                var r when r.Contains("Ссылки запрещены") => "Удаление за ссылки",
                var r when r.Contains("Банальное приветствие") => "Удаление банального приветствия",
                _ => $"Удаление: {reason}"
            };

            var deletionData = new AutoBanNotificationData(
                user,
                message.Chat,
                actionDescription,
                reason,
                message.MessageId,
                LinkToMessage(message.Chat, message.MessageId)
            );

            // ФИКС: Пытаемся переслать сообщение, но если не получается - отправляем без пересылки
            Message? forwardedMessage = null;
            try
            {
                // Пересылаем сообщение и отправляем уведомление с кнопками как реплай
                forwardedMessage = await _bot.ForwardMessage(
                    new ChatId(_appConfig.LogAdminChatId),
                    message.Chat.Id,
                    message.MessageId,
                    cancellationToken: cancellationToken
                );

                // Используем правильный шаблон в зависимости от причины
                string messageText;
                if (reason.Contains("Ссылки запрещены"))
                {
                    var template = _messageService.GetTemplates().GetLogTemplate(LogNotificationType.AutoBanTextMention);
                    messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, deletionData);
                }
                else
                {
                    // Для кастомных шаблонов создаем объект с нужными свойствами
                    var templateData = new
                    {
                        UserFullName = $"{deletionData.User.FirstName} {deletionData.User.LastName}".Trim(),
                        ChatTitle = deletionData.Chat.Title ?? deletionData.Chat.Username ?? "Неизвестный чат",
                        MessageLink = deletionData.MessageLink
                    };

                    var template = reason switch
                    {
                        var r when r.Contains("Банальное приветствие") => "🚫 Удаление банального приветствия\nЮзер {UserFullName} из чата {ChatTitle}\n{MessageLink}",
                        _ => $"🚫 {actionDescription}\nЮзер {{UserFullName}} из чата {{ChatTitle}}\n{{MessageLink}}"
                    };

                    messageText = _messageService.GetTemplates().FormatTemplate(template, templateData);
                }

                await _bot.SendMessage(
                    _appConfig.LogAdminChatId,
                    messageText + "\n\n" + "Действия:",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    replyParameters: forwardedMessage,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception forwardEx) when (forwardEx.Message.Contains("protected content") || forwardEx.Message.Contains("can't be forwarded"))
            {
                _logger.LogWarning("Сообщение имеет защищенный контент, отправляем уведомление без пересылки: {Error}", forwardEx.Message);

                // Получаем содержимое сообщения для отображения
                var messageContent = message.Text ?? message.Caption ?? "Писать в лс";

                // Используем правильный шаблон в зависимости от причины
                var template = reason switch
                {
                    var r when r.Contains("Ссылки запрещены") => "🚫 Удаление сообщения за ссылки\nЮзер {UserFullName} из чата {ChatTitle}\n{MessageLink}\n\nСодержимое:\n{MessageContent}",
                    var r when r.Contains("Банальное приветствие") => "🚫 Удаление банального приветствия\nЮзер {UserFullName} из чата {ChatTitle}\n{MessageLink}\n\nСодержимое:\n{MessageContent}",
                    _ => $"🚫 {actionDescription}\nЮзер {{UserFullName}} из чата {{ChatTitle}}\n{{MessageLink}}\n\nСодержимое:\n{{MessageContent}}"
                };

                // Создаем данные с содержимым сообщения
                var deletionDataWithContent = new
                {
                    UserFullName = $"{deletionData.User.FirstName} {deletionData.User.LastName}".Trim(),
                    ChatTitle = deletionData.Chat.Title ?? deletionData.Chat.Username ?? "Неизвестный чат",
                    MessageLink = deletionData.MessageLink,
                    MessageContent = messageContent
                };

                var messageText = _messageService.GetTemplates().FormatTemplate(template, deletionDataWithContent);

                // Отправляем уведомление без пересылки (просто как обычное сообщение)
                await _bot.SendMessage(
                    _appConfig.LogAdminChatId,
                    messageText + "\n\n" + "Действия:",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }

            _logger.LogDebug("Уведомление с кнопками успешно отправлено в лог-чат");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить уведомление в лог-чат");
        }
    }

    public async Task HandleLogBanAsync(long chatId, long userId, string adminName, CancellationToken cancellationToken = default)
    {
        var callbackDataBan = $"logban_{chatId}_{userId}";
        var userMessage = MemoryCache.Default.Remove(callbackDataBan) as Message;

        // В лог-чате НЕ добавляем сообщение в автобан - это просто бан пользователя
        _logger.LogInformation("🚫📝 Бан из лог-чата - сообщение НЕ добавляется в автобан для пользователя {UserId}", userId);

        try
        {
            // Создаем объекты для UserBanService
            var user = new User { Id = userId };
            var chat = new Chat { Id = chatId };

            // Используем UserBanService для централизованного бана
            // Передаем null для messageToDelete, так как сообщение уже было удалено ранее
            await _userBanService.BanUserAsync(chat, user, BanTypeEnum.ManualBan, "Ручной бан из лог-чата", null, cancellationToken);

            _logger.LogInformation("Пользователь {UserId} забанен из лог-чата администратором {AdminName}", userId, adminName);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя через лог-чат callback");
            throw;
        }
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";
}