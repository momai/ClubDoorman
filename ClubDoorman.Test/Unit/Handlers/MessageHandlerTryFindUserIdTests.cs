using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using System.Runtime.Caching;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.AI;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для TryFindUserIdByUsername метода
/// <tags>unit, message-handler, user-search, cache</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerTryFindUserIdTests
{
    private IUserIndex _userIndex;

    [SetUp]
    public void Setup()
    {
        _userIndex = new ClubDoorman.Services.UserManagement.UserIndex();
        foreach (var item in MemoryCache.Default.ToList())
            MemoryCache.Default.Remove(item.Key);
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем кэш после каждого теста
        foreach (var item in MemoryCache.Default.ToList())
        {
            MemoryCache.Default.Remove(item.Key);
        }
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с пустым username
    /// Проверяет обработку пустой строки
    /// <tags>user-search, empty-input, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithEmptyUsername_ReturnsNull()
    {
        // Arrange
        var username = "";

        // Act
        var result = _userIndex.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с null username
    /// Проверяет обработку null значения
    /// <tags>user-search, null-input, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithNullUsername_ReturnsNull()
    {
        // Arrange
        string? username = null;

        // Act
        var result = _userIndex.TryFindUserIdByUsername(username!);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с регистронезависимым поиском
    /// Проверяет, что поиск не зависит от регистра
    /// <tags>user-search, case-insensitive, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithCaseInsensitiveSearch_FindsUser()
    {
        // Arrange
        var username = "TestUser3";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from @{username.ToLower()}";

        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _userIndex.TryFindUserIdByUsername(username.ToLower());

        // Assert
        Assert.That(result, Is.EqualTo(userId));
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с множественными записями в кэше
    /// Проверяет, что метод находит правильного пользователя среди множества записей
    /// <tags>user-search, multiple-cache-entries, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithMultipleCacheEntries_FindsCorrectUser()
    {
        // Arrange
        var targetUsername = "targetuser4";
        var otherUsername = "otheruser4";
        var chatId1 = 123456L;
        var chatId2 = 789012L;
        var userId1 = 111111L;
        var userId2 = 222222L;

        MemoryCache.Default.Add($"{chatId1}_{userId1}", $"Message from @{targetUsername}", new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });
        MemoryCache.Default.Add($"{chatId2}_{userId2}", $"Message from @{otherUsername}", new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _userIndex.TryFindUserIdByUsername(targetUsername);

        // Assert
        Assert.That(result, Is.EqualTo(userId1));
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с username без @ символа
    /// Проверяет, что метод находит username даже без @ символа
    /// <tags>user-search, username-without-at, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithUsernameWithoutAtSymbol_FindsUser()
    {
        // Arrange
        var username = "testuser8";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from {username}"; // Без @ символа

        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _userIndex.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.EqualTo(userId));
    }

 }