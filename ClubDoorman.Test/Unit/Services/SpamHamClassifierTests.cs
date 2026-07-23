using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("business-logic")]
public class SpamHamClassifierTests
{
    private SpamHamClassifier _classifier = null!;

    [SetUp]
    public void SetUp()
    {
        var appConfig = new Mock<IAppConfig>();
        appConfig.SetupGet(x => x.GoldenBaselineMode).Returns(true);

        _classifier = new SpamHamClassifier(NullLogger<SpamHamClassifier>.Instance, appConfig.Object);
    }

    [Test]
    public async Task IsSpam_SpamMessage_DetectsSpam()
    {
        var message = "🔥🔥🔥 СРОЧНО! ЗАРАБОТАЙ 1000000$ ЗА ДЕНЬ! 🔥🔥🔥 ПЕРЕХОДИ ПО ССЫЛКЕ: https://scam.com";

        var result = await _classifier.IsSpam(message);

        Assert.That(result.Score, Is.GreaterThan(0.5), "Спам сообщение должно иметь высокую вероятность");
        Assert.That(result.Spam, Is.True, "Сообщение должно быть классифицировано как спам");
    }

    [Test]
    public async Task IsSpam_HamMessage_DetectsHam()
    {
        var message = "Привет всем! Как дела? Надеюсь, у всех все хорошо.";

        var result = await _classifier.IsSpam(message);

        Assert.That(result.Score, Is.LessThan(0.5), "Нормальное сообщение должно иметь низкую вероятность спама");
        Assert.That(result.Spam, Is.False, "Сообщение должно быть классифицировано как не спам");
    }
}
