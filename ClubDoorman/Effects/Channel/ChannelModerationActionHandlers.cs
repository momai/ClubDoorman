using ClubDoorman.Models;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;

namespace ClubDoorman.Effects.Channel;

public sealed class ChannelAllowActionHandler : IChannelModerationActionHandler
{
    private readonly ILogger<ChannelAllowActionHandler> _logger;

    public ChannelAllowActionHandler(ILogger<ChannelAllowActionHandler> logger) => _logger = logger;

    public ModerationAction Action => ModerationAction.Allow;

    public Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Содержимое сообщения от channel identity {SenderChatId} разрешено: {Reason}",
            context.SenderChat.Id,
            result.Reason);
        return Task.CompletedTask;
    }
}

public sealed class ChannelDeleteActionHandler : IChannelModerationActionHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<ChannelDeleteActionHandler> _logger;

    public ChannelDeleteActionHandler(
        ITelegramBotClientWrapper bot,
        ILogger<ChannelDeleteActionHandler> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Delete;

    public async Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var message = context.Content.Message;
        _logger.LogInformation(
            "Удаляем сообщение от channel identity {SenderChatId}: {Reason}",
            context.SenderChat.Id,
            result.Reason);
        await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
    }
}

public sealed class ChannelBanActionHandler : IChannelModerationActionHandler
{
    private readonly IUserBanService _userBanService;
    private readonly IChannelModerationReporter _reporter;
    private readonly ILogger<ChannelBanActionHandler> _logger;

    public ChannelBanActionHandler(
        IUserBanService userBanService,
        IChannelModerationReporter reporter,
        ILogger<ChannelBanActionHandler> logger)
    {
        _userBanService = userBanService;
        _reporter = reporter;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.Ban;

    public async Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Баним channel identity {SenderChatId}: {Reason}",
            context.SenderChat.Id,
            result.Reason);

        try
        {
            await _reporter.ReportAsync(context, result, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Не удалось сохранить evidence перед баном channel identity {SenderChatId}",
                context.SenderChat.Id);
        }

        await _userBanService.AutoBanChannelAsync(context.Content.Message, cancellationToken);
    }
}

public sealed class ChannelReportActionHandler : IChannelModerationActionHandler
{
    private readonly IChannelModerationReporter _reporter;

    public ChannelReportActionHandler(IChannelModerationReporter reporter) => _reporter = reporter;

    public ModerationAction Action => ModerationAction.Report;

    public Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken) =>
        _reporter.ReportAsync(context, result, cancellationToken);
}

public sealed class ChannelManualReviewActionHandler : IChannelModerationActionHandler
{
    private readonly IChannelModerationReporter _reporter;

    public ChannelManualReviewActionHandler(IChannelModerationReporter reporter) => _reporter = reporter;

    public ModerationAction Action => ModerationAction.RequireManualReview;

    public Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken) =>
        _reporter.ReportAsync(context, result, cancellationToken);
}

public sealed class ChannelAiAnalysisActionHandler : IChannelModerationActionHandler
{
    private readonly IChannelModerationReporter _reporter;

    public ChannelAiAnalysisActionHandler(IChannelModerationReporter reporter) => _reporter = reporter;

    public ModerationAction Action => ModerationAction.RequireAiAnalysis;

    public Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken) =>
        _reporter.ReportAsync(context, result, cancellationToken);
}
