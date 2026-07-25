namespace ClubDoorman.Test.Architecture;

/// <summary>
/// Exact current direct Telegram.* type refs per pipeline step FullName.
/// Nested async state-machine refs are included in the step's set by the inspector.
/// Update deliberately on both expansion and shrink.
/// </summary>
internal static class PipelineStepTelegramBaseline
{
    public static readonly IReadOnlyDictionary<string, string[]> Allowed =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.AiProfileAnalysisStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.AlreadyApprovedStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.BanlistCheckStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.BaseModerationStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.CaptchaPendingStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.ChatId",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.ChannelMessageStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.ClubMemberSkipStep"] =
            [
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.CommandStep"] =
            [
                "Telegram.Bot.Types.Message"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.FinalModerationActionStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.FirstMessageLogStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.LeftMemberCleanupStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.ChatId",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.NewMembersStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.PrivateSkipStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Enums.ChatType",
                "Telegram.Bot.Types.Message"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.SystemOrBotMessageStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ]
        };
}
