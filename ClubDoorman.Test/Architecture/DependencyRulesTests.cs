using ClubDoorman.Features.Moderation;
using ClubDoorman.Effects.Moderation;
using ClubDoorman.Models;
using ClubDoorman.Services.Handlers.Pipeline;
using NetArchTest.Rules;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Architecture;

[TestFixture]
public class DependencyRulesTests
{
    [Test]
    public void IMessageStep_ConcreteImplementations_ResideInPipelineStepsNamespace()
    {
        var steps = PipelineStepDependencyInspector.GetConcreteMessageSteps();
        Assert.That(steps, Is.Not.Empty, "Expected concrete IMessageStep implementations");

        var outside = steps
            .Where(t => !PipelineStepDependencyInspector.IsInStepsNamespace(t.Namespace))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(outside, Is.Empty,
            "Concrete IMessageStep implementations must reside in "
            + $"{PipelineStepDependencyInspector.StepsNamespace} or a child namespace. Outside: "
            + string.Join(", ", outside));
    }

    [Test]
    public void PipelineSteps_DoNotTakeStatefulConcreteInfrastructureInConstructors()
    {
        // Boundary: steps must not DI-inject concrete storage/cache/config/infra services.
        // Interfaces, abstracts, value types, and ordinary DTOs/value objects remain allowed.
        var violations = PipelineStepDependencyInspector.GetConcreteMessageSteps()
            .SelectMany(PipelineStepDependencyInspector.GetForbiddenConstructorParameters)
            .ToList();

        Assert.That(violations, Is.Empty,
            "Pipeline steps must not take stateful concrete infrastructure in constructors. Violations:\n"
            + string.Join("\n", violations));
    }

    [Test]
    public void PipelineSteps_DirectTelegramTypeRefs_StayWithinBaseline()
    {
        // NetArchTest HaveDependencyOn("Telegram.Bot") is transitive and true for every step
        // that touches MessageContext. We freeze the *exact current* set of direct Telegram.*
        // type refs per step FullName (including nested async state machines) so both expansion
        // and shrink require an intentional baseline update.
        var actual = PipelineStepDependencyInspector.GetDirectTelegramTypeRefs();
        var baseline = PipelineStepDependencyInspector.AllowedTelegramTypeBaseline;
        var failures = new List<string>();

        foreach (var (fullName, refs) in actual)
        {
            var allowed = baseline.TryGetValue(fullName, out var listed)
                ? listed
                : Array.Empty<string>();
            var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);

            var outsideTypes = refs
                .Where(r => !r.StartsWith("Telegram.Bot.Types", StringComparison.Ordinal))
                .ToList();
            if (outsideTypes.Count > 0)
            {
                failures.Add(
                    $"{fullName}: Telegram client/SDK types are forbidden (use ITelegramBotClientWrapper). "
                    + string.Join(", ", outsideTypes));
            }

            var extras = refs.Where(r => !allowedSet.Contains(r)).ToList();
            if (extras.Count > 0)
            {
                failures.Add(
                    $"{fullName}: new Telegram type refs beyond baseline: {string.Join(", ", extras)}. "
                    + "Update AllowedTelegramTypeBaseline deliberately if this is intended.");
            }

            var stale = allowedSet.Where(a => !refs.Contains(a)).ToList();
            if (stale.Count > 0)
            {
                failures.Add(
                    $"{fullName}: baseline still allows unused Telegram type refs: {string.Join(", ", stale)}. "
                    + "Shrink AllowedTelegramTypeBaseline deliberately.");
            }
        }

        // Baseline entries must still exist as step types (typos / renames break the gate).
        foreach (var key in baseline.Keys)
        {
            if (!actual.ContainsKey(key))
                failures.Add($"Baseline lists unknown step FullName: {key}");
        }

        Assert.That(failures, Is.Empty,
            "Pipeline steps Telegram surface must match the frozen current baseline.\n"
            + string.Join("\n", failures));
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

    [Test]
    public void ModerationEffects_DoNotUseServiceProvider()
    {
        var violations = typeof(IModerationActionHandler).Assembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(IModerationActionHandler).Namespace)
            .SelectMany(type => type.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters()
                .Where(parameter => typeof(IServiceProvider).IsAssignableFrom(parameter.ParameterType))
                .Select(parameter => $"{constructor.DeclaringType!.FullName}.{constructor.Name}({parameter.Name})"))
            .ToList();

        Assert.That(violations, Is.Empty,
            "Moderation effects must declare typed dependencies instead of resolving them at runtime. Violations:\n"
            + string.Join("\n", violations));
    }

    [Test]
    public void ModerationActionHandlers_DoNotStoreRuntimeContext()
    {
        var runtimeTypes = new HashSet<Type>
        {
            typeof(Message),
            typeof(User),
            typeof(Chat),
            typeof(ModerationResult),
            typeof(ModerationActionContext)
        };
        var violations = typeof(IModerationActionHandler).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IModerationActionHandler).IsAssignableFrom(type))
            .SelectMany(type => type.GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic))
            .Where(field => runtimeTypes.Contains(field.FieldType))
            .Select(field => $"{field.DeclaringType!.FullName}.{field.Name}: {field.FieldType.Name}")
            .ToList();

        Assert.That(violations, Is.Empty,
            "Singleton moderation action handlers must receive per-message state through ModerationActionContext. Violations:\n"
            + string.Join("\n", violations));
    }

    private static void AssertRulePasses(TestResult result, string rule)
    {
        var failures = string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
        Assert.That(result.IsSuccessful, Is.True, $"{rule}. Violations: {failures}");
    }
}
