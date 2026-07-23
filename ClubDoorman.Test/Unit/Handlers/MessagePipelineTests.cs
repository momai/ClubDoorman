using ClubDoorman.Services.Handlers.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
public class MessagePipelineTests
{
    [Test]
    public async Task RunAsync_OrdersStepsByOrder()
    {
        var execution = new List<string>();
        var pipeline = CreatePipeline(
            new RecordingStep(20, "second", execution),
            new RecordingStep(10, "first", execution));

        await pipeline.RunAsync(CreateContext(), CancellationToken.None);

        Assert.That(execution, Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public async Task RunAsync_StopResult_DoesNotExecuteLaterSteps()
    {
        var execution = new List<string>();
        var pipeline = CreatePipeline(
            new RecordingStep(10, "stop", execution, StepResult.StopOk("stop")),
            new RecordingStep(20, "later", execution));

        await pipeline.RunAsync(CreateContext(), CancellationToken.None);

        Assert.That(execution, Is.EqualTo(new[] { "stop" }));
    }

    [Test]
    public async Task RunAsync_FailedResult_DoesNotExecuteLaterSteps()
    {
        var execution = new List<string>();
        var pipeline = CreatePipeline(
            new RecordingStep(10, "failed", execution, StepResult.Fail(new InvalidOperationException("test failure"))),
            new RecordingStep(20, "later", execution));

        await pipeline.RunAsync(CreateContext(), CancellationToken.None);

        Assert.That(execution, Is.EqualTo(new[] { "failed" }));
    }

    private static MessagePipeline CreatePipeline(params IMessageStep[] steps) =>
        new(steps, NullLogger<MessagePipeline>.Instance);

    private static MessageContext CreateContext()
    {
        var message = new Message
        {
            Chat = new Chat { Id = -100123 },
            Text = "test"
        };
        return new MessageContext
        {
            Update = new Update { Message = message },
            Message = message
        };
    }

    private sealed class RecordingStep : IMessageStep
    {
        private readonly List<string> _execution;
        private readonly StepResult _result;

        public RecordingStep(int order, string name, List<string> execution, StepResult? result = null)
        {
            Order = order;
            Name = name;
            _execution = execution;
            _result = result ?? StepResult.Continue();
        }

        public int Order { get; }
        public string Name { get; }

        public Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
        {
            _execution.Add(Name);
            return Task.FromResult(_result);
        }
    }
}
