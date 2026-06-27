using ClubDoorman.Services.UserBan;
using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("delete-message-later")]
public class MessageHandlerDeleteMessageLaterTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MessageHandlerTestFactory();
        _messageHandler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task DeleteMessageLater_WithShortTimeout_InvokesDelete()
    {
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var envelope = MessageEnvelope.CreateTest(messageId: 12345, chatId: 67890);
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);
        var messageHandler = _factory.CreateMessageHandlerWithFake(fakeClient);

        messageHandler.DeleteMessageLater(message, TimeSpan.FromMilliseconds(40), CancellationToken.None);

        await Task.Delay(120);
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
    }

    [Test]
    public async Task DeleteMessageLater_WhenDeleteFails_LogsWarning()
    {
        var (_, _, message) = TK.Specialized.Messages.TextOnlyScenario();
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.DeleteMessageWithOutcomeAsync(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ChatId c, int m, CancellationToken ct) => new DeleteMessageResult((long)c.Identifier!, m, DeleteMessageOutcome.UnexpectedError, 5, "boom", "raw"));
        });
        _messageHandler.DeleteMessageLater(message, TimeSpan.FromMilliseconds(30), CancellationToken.None);
        await Task.Delay(120);
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v!.ToString()!.Contains("Не удалось удалить сообщение")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task DeleteMessageLater_WhenCancelled_DoesNotInvokeDelete()
    {
        var (_, _, message) = TK.Specialized.Messages.TextOnlyScenario();
        var cts = new CancellationTokenSource();
        _messageHandler.DeleteMessageLater(message, TimeSpan.FromMilliseconds(100), cts.Token);
        cts.Cancel();
        await Task.Delay(150);
        _factory.BotMock.Verify(x => x.DeleteMessageWithOutcomeAsync(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
