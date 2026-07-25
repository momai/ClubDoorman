using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.TextProcessing;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Features.Moderation;

public sealed class ContentModerationPolicy : IContentModerationPolicy
{
    private readonly ISpamHamClassifier _classifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<ContentModerationPolicy> _logger;

    public ContentModerationPolicy(
        ISpamHamClassifier classifier,
        IBadMessageManager badMessageManager,
        IAppConfig appConfig,
        ILogger<ContentModerationPolicy> logger)
    {
        _classifier = classifier;
        _badMessageManager = badMessageManager;
        _appConfig = appConfig;
        _logger = logger;
    }

    public async Task<ModerationResult> CheckContentAsync(
        ContentModerationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var message = input.Message;
        var text = input.Text;

        if (message.ReplyMarkup != null)
            return new ModerationResult(ModerationAction.Ban, "Сообщение с кнопками");

        if (message.Story != null)
            return new ModerationResult(ModerationAction.Delete, "Сторис");

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            return CheckMediaContent(input) ??
                   new ModerationResult(ModerationAction.Report, "Медиа без подписи");
        }

        var isKnownBad = _badMessageManager.KnownBadMessage(text);
        _logger.LogDebug(
            "Проверка известных плохих сообщений: текст='{Text}', известное={IsKnownBad}",
            text.Length > 50 ? text[..50] + "..." : text,
            isKnownBad);

        if (isKnownBad)
        {
            _logger.LogInformation("Найдено известное спам-сообщение: '{Text}'", text);
            return new ModerationResult(ModerationAction.Ban, "Известное спам-сообщение");
        }

        return await CheckTextContentAsync(input, text, cancellationToken);
    }

    private ModerationResult? CheckMediaContent(ContentModerationInput input)
    {
        var message = input.Message;
        var chatId = input.DestinationChat.Id;
        var hasPhotoOrVideo = message.Photo != null || message.Video != null;
        var hasStickerOrDocument = message.Sticker != null || message.Document != null;
        var isAnnouncement = ChatSettingsManager.GetChatType(chatId) == "announcement";

        if (_appConfig.IsMediaFilteringDisabledForChat(chatId) && hasPhotoOrVideo && !hasStickerOrDocument)
            return null;

        if (!isAnnouncement && hasStickerOrDocument)
            return new ModerationResult(ModerationAction.Delete, "В первых трёх сообщениях нельзя отправлять стикеры или документы");

        if (!_appConfig.IsMediaFilteringDisabledForChat(chatId) && !isAnnouncement && hasPhotoOrVideo)
            return new ModerationResult(ModerationAction.Delete, "В первых трёх сообщениях нельзя отправлять картинки или видео");

        return null;
    }

    private async Task<ModerationResult> CheckTextContentAsync(
        ContentModerationInput input,
        string text,
        CancellationToken cancellationToken)
    {
        var isAnnouncement = ChatSettingsManager.GetChatType(input.DestinationChat.Id) == "announcement";

        if (_appConfig.TextMentionFilterEnabled)
        {
            var hasLinks = SimpleFilters.HasLinks(text);
            _logger.LogDebug(
                "Проверка ссылок: текст='{Text}', найдены={HasLinks}",
                text.Length > 50 ? text[..50] + "..." : text,
                hasLinks);

            if (hasLinks)
            {
                _logger.LogInformation("Найдены ссылки в тексте: '{Text}'", text);
                return new ModerationResult(ModerationAction.Delete, "Ссылки запрещены");
            }

            if (input.Message.Entities?.Any(entity =>
                    entity.Type is MessageEntityType.Url or MessageEntityType.TextLink) == true)
            {
                _logger.LogInformation("Найдены URL-сущности или TextLink в сообщении");
                return new ModerationResult(ModerationAction.Delete, "Ссылки запрещены");
            }
        }

        var tooManyEmojis = SimpleFilters.TooManyEmojis(text);
        _logger.LogDebug(
            "Проверка эмодзи: текст='{Text}', многовато={TooMany}, объявление={IsAnnouncement}",
            text.Length > 50 ? text[..50] + "..." : text,
            tooManyEmojis,
            isAnnouncement);

        if (!isAnnouncement && tooManyEmojis)
        {
            _logger.LogInformation("Слишком много эмодзи в тексте: '{Text}'", text);
            return new ModerationResult(ModerationAction.Delete, "В этом сообщении многовато эмоджи");
        }

        var normalized = TextProcessor.NormalizeText(text);
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            return new ModerationResult(
                _appConfig.LookAlikeAutoBan ? ModerationAction.Ban : ModerationAction.Delete,
                reason);
        }

        var hasStopWords = SimpleFilters.HasStopWords(normalized);
        _logger.LogDebug("Проверка стоп-слов: текст='{Text}', найдены={HasStopWords}", normalized, hasStopWords);
        if (hasStopWords)
        {
            _logger.LogInformation("Найдены стоп-слова в тексте: '{Text}'", normalized);
            return new ModerationResult(ModerationAction.Delete, "В этом сообщении есть стоп-слова");
        }

        var isBoringGreeting = SimpleFilters.IsBoringGreeting(text);
        _logger.LogDebug(
            "Проверка банальных приветствий: текст='{Text}', банальное={IsBoringGreeting}",
            text.Length > 50 ? text[..50] + "..." : text,
            isBoringGreeting);
        if (isBoringGreeting)
        {
            _logger.LogInformation("Обнаружено банальное приветствие: '{Text}'", text);
            return new ModerationResult(ModerationAction.Delete, "Банальное приветствие");
        }

        var (spam, score) = await _classifier
            .IsSpam(normalized)
            .WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        _logger.LogDebug("ML анализ: текст='{Text}', спам={Spam}, скор={Score}", normalized, spam, score);

        if (spam)
        {
            _logger.LogInformation("ML классификатор определил спам: '{Text}', скор={Score}", normalized, score);
            return new ModerationResult(ModerationAction.Delete, $"ML решил что это спам, скор {score}", score);
        }

        if (score > -0.6 && _appConfig.LowConfidenceHamForward)
        {
            return new ModerationResult(
                ModerationAction.RequireAiAnalysis,
                $"ML не уверен (скор {score}) - требуется AI анализ",
                score);
        }

        return new ModerationResult(ModerationAction.Allow, "Сообщение прошло все проверки", score);
    }
}
