using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Dispatcher;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("services")]
[Category("dispatcher")]
public class UpdateDispatcherTests
{
    private Mock<ILogger<UpdateDispatcher>> _loggerMock;
    private Mock<IUpdateHandler> _handler1Mock;
    private Mock<IUpdateHandler> _handler2Mock;
    private UpdateDispatcher _dispatcher;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<UpdateDispatcher>>();
        _handler1Mock = new Mock<IUpdateHandler>();
        _handler2Mock = new Mock<IUpdateHandler>();

        var handlers = new List<IUpdateHandler> { _handler1Mock.Object, _handler2Mock.Object };
        _dispatcher = new UpdateDispatcher(handlers, _loggerMock.Object);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_ValidUpdate_ProcessesAllHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(false);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_AllHandlersCanHandle_CallsAllHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(true);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_NoHandlersCanHandle_LogsDebugInfo()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(false);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(false);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_HandlerThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var update = CreateTestUpdate();
        var expectedException = new Exception("Handler error");
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.DispatchAsync(update));

        Assert.That(exception, Is.EqualTo(expectedException));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_OperationCanceledException_LogsInfoAndRethrows()
    {
        // Arrange
        var update = CreateTestUpdate();
        var expectedException = new OperationCanceledException();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _dispatcher.DispatchAsync(update));

        Assert.That(exception, Is.EqualTo(expectedException));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_WithCancellationToken_PassesTokenToHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        var cancellationToken = new CancellationToken();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);

        // Act
        await _dispatcher.DispatchAsync(update, cancellationToken);

        // Assert
        _handler1Mock.Verify(x => x.HandleAsync(update, cancellationToken), Times.Once);
    }

    [Test]
    [Category("integration")]
    public async Task DispatchAsync_MultipleHandlersWithDifferentResponses_ProcessesCorrectly()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(true);

        var handler1Called = false;
        var handler2Called = false;

        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .Callback(() => handler1Called = true)
            .Returns(Task.CompletedTask);

        _handler2Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .Callback(() => handler2Called = true)
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        Assert.That(handler1Called, Is.True);
        Assert.That(handler2Called, Is.True);
    }

    private static Update CreateTestUpdate()
    {
        return new Update
        {
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 12345 },
                From = new User { Id = 67890 },
                Text = "Test message"
            }
        };
    }
}