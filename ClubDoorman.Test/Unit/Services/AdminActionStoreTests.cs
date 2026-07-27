using ClubDoorman.Services.Messaging;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
public class AdminActionStoreTests
{
    [Test]
    public async Task PutProfileReview_TwoConcurrentReviewsForSameUserAndChat_KeepsIndependentState()
    {
        using var store = new AdminActionStore();
        var first = CreateState(1);
        var second = CreateState(2);

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
        var state = CreateState(1);
        var token = store.PutProfileReview(state, TimeSpan.FromMinutes(1));

        Assert.That(store.TakeProfileReview(token), Is.SameAs(state));
        Assert.That(store.TakeProfileReview(token), Is.Null);
    }

    [Test]
    public void DiscardProfileReview_RemovesState()
    {
        using var store = new AdminActionStore();
        var token = store.PutProfileReview(CreateState(1), TimeSpan.FromMinutes(1));

        store.DiscardProfileReview(token);

        Assert.That(store.TakeProfileReview(token), Is.Null);
    }

    [Test]
    public async Task TakeProfileReview_AfterExpiration_ReturnsNull()
    {
        using var store = new AdminActionStore();
        var token = store.PutProfileReview(CreateState(1), TimeSpan.FromMilliseconds(20));

        await Task.Delay(100);

        Assert.That(store.TakeProfileReview(token), Is.Null);
    }

    private static ProfileReviewActionState CreateState(long messageId) => new(-456, 123, messageId);
}
