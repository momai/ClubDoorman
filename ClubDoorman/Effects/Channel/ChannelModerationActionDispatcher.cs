using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Channel;

public sealed record ChannelModerationContext(
    ContentModerationInput Content,
    Chat SenderChat,
    bool IsSilentMode);

public interface IChannelModerationActionHandler
{
    ModerationAction Action { get; }

    Task ExecuteAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken);
}

public interface IChannelModerationActionDispatcher
{
    Task DispatchAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken);
}

public sealed class ChannelModerationActionDispatcher : IChannelModerationActionDispatcher
{
    private readonly IReadOnlyDictionary<ModerationAction, IChannelModerationActionHandler> _handlers;
    private readonly ILogger<ChannelModerationActionDispatcher> _logger;

    public ChannelModerationActionDispatcher(
        IEnumerable<IChannelModerationActionHandler> handlers,
        ILogger<ChannelModerationActionDispatcher> logger)
    {
        _logger = logger;

        var handlerList = handlers.ToList();
        var duplicateActions = handlerList
            .GroupBy(handler => handler.Action)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateActions.Length > 0)
        {
            throw new InvalidOperationException(
                $"Multiple channel moderation action handlers registered for: {string.Join(", ", duplicateActions)}");
        }

        _handlers = handlerList.ToDictionary(handler => handler.Action);
        var missingActions = Enum.GetValues<ModerationAction>()
            .Where(action => !_handlers.ContainsKey(action))
            .ToArray();

        if (missingActions.Length > 0)
        {
            throw new InvalidOperationException(
                $"No channel moderation action handler registered for: {string.Join(", ", missingActions)}");
        }
    }

    public Task DispatchAsync(
        ChannelModerationContext context,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(result.Action, out var handler))
        {
            throw new InvalidOperationException(
                $"No channel moderation action handler registered for: {result.Action}");
        }

        _logger.LogDebug(
            "Dispatching channel moderation action {Action} to {Handler}",
            result.Action,
            handler.GetType().Name);

        return handler.ExecuteAsync(context, result, cancellationToken);
    }
}
