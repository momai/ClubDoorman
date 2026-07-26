using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using Moq;
using NUnit.Framework;
using System.Reflection;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[NonParallelizable]
public class GlobalStatsManagerConcurrencyTests
{
    private string _dataRoot = null!;
    private string? _previousDataRoot;

    [SetUp]
    public void SetUp()
    {
        _dataRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _previousDataRoot = Environment.GetEnvironmentVariable("DOORMAN_DATA_ROOT");
        Environment.SetEnvironmentVariable("DOORMAN_DATA_ROOT", _dataRoot);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("DOORMAN_DATA_ROOT", _previousDataRoot);
        if (Directory.Exists(_dataRoot))
            Directory.Delete(_dataRoot, true);
    }

    [Test]
    public async Task UpdateAllMembersAsync_WaitsForStatisticsLockBeforeSnapshot()
    {
        var manager = new GlobalStatsManager();
        manager.IncCaptcha(1, "Test chat");
        var bot = CreateBotThatSignalsCall(out var called);
        var statisticsLock = GetStatisticsLock(manager);

        Monitor.Enter(statisticsLock);
        try
        {
            var updateTask = Task.Run(() => manager.UpdateAllMembersAsync(bot.Object));

            Assert.That(called.Task.Wait(100), Is.False);

            Monitor.Exit(statisticsLock);
            await updateTask;
        }
        finally
        {
            if (Monitor.IsEntered(statisticsLock))
                Monitor.Exit(statisticsLock);
        }

        Assert.That(called.Task.IsCompleted, Is.True);
    }

    [Test]
    public async Task UpdateZeroMemberChatsAsync_WaitsForStatisticsLockBeforeSnapshot()
    {
        var manager = new GlobalStatsManager();
        manager.IncCaptcha(1, "Test chat");
        var bot = CreateBotThatSignalsCall(out var called);
        var statisticsLock = GetStatisticsLock(manager);

        Monitor.Enter(statisticsLock);
        try
        {
            var updateTask = Task.Run(() => manager.UpdateZeroMemberChatsAsync(bot.Object));

            Assert.That(called.Task.Wait(100), Is.False);

            Monitor.Exit(statisticsLock);
            await updateTask;
        }
        finally
        {
            if (Monitor.IsEntered(statisticsLock))
                Monitor.Exit(statisticsLock);
        }

        Assert.That(called.Task.IsCompleted, Is.True);
    }

    private static Mock<ITelegramBotClientWrapper> CreateBotThatSignalsCall(out TaskCompletionSource called)
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        called = signal;
        var bot = new Mock<ITelegramBotClientWrapper>();
        bot.Setup(x => x.GetChatMemberCount(It.IsAny<Telegram.Bot.Types.ChatId>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                signal.SetResult();
                return Task.FromResult(1);
            });
        return bot;
    }

    private static object GetStatisticsLock(GlobalStatsManager manager) => typeof(GlobalStatsManager)
        .GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(manager)!;
}
