using ClubDoorman.Models;
using ClubDoorman.Services.AI;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Effects.Moderation;

public sealed class AiAnalysisActionHandler : IModerationActionHandler
{
    private readonly IAiCascadeService _aiCascadeService;
    private readonly ILogger<AiAnalysisActionHandler> _logger;

    public AiAnalysisActionHandler(
        IAiCascadeService aiCascadeService,
        ILogger<AiAnalysisActionHandler> logger)
    {
        _aiCascadeService = aiCascadeService;
        _logger = logger;
    }

    public ModerationAction Action => ModerationAction.RequireAiAnalysis;

    public Task ExecuteAsync(ModerationActionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ML не уверен, запускаем AI анализ: {Reason}", "RequireAiAnalysis");
        return _aiCascadeService.HandleAiCascadeAnalysisAsync(
            context.Message,
            context.User,
            context.Result.Confidence ?? 0,
            context.IsSilentMode,
            cancellationToken);
    }
}
