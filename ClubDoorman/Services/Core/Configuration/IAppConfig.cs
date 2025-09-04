using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Интерфейс для конфигурации приложения
/// Заменяет статические свойства в Config.cs для лучшей тестируемости
/// </summary>
public interface IAppConfig
{
    /// <summary>
    /// API токен для OpenRouter
    /// </summary>
    string? OpenRouterApi { get; }

    /// <summary>
    /// Включено ли обнаружение подозрительных пользователей
    /// </summary>
    bool SuspiciousDetectionEnabled { get; }

    /// <summary>
    /// Порог мимикрии для обнаружения подозрительных пользователей
    /// </summary>
    double MimicryThreshold { get; }

    /// <summary>
    /// Количество сообщений для перехода из подозрительных в одобренные
    /// </summary>
    int SuspiciousToApprovedMessageCount { get; }

    /// <summary>
    /// ID админского чата
    /// </summary>
    long AdminChatId { get; }

    /// <summary>
    /// ID чата для логирования
    /// </summary>
    long LogAdminChatId { get; }

    /// <summary>
    /// Список чатов с включенным AI
    /// </summary>
    HashSet<long> AiEnabledChats { get; }

    /// <summary>
    /// Включен ли AI для конкретного чата
    /// </summary>
    bool IsAiEnabledForChat(long chatId);

    /// <summary>
    /// Разрешён ли чат для работы бота
    /// </summary>
    bool IsChatAllowed(long chatId);

    /// <summary>
    /// Разрешён ли приватный старт
    /// </summary>
    bool IsPrivateStartAllowed();

    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    string BotApi { get; }

    /// <summary>
    /// Токен сервиса клуба
    /// </summary>
    string? ClubServiceToken { get; }

    /// <summary>
    /// URL клуба
    /// </summary>
    string ClubUrl { get; }

    /// <summary>
    /// Отключенные чаты
    /// </summary>
    HashSet<long> DisabledChats { get; }

    /// <summary>
    /// Whitelist групп - если указан, бот работает только в этих группах
    /// </summary>
    HashSet<long> WhitelistChats { get; }

    /// <summary>
    /// Группы, где не показывать рекламу
    /// </summary>
    HashSet<long> NoVpnAdGroups { get; }

    /// <summary>
    /// Группы, в которых отключена капча
    /// </summary>
    HashSet<long> NoCaptchaGroups { get; }

    /// <summary>
    /// Включен ли фильтр ссылок
    /// </summary>
    bool TextMentionFilterEnabled { get; }

    /// <summary>
    /// Автоматически банить пользователей, входящих через папки
    /// </summary>
    bool BanFolderInviteUsers { get; }

    /// <summary>
    /// Количество повторных нарушений ML фильтра перед баном
    /// </summary>
    int MlViolationsBeforeBan { get; }

    /// <summary>
    /// Количество повторных нарушений стоп-слов перед баном
    /// </summary>
    int StopWordsViolationsBeforeBan { get; }

    /// <summary>
    /// Количество повторных нарушений эмодзи перед баном
    /// </summary>
    int EmojiViolationsBeforeBan { get; }

    /// <summary>
    /// Количество повторных нарушений lookalike символов перед баном
    /// </summary>
    int LookalikeViolationsBeforeBan { get; }

    /// <summary>
    /// Количество повторных нарушений банальных приветствий перед баном
    /// </summary>
    int BoringGreetingsViolationsBeforeBan { get; }

    /// <summary>
    /// Количество непройденных капч перед баном
    /// </summary>
    int CaptchaViolationsBeforeBan { get; }

    /// <summary>
    /// Отправлять уведомления о банах за повторные нарушения в админ-чат вместо лог-чата
    /// </summary>
    bool RepeatedViolationsBanToAdminChat { get; }

    /// <summary>
    /// Отключить уведомления о банах за вход через папки в лог-чате
    /// </summary>
    bool BanFolderInviteNotificationsDisable { get; }

    // === НОВЫЕ СВОЙСТВА ИЗ STRONGLY-TYPED OPTIONS ===

    /// <summary>
    /// Автоматически банить пользователей из черного списка
    /// </summary>
    bool BlacklistAutoBan { get; }

    /// <summary>
    /// Автоматически банить каналы
    /// </summary>
    bool ChannelAutoBan { get; }



    /// <summary>
    /// Автоматически банить пользователей с похожими именами
    /// </summary>
    bool LookAlikeAutoBan { get; }

    /// <summary>
    /// Автоматически банить по кнопкам
    /// </summary>
    bool ButtonAutoBan { get; }

    /// <summary>
    /// Автоматически банить при высокой уверенности
    /// </summary>
    bool HighConfidenceAutoBan { get; }

    /// <summary>
    /// Пересылать сообщения с низкой уверенностью в ham
    /// </summary>
    bool LowConfidenceHamForward { get; }

    /// <summary>
    /// Включить кнопку одобрения
    /// </summary>
    bool ApproveButtonEnabled { get; }

    /// <summary>
    /// Удаление пересланных сообщений от новичков
    /// </summary>
    bool DeleteForwardedMessages { get; }

    /// <summary>
    /// Отключить приветственные сообщения
    /// </summary>
    bool DisableWelcome { get; }

    /// <summary>
    /// Отключить фильтрацию картинок/видео/документов глобально
    /// </summary>
    bool DisableMediaFiltering { get; }

    /// <summary>
    /// Режим автоодобрения пользователей (true = глобальный, false = групповой)
    /// </summary>
    bool GlobalApprovalMode { get; }

    /// <summary>
    /// Группы где фильтрация медиа отключена
    /// </summary>
    HashSet<long> MediaFilteringDisabledChats { get; }

    /// <summary>
    /// Проверяет, отключена ли фильтрация медиа для данного чата
    /// </summary>
    bool IsMediaFilteringDisabledForChat(long chatId);

    // === КОНФИГУРАЦИЯ ЭФФЕКТОВ МОДЕРАЦИИ ===

    /// <summary>
    /// Конфигурация эффектов модерации
    /// </summary>
    EffectsConfiguration Effects { get; }

    // === ТЕСТОВЫЕ / GOLDEN НАСТРОЙКИ ===

    /// <summary>
    /// Включен ли golden baseline режим (ускоряет определенные фоновые процессы и подменяет TelegramBotClient).
    /// </summary>
    bool GoldenBaselineMode { get; }

    /// <summary>
    /// Тестовый блэклист пользователей (используется только в тестовых сценариях или golden baseline режиме).
    /// Пустой если переменная окружения не задана.
    /// </summary>
    HashSet<long> TestBlacklistUserIds { get; }
}