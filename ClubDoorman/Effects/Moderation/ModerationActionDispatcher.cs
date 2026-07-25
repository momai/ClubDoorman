using ClubDoorman.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Moderation;

public sealed record ModerationActionContext(
    Message Message,
    User User,
    Chat Chat,
    ModerationResult Result,
    bool IsSilentMode);

public interface IModerationActionHandler
{
    ModerationAction Action { get; }

    Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken);
}

public interface IModerationActionDispatcher
{
    Task DispatchAsync(ModerationActionContext context, CancellationToken cancellationToken);
}

public sealed class ModerationActionDispatcher : IModerationActionDispatcher
{
    private readonly IReadOnlyDictionary<ModerationAction, IModerationActionHandler> _handlers;
    private readonly ILogger<ModerationActionDispatcher> _logger;

    public ModerationActionDispatcher(
        IEnumerable<IModerationActionHandler> handlers,
        ILogger<ModerationActionDispatcher> logger)
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
                $"Multiple moderation action handlers registered for: {string.Join(", ", duplicateActions)}");
        }

        _handlers = handlerList.ToDictionary(handler => handler.Action);

        var missingActions = Enum.GetValues<ModerationAction>()
            .Where(action => !_handlers.ContainsKey(action))
            .ToArray();

        if (missingActions.Length > 0)
        {
            throw new InvalidOperationException(
                $"No moderation action handler registered for: {string.Join(", ", missingActions)}");
        }
    }

    public Task DispatchAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(context.Result.Action, out var handler))
        {
            throw new InvalidOperationException(
                $"No moderation action handler registered for: {context.Result.Action}");
        }

        _logger.LogDebug(
            "Dispatching moderation action {Action} to {Handler}",
            context.Result.Action,
            handler.GetType().Name);

        return handler.ExecuteAsync(context, cancellationToken);
    }
}
