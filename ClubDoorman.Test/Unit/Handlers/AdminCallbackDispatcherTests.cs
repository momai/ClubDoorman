using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("fast")]
public class AdminCallbackDispatcherTests
{
    [Test]
    public void DuplicatePrefix_FailsWhenDispatcherIsResolvedFromDi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAdminCallbackHandler>(new StubHandler("same"));
        services.AddSingleton<IAdminCallbackHandler>(new StubHandler("same"));
        services.AddSingleton<IAdminCallbackDispatcher, AdminCallbackDispatcher>();
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IAdminCallbackDispatcher>());

        Assert.That(exception!.Message, Does.Contain("same"));
    }

    [Test]
    public async Task KnownPrefixWithMalformedPayload_ReturnsInvalidWithoutExecutingAction()
    {
        var userManager = new Mock<IUserManager>();
        var handler = new ApproveUserCallbackHandler(
            userManager.Object,
            Mock.Of<ITelegramBotClientWrapper>(),
            new NullLogger<ApproveUserCallbackHandler>());
        var dispatcher = CreateDispatcher(handler);

        var result = await dispatcher.DispatchAsync(CreateCallback("approve_not-a-user"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(AdminCallbackStatus.Invalid));
        userManager.VerifyNoOtherCalls();
    }

    [Test]
    public async Task UnknownPrefix_ReturnsNotHandledAndLogsWarning()
    {
        var logger = new Mock<ILogger<AdminCallbackDispatcher>>();
        var dispatcher = new AdminCallbackDispatcher([], logger.Object);

        var result = await dispatcher.DispatchAsync(CreateCallback("unknown_42"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(AdminCallbackStatus.NotHandled));
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains("unknown")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void AdminOpsFeature_ResolvesRegisteredCallbackGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        services.AddSingleton(Mock.Of<IUserManager>());
        services.AddSingleton(Mock.Of<IBadMessageManager>());
        services.AddSingleton(Mock.Of<IUserBanService>());
        services.AddSingleton(Mock.Of<ILogChatService>());
        services.AddSingleton(Mock.Of<IAdminActionStore>());
        services.AddSingleton(Mock.Of<IAppConfig>());
        services.AddSingleton(Mock.Of<IAiChecks>());
        services.AddSingleton(Mock.Of<IMessageService>());
        services.AddSingleton(Mock.Of<IModerationService>());
        services.AddAdminOpsFeature();
        using var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IAdminCallbackDispatcher>();
        var prefixes = provider.GetServices<IAdminCallbackHandler>()
            .Select(handler => handler.Prefix)
            .ToArray();

        Assert.That(dispatcher, Is.InstanceOf<AdminCallbackDispatcher>());
        Assert.That(prefixes, Is.EquivalentTo(new[]
        {
            "approve", "ban", "logban", "banprofile", "aiOk", "suspicious", "noop"
        }));
    }

    private static AdminCallbackDispatcher CreateDispatcher(params IAdminCallbackHandler[] handlers)
    {
        return new AdminCallbackDispatcher(handlers, new NullLogger<AdminCallbackDispatcher>());
    }

    private static CallbackQuery CreateCallback(string data)
    {
        return new CallbackQuery
        {
            Id = "callback-id",
            Data = data,
            From = new User { Id = 1, FirstName = "Admin" },
            Message = new Message { Chat = new Chat { Id = -1000 } }
        };
    }

    private sealed class StubHandler : IAdminCallbackHandler
    {
        public StubHandler(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }

        public Task<AdminCallbackResult> HandleAsync(
            AdminCallbackContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCallbackResult.Handled());
        }
    }
}
