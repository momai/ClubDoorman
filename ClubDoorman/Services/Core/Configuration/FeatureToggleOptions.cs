namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Опции для включения/отключения функций
/// </summary>
public class FeatureToggleOptions
{
    /// <summary>
    /// Пересылать сообщения с низкой уверенностью в ham
    /// </summary>
    public bool LowConfidenceHamForward { get; set; } = false;

    /// <summary>
    /// Включить кнопку одобрения
    /// </summary>
    public bool ApproveButtonEnabled { get; set; } = false;

    /// <summary>
    /// Удаление пересланных сообщений от новичков
    /// </summary>
    public bool DeleteForwardedMessages { get; set; } = false;

    /// <summary>
    /// Включить фильтр ссылок
    /// </summary>
    public bool TextMentionFilterEnabled { get; set; } = false;

    /// <summary>
    /// Отключить приветственные сообщения
    /// </summary>
    public bool DisableWelcome { get; set; } = false;

    /// <summary>
    /// Отключить фильтрацию картинок/видео/документов глобально
    /// </summary>
    public bool DisableMediaFiltering { get; set; } = false;

    /// <summary>
    /// Режим автоодобрения пользователей:
    /// - true: глобальный режим (3 сообщения в любых группах → одобрение во всех группах)
    /// - false: групповой режим (3 сообщения в каждой группе → одобрение только в этой группе)
    /// </summary>
    public bool GlobalApprovalMode { get; set; } = true;

    /// <summary>
    /// Отправлять уведомления о банах за повторные нарушения в админ-чат вместо лог-чата
    /// </summary>
    public bool RepeatedViolationsBanToAdminChat { get; set; } = false;

    /// <summary>
    /// Отключить уведомления о банах за вход через папки в лог-чате
    /// </summary>
    public bool BanFolderInviteNotificationsDisable { get; set; } = false;
}