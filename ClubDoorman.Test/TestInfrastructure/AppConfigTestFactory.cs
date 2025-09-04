using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Moq;
using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Фабрика для создания моков IAppConfig в тестах
/// </summary>
public static class AppConfigTestFactory
{
    /// <summary>
    /// Создаёт мок IAppConfig с настройками по умолчанию для тестов
    /// </summary>
    public static IAppConfig CreateDefault()
    {
        var mock = new Mock<IAppConfig>();

        // Настройки по умолчанию для тестов
        mock.Setup(x => x.OpenRouterApi).Returns("test-api-key");
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(true);
        mock.Setup(x => x.MimicryThreshold).Returns(0.7);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(3);
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(123456789);
        mock.Setup(x => x.AiEnabledChats).Returns(new HashSet<long> { 123456789 });
    mock.Setup(x => x.GoldenBaselineMode).Returns(false);
    mock.Setup(x => x.TestBlacklistUserIds).Returns(new HashSet<long>());

        // Группа 2: Настройки чатов и разрешений
        mock.Setup(x => x.BotApi).Returns("test-bot-api");
        mock.Setup(x => x.ClubServiceToken).Returns("test-club-token");
        mock.Setup(x => x.ClubUrl).Returns("https://test.club/");
        mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.WhitelistChats).Returns(new HashSet<long>());
        mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());

        // Группа 3: Feature Toggles и настройки бана
        mock.Setup(x => x.BanFolderInviteUsers).Returns(false);
        mock.Setup(x => x.BanFolderInviteNotificationsDisable).Returns(false);
        mock.Setup(x => x.RepeatedViolationsBanToAdminChat).Returns(false);
        mock.Setup(x => x.TextMentionFilterEnabled).Returns(false);
        mock.Setup(x => x.DeleteForwardedMessages).Returns(false);
        mock.Setup(x => x.DisableWelcome).Returns(false);
        mock.Setup(x => x.DisableMediaFiltering).Returns(false);
        mock.Setup(x => x.GlobalApprovalMode).Returns(true);
        mock.Setup(x => x.MediaFilteringDisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.IsMediaFilteringDisabledForChat(It.IsAny<long>())).Returns(false);

        // Группа 4: Автобан настройки
        mock.Setup(x => x.BlacklistAutoBan).Returns(true);
        mock.Setup(x => x.ChannelAutoBan).Returns(true);
        mock.Setup(x => x.LookAlikeAutoBan).Returns(true);
        mock.Setup(x => x.ButtonAutoBan).Returns(true);
        mock.Setup(x => x.HighConfidenceAutoBan).Returns(true);
        mock.Setup(x => x.LowConfidenceHamForward).Returns(false);
        mock.Setup(x => x.ApproveButtonEnabled).Returns(false);

        // Группа 5: Пороговые значения
        mock.Setup(x => x.MlViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.StopWordsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.EmojiViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.LookalikeViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.BoringGreetingsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.CaptchaViolationsBeforeBan).Returns(0);

        // Эффекты модерации
        mock.Setup(x => x.Effects).Returns(new ClubDoorman.Infrastructure.EffectsConfiguration());

        // Методы
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(true);

        return mock.Object;
    }

    /// <summary>
    /// Создаёт мок IAppConfig с отключенным AI
    /// </summary>
    public static IAppConfig CreateWithoutAi()
    {
        var mock = new Mock<IAppConfig>();

        // Настройки без AI
        mock.Setup(x => x.OpenRouterApi).Returns((string?)null);
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(false);
        mock.Setup(x => x.MimicryThreshold).Returns(0.7);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(3);
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(123456789);
        mock.Setup(x => x.AiEnabledChats).Returns(new HashSet<long>());
    mock.Setup(x => x.GoldenBaselineMode).Returns(false);
    mock.Setup(x => x.TestBlacklistUserIds).Returns(new HashSet<long>());

        // Группа 2: Настройки чатов и разрешений
        mock.Setup(x => x.BotApi).Returns("test-bot-api");
        mock.Setup(x => x.ClubServiceToken).Returns("test-club-token");
        mock.Setup(x => x.ClubUrl).Returns("https://test.club/");
        mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.WhitelistChats).Returns(new HashSet<long>());
        mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());

        // Группа 3: Feature Toggles и настройки бана
        mock.Setup(x => x.BanFolderInviteUsers).Returns(false);
        mock.Setup(x => x.BanFolderInviteNotificationsDisable).Returns(false);
        mock.Setup(x => x.RepeatedViolationsBanToAdminChat).Returns(false);
        mock.Setup(x => x.TextMentionFilterEnabled).Returns(false);
        mock.Setup(x => x.DeleteForwardedMessages).Returns(false);
        mock.Setup(x => x.DisableWelcome).Returns(false);
        mock.Setup(x => x.DisableMediaFiltering).Returns(false);
        mock.Setup(x => x.GlobalApprovalMode).Returns(true);
        mock.Setup(x => x.MediaFilteringDisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.IsMediaFilteringDisabledForChat(It.IsAny<long>())).Returns(false);

        // Группа 4: Автобан настройки
        mock.Setup(x => x.BlacklistAutoBan).Returns(true);
        mock.Setup(x => x.ChannelAutoBan).Returns(true);
        mock.Setup(x => x.LookAlikeAutoBan).Returns(true);
        mock.Setup(x => x.ButtonAutoBan).Returns(true);
        mock.Setup(x => x.HighConfidenceAutoBan).Returns(true);
        mock.Setup(x => x.LowConfidenceHamForward).Returns(false);
        mock.Setup(x => x.ApproveButtonEnabled).Returns(false);

        // Группа 5: Пороговые значения
        mock.Setup(x => x.MlViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.StopWordsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.EmojiViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.LookalikeViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.BoringGreetingsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.CaptchaViolationsBeforeBan).Returns(0);

        // Эффекты модерации
        mock.Setup(x => x.Effects).Returns(new ClubDoorman.Infrastructure.EffectsConfiguration());

        // Методы
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(false);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(true);

        return mock.Object;
    }

    /// <summary>
    /// Создаёт мок IAppConfig с кастомными настройками
    /// </summary>
    public static IAppConfig CreateCustom(
        string? openRouterApi = "test-api-key",
        bool suspiciousDetectionEnabled = true,
        double mimicryThreshold = 0.7,
        int suspiciousToApprovedMessageCount = 3,
        long adminChatId = 123456789,
        long logAdminChatId = 123456789,
        HashSet<long>? aiEnabledChats = null,
        bool isAiEnabledForChat = true,
        bool isChatAllowed = true,
        bool isPrivateStartAllowed = true)
    {
        var mock = new Mock<IAppConfig>();

        mock.Setup(x => x.OpenRouterApi).Returns(openRouterApi);
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(suspiciousDetectionEnabled);
        mock.Setup(x => x.MimicryThreshold).Returns(mimicryThreshold);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(suspiciousToApprovedMessageCount);
        mock.Setup(x => x.AdminChatId).Returns(adminChatId);
        mock.Setup(x => x.LogAdminChatId).Returns(logAdminChatId);
        mock.Setup(x => x.AiEnabledChats).Returns(aiEnabledChats ?? new HashSet<long> { adminChatId });
    mock.Setup(x => x.GoldenBaselineMode).Returns(false);
    mock.Setup(x => x.TestBlacklistUserIds).Returns(new HashSet<long>());

        // Группа 2: Настройки чатов и разрешений
        mock.Setup(x => x.BotApi).Returns("test-bot-api");
        mock.Setup(x => x.ClubServiceToken).Returns("test-club-token");
        mock.Setup(x => x.ClubUrl).Returns("https://test.club/");
        mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.WhitelistChats).Returns(new HashSet<long>());
        mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());

        // Группа 3: Feature Toggles и настройки бана
        mock.Setup(x => x.BanFolderInviteUsers).Returns(false);
        mock.Setup(x => x.BanFolderInviteNotificationsDisable).Returns(false);
        mock.Setup(x => x.RepeatedViolationsBanToAdminChat).Returns(false);
        mock.Setup(x => x.TextMentionFilterEnabled).Returns(false);
        mock.Setup(x => x.DeleteForwardedMessages).Returns(false);
        mock.Setup(x => x.DisableWelcome).Returns(false);
        mock.Setup(x => x.DisableMediaFiltering).Returns(false);
        mock.Setup(x => x.GlobalApprovalMode).Returns(true);
        mock.Setup(x => x.MediaFilteringDisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.IsMediaFilteringDisabledForChat(It.IsAny<long>())).Returns(false);

        // Группа 4: Автобан настройки
        mock.Setup(x => x.BlacklistAutoBan).Returns(true);
        mock.Setup(x => x.ChannelAutoBan).Returns(true);
        mock.Setup(x => x.LookAlikeAutoBan).Returns(true);
        mock.Setup(x => x.ButtonAutoBan).Returns(true);
        mock.Setup(x => x.HighConfidenceAutoBan).Returns(true);
        mock.Setup(x => x.LowConfidenceHamForward).Returns(false);
        mock.Setup(x => x.ApproveButtonEnabled).Returns(false);

        // Группа 5: Пороговые значения
        mock.Setup(x => x.MlViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.StopWordsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.EmojiViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.LookalikeViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.BoringGreetingsViolationsBeforeBan).Returns(0);
        mock.Setup(x => x.CaptchaViolationsBeforeBan).Returns(0);

        // Эффекты модерации
        mock.Setup(x => x.Effects).Returns(new ClubDoorman.Infrastructure.EffectsConfiguration());

        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(isAiEnabledForChat);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(isChatAllowed);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(isPrivateStartAllowed);

        return mock.Object;
    }
}