using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

public sealed class BanProfileCallbackHandler : IAdminCallbackHandler
{
    private readonly IAdminActionStore _adminActionStore;
    private readonly IUserBanService _userBanService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<BanProfileCallbackHandler> _logger;

    public BanProfileCallbackHandler(
        IAdminActionStore adminActionStore,
        IUserBanService userBanService,
        ITelegramBotClientWrapper bot,
        IAppConfig appConfig,
        ILogger<BanProfileCallbackHandler> logger)
    {
        _adminActionStore = adminActionStore;
        _userBanService = userBanService;
        _bot = bot;
        _appConfig = appConfig;
        _logger = logger;
    }

    public string Prefix => "banprofile";

    public async Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.Payload))
            return AdminCallbackResult.Invalid();

        var reviewState = _adminActionStore.TakeProfileReview(context.Payload);
        if (reviewState == null)
            return AdminCallbackResult.Handled("Действие устарело или уже выполнено", true);

        var message = context.Message;
        var userId = reviewState.UserId;
        _logger.LogInformation(
            "Бан по профилю: сообщение не добавляется в автобан для пользователя {UserId}",
            userId);

        try
        {
            await _userBanService.BanUserAsync(
                new Chat { Id = reviewState.ChatId },
                new User { Id = userId },
                BanTypeEnum.ProfileBan,
                "Бан по профилю",
                reviewState.MessageId,
                reviewState.ChatId,
                cancellationToken);

            if (reviewState.MessageId != null)
            {
                try
                {
                    await _bot.ForwardMessage(
                        _appConfig.AdminChatId,
                        reviewState.ChatId,
                        (int)reviewState.MessageId.Value,
                        cancellationToken);
                    _logger.LogDebug(
                        "При ручном бане переслано сообщение пользователя {UserId}",
                        userId);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Не удалось переслать сообщение пользователя {UserId} при ручном бане - вероятно, уже удалено",
                        userId);
                }
            }
            else
            {
                _logger.LogDebug(
                    "При ручном бане сообщение пользователя {UserId} не пересылается - MessageId отсутствует",
                    userId);
            }

            var updatedText = $"{message.Text}\n\n🚫 Забанен за спам-профиль администратором {context.AdminDisplayName}\n🧹 Пользователь очищен из всех списков\n⚠️ Сообщение НЕ добавлено в автобан (проблема в профиле)";
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                updatedText,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Пользователь {UserId} забанен за спам-профиль администратором {AdminName}",
                userId,
                context.AdminDisplayName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось забанить пользователя через админский callback (бан по профилю)");
            var errorText = $"{message.Text}\n\n❌ Ошибка при бане администратором {context.AdminDisplayName}\nНе могу забанить. Не хватает могущества? Сходите забаньте руками";
            await _bot.EditMessageText(
                message.Chat.Id,
                message.MessageId,
                errorText,
                cancellationToken: cancellationToken);
        }

        return AdminCallbackResult.Handled();
    }
}
