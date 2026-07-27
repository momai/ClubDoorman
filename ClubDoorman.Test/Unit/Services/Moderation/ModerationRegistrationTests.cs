using System.Linq;
using ClubDoorman.Effects.Moderation;
using ClubDoorman.Effects.Channel;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.ChannelModeration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services.Moderation;

/// <summary>
/// Тесты, гарантирующие что фасад модерации регистрируется ровно один раз (только через Feature).
/// </summary>
public class ModerationRegistrationTests
{
    [Test]
    public void AddClubDoorman_ShouldRegisterSingleModerationFacade()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddClubDoorman();

        // assert
        var descriptors = services.Where(d => d.ServiceType == typeof(IModerationFacade)).ToList();
        Assert.That(descriptors.Count, Is.EqualTo(1),
            "IModerationFacade должен регистрироваться один раз (в Feature). Удалите дубликаты в legacy модулях.");
    }

    [Test]
    public void AddClubDoorman_ShouldRegisterSingleContentModerationPolicy()
    {
        var services = new ServiceCollection();

        services.AddClubDoorman();

        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IContentModerationPolicy))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(descriptors, Has.Count.EqualTo(1));
            Assert.That(descriptors[0].ImplementationType, Is.EqualTo(typeof(ContentModerationPolicy)));
            Assert.That(descriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        });
    }

    [Test]
    public void AddClubDoorman_ShouldRegisterCompleteChannelActionGraph()
    {
        var services = new ServiceCollection();

        services.AddClubDoorman();

        var handlers = services
            .Where(descriptor => descriptor.ServiceType == typeof(IChannelModerationActionHandler))
            .ToList();
        var dispatchers = services
            .Where(descriptor => descriptor.ServiceType == typeof(IChannelModerationActionDispatcher))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(handlers.Select(descriptor => descriptor.ImplementationType), Is.EquivalentTo(new[]
            {
                typeof(ChannelAllowActionHandler),
                typeof(ChannelDeleteActionHandler),
                typeof(ChannelBanActionHandler),
                typeof(ChannelReportActionHandler),
                typeof(ChannelManualReviewActionHandler),
                typeof(ChannelAiAnalysisActionHandler)
            }));
            Assert.That(dispatchers, Has.Count.EqualTo(1));
            Assert.That(dispatchers[0].ImplementationType, Is.EqualTo(typeof(ChannelModerationActionDispatcher)));
            Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IChannelModerationReporter)), Is.EqualTo(1));
        });
    }

    [Test]
    public void AddClubDoorman_ShouldRegisterOneHandlerForEveryModerationAction()
    {
        var services = new ServiceCollection();

        services.AddClubDoorman();

        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IModerationActionHandler))
            .ToList();
        var expectedTypes = new[]
        {
            typeof(AllowActionHandler),
            typeof(DeleteActionHandler),
            typeof(BanActionHandler),
            typeof(ReportActionHandler),
            typeof(ManualReviewActionHandler),
            typeof(AiAnalysisActionHandler)
        };

        Assert.Multiple(() =>
        {
            Assert.That(descriptors.Select(descriptor => descriptor.ImplementationType), Is.EquivalentTo(expectedTypes));
            Assert.That(descriptors.All(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton), Is.True);
        });
    }

    [Test]
    public void AddClubDoorman_ShouldRegisterSingleModerationActionDispatcher()
    {
        var services = new ServiceCollection();

        services.AddClubDoorman();

        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IModerationActionDispatcher))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(descriptors, Has.Count.EqualTo(1));
            Assert.That(descriptors[0].ImplementationType, Is.EqualTo(typeof(ModerationActionDispatcher)));
            Assert.That(descriptors[0].Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        });
    }

    [Test]
    public void AddClubDoorman_ShouldResolveValidatedModerationActionGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddClubDoorman();
        services.AddSingleton(Mock.Of<IModerationPolicy>());
        services.AddSingleton(Mock.Of<INotificationService>());
        services.AddSingleton(Mock.Of<IUserBanService>());
        services.AddSingleton(Mock.Of<IUserFlowLogger>());
        services.AddSingleton(Mock.Of<IAiCascadeService>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IModerationActionDispatcher>(),
                Is.TypeOf<ModerationActionDispatcher>());
            Assert.That(provider.GetRequiredService<IModerationFacade>(),
                Is.TypeOf<ModerationFacade>());
            Assert.That(provider.GetServices<IModerationActionHandler>(), Has.Exactly(6).Items);
        });
    }
}
