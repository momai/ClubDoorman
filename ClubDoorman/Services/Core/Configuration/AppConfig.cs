using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Options;

/// <summary>
/// Реализация конфигурации приложения
/// Переносит логику из статического класса Config.cs для лучшей тестируемости
/// </summary>
public class AppConfig : IAppConfig
{
    private readonly IOptions<AutoBanOptions> _autoBanOptions;
    private readonly IOptions<ViolationThresholdOptions> _violationThresholdOptions;
    private readonly IOptions<FeatureToggleOptions> _featureToggleOptions;
    private readonly IOptions<ChatFilteringOptions> _chatFilteringOptions;
    private readonly IOptions<CoreOptions> _coreOptions;
    private readonly IOptions<ChatAccessOptions> _chatAccessOptions;
    private readonly IOptions<AiOptions> _aiOptions;
    private readonly IOptions<TestHarnessOptions>? _testHarnessOptions;

    public AppConfig(
        IOptions<AutoBanOptions> autoBanOptions,
        IOptions<ViolationThresholdOptions> violationThresholdOptions,
        IOptions<FeatureToggleOptions> featureToggleOptions,
        IOptions<ChatFilteringOptions> chatFilteringOptions,
        IOptions<CoreOptions> coreOptions,
        IOptions<ChatAccessOptions> chatAccessOptions,
    IOptions<AiOptions> aiOptions,
    IOptions<TestHarnessOptions>? testHarnessOptions)
    {
        _autoBanOptions = autoBanOptions;
        _violationThresholdOptions = violationThresholdOptions;
        _featureToggleOptions = featureToggleOptions;
        _chatFilteringOptions = chatFilteringOptions;
        _coreOptions = coreOptions;
        _chatAccessOptions = chatAccessOptions;
        _aiOptions = aiOptions;
    _testHarnessOptions = testHarnessOptions ?? Microsoft.Extensions.Options.Options.Create(new TestHarnessOptions());
        Effects = new EffectsConfiguration();
    }

    /// <summary>
    /// API токен для OpenRouter
    /// </summary>
    public string? OpenRouterApi => _aiOptions.Value.OpenRouterApi; // полностью мигрировано в AiOptions

    /// <summary>
    /// Включено ли обнаружение подозрительных пользователей
    /// </summary>
    public bool SuspiciousDetectionEnabled => _aiOptions.Value.SuspiciousDetectionEnabled;

    /// <summary>
    /// Порог мимикрии для обнаружения подозрительных пользователей
    /// </summary>
    public double MimicryThreshold => _aiOptions.Value.MimicryThreshold;

    /// <summary>
    /// Количество сообщений для перехода из подозрительных в одобренные
    /// </summary>
    public int SuspiciousToApprovedMessageCount => _aiOptions.Value.SuspiciousToApprovedMessageCount;

    /// <summary>
    /// ID админского чата
    /// </summary>
    public long AdminChatId => _coreOptions.Value.AdminChatId;

    /// <summary>
    /// ID чата для логирования
    /// </summary>
    public long LogAdminChatId => _coreOptions.Value.LogAdminChatId == 0 ? AdminChatId : _coreOptions.Value.LogAdminChatId;

    /// <summary>
    /// Список чатов с включенным AI
    /// </summary>
    public HashSet<long> AiEnabledChats => _aiOptions.Value.AiEnabledChats;

    /// <summary>
    /// Включен ли AI для конкретного чата
    /// </summary>
    public bool IsAiEnabledForChat(long chatId)
    {
        var set = AiEnabledChats;
        return set.Count == 0 || set.Contains(chatId);
    }

    /// <summary>
    /// Разрешён ли чат для работы бота
    /// </summary>
    public bool IsChatAllowed(long chatId)
    {
        var wl = WhitelistChats;
        return wl.Count == 0 || wl.Contains(chatId);
    }

    /// <summary>
    /// Разрешён ли приватный старт
    /// </summary>
    public bool IsPrivateStartAllowed()
    {
        // Если whitelist не пуст - команда /start в личке отключена
        return WhitelistChats.Count == 0;
    }

    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    public string BotApi => _coreOptions.Value.BotApi ?? string.Empty; // гарантируем непустую строку вместо null

    /// <summary>
    /// Токен сервиса клуба
    /// </summary>
    public string? ClubServiceToken => _coreOptions.Value.ClubServiceToken;

    /// <summary>
    /// URL клуба
    /// </summary>
    public string ClubUrl => _coreOptions.Value.ClubUrl;

    /// <summary>
    /// Отключенные чаты
    /// </summary>
    public HashSet<long> DisabledChats => _chatAccessOptions.Value.DisabledChats;

    /// <summary>
    /// Whitelist групп - если указан, бот работает только в этих группах
    /// </summary>
    public HashSet<long> WhitelistChats => _chatAccessOptions.Value.WhitelistChats;

    /// <summary>
    /// Группы, где не показывать рекламу
    /// </summary>
    public HashSet<long> NoVpnAdGroups => _chatAccessOptions.Value.NoVpnAdGroups;

    /// <summary>
    /// Группы, в которых отключена капча
    /// </summary>
    public HashSet<long> NoCaptchaGroups => _chatAccessOptions.Value.NoCaptchaGroups;

    /// <summary>
    /// Включен ли фильтр ссылок
    /// </summary>
    public bool TextMentionFilterEnabled => _featureToggleOptions.Value.TextMentionFilterEnabled;

    /// <summary>
    /// Автоматически банить пользователей, входящих через папки
    /// </summary>
    public bool BanFolderInviteUsers => _autoBanOptions.Value.BanFolderInviteUsers;

    /// <summary>
    /// Количество повторных нарушений ML фильтра перед баном
    /// </summary>
    public int MlViolationsBeforeBan => _violationThresholdOptions.Value.MlViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений стоп-слов перед баном
    /// </summary>
    public int StopWordsViolationsBeforeBan => _violationThresholdOptions.Value.StopWordsViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений эмодзи перед баном
    /// </summary>
    public int EmojiViolationsBeforeBan => _violationThresholdOptions.Value.EmojiViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений lookalike символов перед баном
    /// </summary>
    public int LookalikeViolationsBeforeBan => _violationThresholdOptions.Value.LookalikeViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений банальных приветствий перед баном
    /// </summary>
    public int BoringGreetingsViolationsBeforeBan => _violationThresholdOptions.Value.BoringGreetingsViolationsBeforeBan;

    /// <summary>
    /// Количество непройденных капч перед баном
    /// </summary>
    public int CaptchaViolationsBeforeBan => _violationThresholdOptions.Value.CaptchaViolationsBeforeBan;

    /// <summary>
    /// Отправлять уведомления о банах за повторные нарушения в админ-чат вместо лог-чата
    /// </summary>
    public bool RepeatedViolationsBanToAdminChat => _featureToggleOptions.Value.RepeatedViolationsBanToAdminChat;

    /// <summary>
    /// Отключить уведомления о банах за вход через папки в лог-чате
    /// </summary>
    public bool BanFolderInviteNotificationsDisable => _featureToggleOptions.Value.BanFolderInviteNotificationsDisable;

    // === НОВЫЕ СВОЙСТВА ИЗ STRONGLY-TYPED OPTIONS ===

    /// <summary>
    /// Автоматически банить пользователей из черного списка
    /// </summary>
    public bool BlacklistAutoBan => _autoBanOptions.Value.BlacklistAutoBan;

    /// <summary>
    /// Автоматически банить каналы
    /// </summary>
    public bool ChannelAutoBan => _autoBanOptions.Value.ChannelAutoBan;



    /// <summary>
    /// Автоматически банить пользователей с похожими именами
    /// </summary>
    public bool LookAlikeAutoBan => _autoBanOptions.Value.LookAlikeAutoBan;

    /// <summary>
    /// Автоматически банить по кнопкам
    /// </summary>
    public bool ButtonAutoBan => _autoBanOptions.Value.ButtonAutoBan;

    /// <summary>
    /// Автоматически банить при высокой уверенности
    /// </summary>
    public bool HighConfidenceAutoBan => _autoBanOptions.Value.HighConfidenceAutoBan;

    /// <summary>
    /// Пересылать сообщения с низкой уверенностью в ham
    /// </summary>
    public bool LowConfidenceHamForward => _featureToggleOptions.Value.LowConfidenceHamForward;

    /// <summary>
    /// Включить кнопку одобрения
    /// </summary>
    public bool ApproveButtonEnabled => _featureToggleOptions.Value.ApproveButtonEnabled;

    /// <summary>
    /// Удаление пересланных сообщений от новичков
    /// </summary>
    public bool DeleteForwardedMessages => _featureToggleOptions.Value.DeleteForwardedMessages;

    /// <summary>
    /// Отключить приветственные сообщения
    /// </summary>
    public bool DisableWelcome => _featureToggleOptions.Value.DisableWelcome;

    /// <summary>
    /// Отключить фильтрацию картинок/видео/документов глобально
    /// </summary>
    public bool DisableMediaFiltering => _featureToggleOptions.Value.DisableMediaFiltering;

    /// <summary>
    /// Режим автоодобрения пользователей (true = глобальный, false = групповой)
    /// </summary>
    public bool GlobalApprovalMode => _featureToggleOptions.Value.GlobalApprovalMode;

    /// <summary>
    /// Группы где фильтрация медиа отключена
    /// </summary>
    public HashSet<long> MediaFilteringDisabledChats => _chatFilteringOptions.Value.MediaFilteringDisabledChats;

    /// <summary>
    /// Проверяет, отключена ли фильтрация медиа для данного чата
    /// </summary>
    public bool IsMediaFilteringDisabledForChat(long chatId)
    {
        // Если глобально отключено - фильтрация отключена везде
        if (DisableMediaFiltering)
            return true;

        // Если чат в списке исключений - фильтрация отключена
        return MediaFilteringDisabledChats.Contains(chatId);
    }

    // === КОНФИГУРАЦИЯ ЭФФЕКТОВ МОДЕРАЦИИ ===

    /// <summary>
    /// Конфигурация эффектов модерации
    /// </summary>
    public EffectsConfiguration Effects { get; }

    // === ТЕСТОВЫЕ / GOLDEN НАСТРОЙКИ ===

    public bool GoldenBaselineMode => _testHarnessOptions?.Value?.GoldenBaselineMode ?? false;

    public HashSet<long> TestBlacklistUserIds => _testHarnessOptions?.Value?.TestBlacklistUserIds ?? _emptySet;

    private static readonly HashSet<long> _emptySet = new();

}