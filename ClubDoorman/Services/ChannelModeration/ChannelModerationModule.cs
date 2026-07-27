using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Effects.Channel;

namespace ClubDoorman.Services.ChannelModeration;

public static class ChannelModerationModule
{
    public static IServiceCollection AddChannelModerationServices(this IServiceCollection services)
    {
        services.AddSingleton<IChannelModerationReporter, ChannelModerationReporter>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelAllowActionHandler>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelDeleteActionHandler>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelBanActionHandler>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelReportActionHandler>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelManualReviewActionHandler>();
        services.AddSingleton<IChannelModerationActionHandler, ChannelAiAnalysisActionHandler>();
        services.AddSingleton<IChannelModerationActionDispatcher, ChannelModerationActionDispatcher>();
        services.AddSingleton<IChannelModerationService, ChannelModerationService>();

        return services;
    }
}
