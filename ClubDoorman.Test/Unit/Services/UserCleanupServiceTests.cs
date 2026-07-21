using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для UserCleanupService
/// </summary>
[TestFixture]
[Category("business-logic")]
public class UserCleanupServiceTests
{
    private UserCleanupServiceTestFactory _factory = null!;
    private UserCleanupService _service = null!;
    private ApprovedUsersStorage _approvedUsersStorage = null!;
    private Mock<ILogger<UserCleanupService>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new UserCleanupServiceTestFactory();
        _approvedUsersStorage = new ApprovedUsersStorage(_factory.ApprovedUsersStorageLoggerMock.Object);
        _service = new UserCleanupService(_approvedUsersStorage, _factory.LoggerMock.Object);
        _loggerMock = _factory.LoggerMock;
    }

    [Test]
    public void RemoveUserFromAllApprovals_WhenUserExists_ReturnsTrue()
    {
        // Arrange
        var userId = 123456L;
        var reason = "Тестовая очистка";
        _approvedUsersStorage.ApproveUserGlobally(userId);

        // Act
        var result = _service.RemoveUserFromAllApprovals(userId, reason);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_approvedUsersStorage.IsGloballyApproved(userId), Is.False);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(userId.ToString()) && v.ToString().Contains(reason)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void RemoveUserFromAllApprovals_WhenUserNotExists_ReturnsFalse()
    {
        // Arrange
        var userId = 123456L;
        var reason = "Тестовая очистка";

        // Act
        var result = _service.RemoveUserFromAllApprovals(userId, reason);

        // Assert
        Assert.That(result, Is.False);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Test]
    public void RemoveUserFromGroupApproval_WhenUserExists_ReturnsTrue()
    {
        // Arrange
        var userId = 123456L;
        var groupId = 789L;
        var reason = "Тестовая очистка группы";
        _approvedUsersStorage.ApproveUserInGroup(userId, groupId);

        // Act
        var result = _service.RemoveUserFromGroupApproval(userId, groupId, reason);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_approvedUsersStorage.IsApprovedInGroup(userId, groupId), Is.False);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(userId.ToString()) && v.ToString().Contains(groupId.ToString()) && v.ToString().Contains(reason)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void RemoveUserFromGroupApproval_WhenUserNotExists_ReturnsFalse()
    {
        // Arrange
        var userId = 123456L;
        var groupId = 789L;
        var reason = "Тестовая очистка группы";

        // Act
        var result = _service.RemoveUserFromGroupApproval(userId, groupId, reason);

        // Assert
        Assert.That(result, Is.False);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Test]
    public void RemoveUserFromGroupApproval_UserNotApprovedInGroup_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;

        // Act
        var result = _service.RemoveUserFromGroupApproval(userId, groupId, "Тест");

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_approvedUsersStorage.IsApprovedInGroup(userId, groupId), Is.False);
    }

    [Test]
    public void RemoveUserFromGlobalApproval_UserApprovedGlobally_ReturnsTrue()
    {
        // Arrange
        var userId = 12345L;
        _approvedUsersStorage.ApproveUserGlobally(userId);

        // Act
        var result = _service.RemoveUserFromGlobalApproval(userId, "Тест");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_approvedUsersStorage.IsGloballyApproved(userId), Is.False);
    }

    [Test]
    public void RemoveUserFromGlobalApproval_UserNotApprovedGlobally_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;

        // Act
        var result = _service.RemoveUserFromGlobalApproval(userId, "Тест");

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_approvedUsersStorage.IsGloballyApproved(userId), Is.False);
    }
}