using Telegram.Bot.Types;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models; // for ModerationResult

namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Упрощённый контекст, будет расширяться на миграционных шагах.
/// </summary>
public class MessageContext
{
    public required Update Update { get; init; }
    public required Message Message { get; init; }
    public Guid OperationId { get; init; } = Guid.NewGuid();
    public string? GmCorrelation { get; init; }
    public bool CommandHandled { get; set; }
    public bool NewMembersHandled { get; set; } // set by NewMembersStep when it stops pipeline
    public bool LeftMemberCleanupHandled { get; set; }
    public bool ChannelMessageHandled { get; set; }
    public bool PrivateSkipHandled { get; set; }
    // Moderation pre-chain steps (100..140)
    public bool CaptchaPendingHandled { get; set; }
    public bool BanlistHandled { get; set; }
    public bool AlreadyApprovedHandled { get; set; }
    public bool ClubMemberSkipHandled { get; set; }
    public bool UserResultHandled { get; set; } // generic early exit flag if UserResult populated
    public object? UserResult { get; set; }
    public bool EventPublished { get; set; }
    // Moderation chain (200+ planned extractions)
    public User? User => Message.From;
    public Chat Chat => Message.Chat;
    public bool IsSilentMode { get; set; }
    public ModerationResult? ModerationResult { get; set; }
    public bool AiProfileRestricted { get; set; }
    // Место для будущих производных данных (normalized text, flags, decision...).
}
