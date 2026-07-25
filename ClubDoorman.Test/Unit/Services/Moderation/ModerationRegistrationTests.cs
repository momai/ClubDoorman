using System.Linq;
using ClubDoorman.Effects.Moderation;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
}
