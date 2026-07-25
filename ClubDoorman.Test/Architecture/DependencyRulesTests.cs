using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.Handlers.Pipeline;
using NetArchTest.Rules;
using NUnit.Framework;

namespace ClubDoorman.Test.Architecture;

[TestFixture]
public class DependencyRulesTests
{
    [Test]
    public void PipelineSteps_InStepsNamespace_ImplementIMessageStep()
    {
        var steps = PipelineStepDependencyInspector.GetPipelineStepTypes();
        Assert.That(steps, Is.Not.Empty, "Expected concrete classes under Pipeline.Steps");

        var missing = steps
            .Where(t => !typeof(IMessageStep).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(missing, Is.Empty,
            "Classes in Pipeline.Steps must implement IMessageStep (including via base class). Missing: "
            + string.Join(", ", missing));
    }

    [Test]
    public void PipelineSteps_ConstructorDependencies_MustBeAbstractions()
    {
        // Real boundary: steps receive DI collaborators through constructors.
        // Concrete config/storage/options types must not be constructor parameters.
        // (Type-graph HaveDependencyOn denylists miss new concrete types until listed by hand.)
        var violations = PipelineStepDependencyInspector.GetPipelineStepTypes()
            .SelectMany(PipelineStepDependencyInspector.GetForbiddenConstructorParameters)
            .ToList();

        Assert.That(violations, Is.Empty,
            "Pipeline steps must take constructor abstractions only. Violations:\n"
            + string.Join("\n", violations));
    }

    [Test]
    public void PipelineSteps_DirectTelegramTypeRefs_StayWithinBaseline()
    {
        // NetArchTest HaveDependencyOn("Telegram.Bot") is transitive and true for every step
        // that touches MessageContext. We freeze *direct* Telegram.* type refs per FullName so
        // new SDK surface on an existing step fails the gate.
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
            // Baseline shrink (removed usage) is allowed; only expansions fail the gate.
        }

        // Baseline entries must still exist as step types (typos / renames break the gate).
        foreach (var key in baseline.Keys)
        {
            if (!actual.ContainsKey(key))
                failures.Add($"Baseline lists unknown step FullName: {key}");
        }

        Assert.That(failures, Is.Empty,
            "Pipeline steps Telegram surface must stay within the frozen baseline.\n"
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

    private static void AssertRulePasses(TestResult result, string rule)
    {
        var failures = string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
        Assert.That(result.IsSuccessful, Is.True, $"{rule}. Violations: {failures}");
    }
}
