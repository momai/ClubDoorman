using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

public sealed class AdminCallbackDispatcher : IAdminCallbackDispatcher
{
    private readonly IReadOnlyDictionary<string, IAdminCallbackHandler> _handlers;
    private readonly ILogger<AdminCallbackDispatcher> _logger;

    public AdminCallbackDispatcher(
        IEnumerable<IAdminCallbackHandler> handlers,
        ILogger<AdminCallbackDispatcher> logger)
    {
        _logger = logger;

        var handlerList = handlers.ToList();
        var duplicatePrefixes = handlerList
            .GroupBy(handler => handler.Prefix, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatePrefixes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Multiple admin callback handlers registered for: {string.Join(", ", duplicatePrefixes)}");
        }

        _handlers = handlerList.ToDictionary(handler => handler.Prefix, StringComparer.Ordinal);
    }

    public Task<AdminCallbackResult> DispatchAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogWarning("Admin callback has empty data");
            return Task.FromResult(AdminCallbackResult.Invalid());
        }

        var separatorIndex = data.IndexOf('_');
        var prefix = separatorIndex < 0 ? data : data[..separatorIndex];
        var payload = separatorIndex < 0 ? string.Empty : data[(separatorIndex + 1)..];

        if (string.IsNullOrEmpty(prefix))
        {
            _logger.LogWarning("Admin callback has invalid data: {Data}", data);
            return Task.FromResult(AdminCallbackResult.Invalid());
        }

        if (!_handlers.TryGetValue(prefix, out var handler))
        {
            _logger.LogWarning("No admin callback handler registered for prefix {Prefix}", prefix);
            return Task.FromResult(AdminCallbackResult.NotHandled());
        }

        _logger.LogDebug(
            "Dispatching admin callback {Prefix} to {Handler}",
            prefix,
            handler.GetType().Name);

        return handler.HandleAsync(
            new AdminCallbackContext(callbackQuery, payload),
            cancellationToken);
    }
}
