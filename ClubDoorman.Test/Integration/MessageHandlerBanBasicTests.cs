using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.Moderation;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Базовые тесты банов с использованием MessageHandlerTestFactory
/// Демонстрирует рефакторинг старых тестов на новую инфраструктуру
/// <tags>integration, bans, message-handler, test-kit</tags>
/// </summary>
[TestFixture]
[Category("integration")]
public class MessageHandlerBanBasicTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _handler = null!;
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IModerationFacade> _moderationServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MessageHandlerTestFactory()
            .WithStandardMocks()
            .WithBanMocks();

        _botMock = _factory.BotMock;
        _moderationServiceMock = _factory.ModerationServiceMock;

        _handler = _factory.CreateMessageHandler();
    }

    [Test]
    [Category("autofixture")]
    public async Task DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage()
    {
    // This test asserts that a moderation Delete leads to exactly one call to DeleteMessageWithOutcomeAsync.
    // Legacy DeleteMessage path is deprecated in this scenario.
        // Arrange - use envelope tracking because Telegram.Bot MessageId remains 0 in tests.
        var fakeClient = TestKitTelegram.CreateFakeClient();
        var envelope = MessageEnvelope.CreateTest(
            messageId: 12345,
            userId: 123456789,
            chatId: -1001234567890,
            text: "spam");
        var message = TestKitTelegram.CreateMessageFromEnvelope(fakeClient, envelope);

        // Настраиваем модерацию через умные моки
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "ML решил что это спам"));
        var handler = _factory.CreateMessageHandlerWithFake(fakeClient);

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
    }
}
