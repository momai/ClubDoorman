using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Messaging;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
public class AdminActionStoreTests
{
    [Test]
    public async Task PutProfileReview_TwoConcurrentReviewsForSameUserAndChat_KeepsIndependentState()
    {
        using var store = new AdminActionStore();
        var first = CreateData("first");
        var second = CreateData("second");

        var tokens = await Task.WhenAll(
            Task.Run(() => store.PutProfileReview(first, TimeSpan.FromMinutes(1))),
            Task.Run(() => store.PutProfileReview(second, TimeSpan.FromMinutes(1))));

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0], Is.Not.EqualTo(tokens[1]));
            Assert.That(store.TakeProfileReview(tokens[0]), Is.SameAs(first));
            Assert.That(store.TakeProfileReview(tokens[1]), Is.SameAs(second));
        });
    }

    [Test]
    public void TakeProfileReview_CalledTwice_ReturnsStateOnce()
    {
        using var store = new AdminActionStore();
        var data = CreateData("review");
        var token = store.PutProfileReview(data, TimeSpan.FromMinutes(1));

        Assert.That(store.TakeProfileReview(token), Is.SameAs(data));
        Assert.That(store.TakeProfileReview(token), Is.Null);
    }

    [Test]
    public async Task TakeProfileReview_AfterExpiration_ReturnsNull()
    {
        using var store = new AdminActionStore();
        var token = store.PutProfileReview(CreateData("expired"), TimeSpan.FromMilliseconds(20));

        await Task.Delay(100);

        Assert.That(store.TakeProfileReview(token), Is.Null);
    }

    private static AiProfileAnalysisData CreateData(string reason)
    {
        return new AiProfileAnalysisData(
            new User { Id = 123, FirstName = "User" },
            new Chat { Id = -456, Title = "Chat" },
            0.9,
            reason,
            "bio",
            "message");
    }
}
