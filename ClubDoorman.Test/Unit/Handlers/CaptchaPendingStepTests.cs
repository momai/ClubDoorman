using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers.Pipeline;
using ClubDoorman.Services.Handlers.Pipeline.Steps;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
public class CaptchaPendingStepTests
{
    [Test]
    public async Task ExecuteAsync_ChannelPost_ContinuesWithoutCaptchaOrDeletion()
    {
        var captchaService = new Mock<ICaptchaService>();
        var bot = new Mock<ITelegramBotClientWrapper>();
        var eventsPublisher = new Mock<IModerationEventPublisher>();
        var step = new CaptchaPendingStep(
            captchaService.Object,
            bot.Object,
            eventsPublisher.Object,
            NullLogger<CaptchaPendingStep>.Instance);
        var message = new Message
        {
            Chat = new Chat { Id = -100123, Type = ChatType.Channel },
            SenderChat = new Chat { Id = -100456, Type = ChatType.Channel },
            Text = "channel post"
        };
        var context = new MessageContext
        {
            Update = new Update { Message = message },
            Message = message
        };

        var result = await step.ExecuteAsync(context, CancellationToken.None);

        Assert.That(result.Stop, Is.False);
        Assert.That(context.CaptchaPendingHandled, Is.False);
        captchaService.Verify(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        captchaService.Verify(x => x.GetCaptchaInfo(It.IsAny<string>()), Times.Never);
        bot.Verify(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        eventsPublisher.Verify(x => x.Publish(It.IsAny<string?>(), It.IsAny<ModerationEvent>()), Times.Never);
    }
}
