using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

public enum AdminCallbackStatus
{
    Handled,
    Invalid,
    NotHandled
}

public sealed record AdminCallbackResult(
    AdminCallbackStatus Status,
    string? AnswerText = null,
    bool ShowAlert = false)
{
    public static AdminCallbackResult Handled(string? answerText = null, bool showAlert = false) =>
        new(AdminCallbackStatus.Handled, answerText, showAlert);

    public static AdminCallbackResult Invalid() =>
        new(AdminCallbackStatus.Invalid, "Некорректное действие", true);

    public static AdminCallbackResult NotHandled() =>
        new(AdminCallbackStatus.NotHandled, "Неизвестное действие", true);
}

public sealed record AdminCallbackContext(
    CallbackQuery CallbackQuery,
    string Payload)
{
    public Message Message => CallbackQuery.Message!;

    public string AdminDisplayName => !string.IsNullOrEmpty(CallbackQuery.From.Username)
        ? CallbackQuery.From.Username
        : Utils.FullName(CallbackQuery.From);
}

public interface IAdminCallbackHandler
{
    string Prefix { get; }

    Task<AdminCallbackResult> HandleAsync(
        AdminCallbackContext context,
        CancellationToken cancellationToken);
}

public interface IAdminCallbackDispatcher
{
    Task<AdminCallbackResult> DispatchAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken);
}
