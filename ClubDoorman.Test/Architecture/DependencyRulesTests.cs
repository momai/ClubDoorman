using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Handlers.Pipeline;
using ClubDoorman.Services.Handlers.Pipeline.Steps;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.UserManagement;
using NetArchTest.Rules;
using NUnit.Framework;

namespace ClubDoorman.Test.Architecture;

[TestFixture]
public class DependencyRulesTests
{
    private static readonly Type[] ConcreteStorageOrConfigurationTypes =
    [
        typeof(AppConfig),
        typeof(ConfigurationHelper),
        typeof(AiOptions),
        typeof(AutoBanOptions),
        typeof(ChatAccessOptions),
        typeof(ChatFilteringOptions),
        typeof(CoreOptions),
        typeof(FeatureToggleOptions),
        typeof(TestHarnessOptions),
        typeof(ViolationThresholdOptions),
        typeof(ApprovedUsersStorage),
        typeof(UserIndex),
        typeof(JoinedUserFlags),
        typeof(SuspiciousUsersStorage)
    ];

    // Existing steps access Telegram message data through MessageContext and are exempt as whole types.
    private static readonly Type[] LegacyTelegramStepTypes =
    [
        typeof(AiProfileAnalysisStep),
        typeof(AlreadyApprovedStep),
        typeof(BanlistCheckStep),
        typeof(BaseModerationStep),
        typeof(CaptchaPendingStep),
        typeof(ChannelMessageStep),
        typeof(ClubMemberSkipStep),
        typeof(CommandStep),
        typeof(FinalModerationActionStep),
        typeof(FirstMessageLogStep),
        typeof(NewMembersStep),
        typeof(PrivateSkipStep),
        typeof(LeftMemberCleanupStep),
        typeof(SystemOrBotMessageStep)
    ];

    [Test]
    public void PipelineSteps_DoNotDependOnConcreteStorageOrConfiguration()
    {
        var result = Types.InAssembly(typeof(IMessageStep).Assembly)
            .That()
            .ImplementInterface(typeof(IMessageStep))
            .ShouldNot()
            .HaveDependencyOnAny(FullNamesOf(ConcreteStorageOrConfigurationTypes))
            .GetResult();

        AssertRulePasses(result, "Pipeline steps must depend on storage and configuration abstractions only");
    }

    [Test]
    public void PipelineSteps_ExceptDocumentedLegacyTypes_DoNotDependOnTelegramSdk()
    {
        var result = Types.InAssembly(typeof(IMessageStep).Assembly)
            .That()
            .ImplementInterface(typeof(IMessageStep))
            .And()
            .DoNotHaveName(LegacyTelegramStepTypes.Select(type => type.Name).ToArray())
            .ShouldNot()
            .HaveDependencyOn("Telegram.Bot")
            .GetResult();

        AssertRulePasses(result, "Pipeline steps must not depend on the Telegram SDK");
    }

    [Test]
    public void ModerationFeature_DoesNotDependOnHandlers()
    {
        var result = Types.InAssembly(typeof(ModerationFeature).Assembly)
            .That()
            .ResideInNamespace("ClubDoorman.Features.Moderation")
            .ShouldNot()
            .HaveDependencyOn("ClubDoorman.Services.Handlers")
            .GetResult();

        AssertRulePasses(result, "The moderation feature must not depend on handlers");
    }

    private static string[] FullNamesOf(IEnumerable<Type> types) =>
        types.Select(type => type.FullName!).ToArray();

    private static void AssertRulePasses(TestResult result, string rule)
    {
        var failures = string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
        Assert.That(result.IsSuccessful, Is.True, $"{rule}. Violations: {failures}");
    }
}
