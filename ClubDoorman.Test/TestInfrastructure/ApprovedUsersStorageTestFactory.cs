using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ApprovedUsersStorage
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ApprovedUsersStorageTestFactory
{
    public Mock<ILogger<ApprovedUsersStorage>> LoggerMock { get; } = new();

    public ApprovedUsersStorage CreateApprovedUsersStorage()
    {
        return new ApprovedUsersStorage(
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public ApprovedUsersStorageTestFactory WithLoggerSetup(Action<Mock<ILogger<ApprovedUsersStorage>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }
    #endregion
}
