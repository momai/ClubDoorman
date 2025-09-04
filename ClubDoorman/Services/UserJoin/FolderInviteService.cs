using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services.UserJoin;

/// <summary>
/// Сервис для обработки пользователей, входящих через папки
/// <tags>user-join, folder-invite, moderation, ban</tags>
/// </summary>
public class FolderInviteService : IFolderInviteService
{
    private readonly IAppConfig _appConfig;
    private readonly IUserBanService _userBanService;
    private readonly IMessageService _messageService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<FolderInviteService> _logger;

    /// <summary>
    /// Создает экземпляр FolderInviteService
    /// <tags>user-join, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="appConfig">Конфигурация приложения</param>
    /// <param name="userBanService">Сервис бана пользователей</param>
    /// <param name="messageService">Сервис сообщений</param>
    /// <param name="bot">Telegram бот клиент</param>
    /// <param name="logger">Логгер</param>
    public FolderInviteService(
        IAppConfig appConfig,
        IUserBanService userBanService,
        IMessageService messageService,
        ITelegramBotClientWrapper bot,
        ILogger<FolderInviteService> logger)
    {
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Проверяет, вошел ли пользователь через папку и обрабатывает это событие
    /// <tags>user-join, folder-invite, moderation, ban</tags>
    /// </summary>
    /// <param name="chatMemberUpdated">Обновление участника чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если пользователь был забанен за вход через папку</returns>
    public async Task<bool> HandleFolderInviteAsync(ChatMemberUpdated chatMemberUpdated, CancellationToken cancellationToken = default)
    {
        // Проверяем, включена ли функция бана за вход через папки
        if (!_appConfig.BanFolderInviteUsers)
        {
            _logger.LogDebug("Бан за вход через папки отключен в конфигурации");
            return false;
        }

        // Проверяем, что это действительно новый участник
        if (chatMemberUpdated.NewChatMember.Status != ChatMemberStatus.Member ||
            chatMemberUpdated.OldChatMember.Status != ChatMemberStatus.Left)
        {
            return false;
        }

        // Проверяем, вошел ли пользователь через папку
        if (chatMemberUpdated.ViaChatFolderInviteLink != true)
        {
            return false;
        }

        var user = chatMemberUpdated.NewChatMember.User;
        var chat = chatMemberUpdated.Chat;

        _logger.LogWarning("🚫 ПОЛЬЗОВАТЕЛЬ ВОШЕЛ ЧЕРЕЗ ПАПКУ: {User} (id={UserId}, username={Username}) в группу '{ChatTitle}' (id={ChatId})",
            (user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName)),
            user.Id,
            user.Username ?? "-",
            chat.Title ?? "-",
            chat.Id);

        try
        {
            // Проверяем, отключены ли уведомления о банах за вход через папки
            if (_appConfig.BanFolderInviteNotificationsDisable)
            {
                // Если уведомления отключены, баним без уведомлений
                await _bot.BanChatMember(chat, user.Id, cancellationToken: cancellationToken);
                
                _logger.LogInformation("✅ Пользователь {User} (id={UserId}) забанен за вход через папку в группе '{ChatTitle}' (без уведомления)",
                    (user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName)),
                    user.Id,
                    chat.Title ?? "-");
            }
            else
            {
                // Баним пользователя через AutoBanAsync с уведомлениями
                var fakeMessage = new Message
                {
                    From = user,
                    Chat = chat,
                    Date = DateTime.UtcNow
                };

                await _userBanService.AutoBanAsync(
                    fakeMessage,
                    "Автоматический бан за вход через папку",
                    cancellationToken);

                _logger.LogInformation("✅ Пользователь {User} (id={UserId}) забанен за вход через папку в группе '{ChatTitle}'",
                    (user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName)),
                    user.Id,
                    chat.Title ?? "-");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при бане пользователя {User} (id={UserId}) за вход через папку",
                (user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName)),
                user.Id);
            return false;
        }
    }
}
