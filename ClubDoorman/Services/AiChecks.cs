using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Caching;
using System.Net.Http;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

public class AiChecks : IAiChecks
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<AiChecks> _logger;
    private readonly IAppConfig _appConfig;
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
    
    // Retry policy для обработки временных ошибок API
    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .AddTimeout(TimeSpan.FromSeconds(30)) // Таймаут 30 секунд на HTTP запросы
        .Build();
    
    const string Model = "google/gemini-2.5-flash";
    
    public AiChecks(ITelegramBotClientWrapper bot, ILogger<AiChecks> logger, IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _appConfig = appConfig;
        _api = _appConfig.OpenRouterApi == null ? null : CustomProviders.OpenRouter(_appConfig.OpenRouterApi);
        
        if (_api == null)
        {
            _logger.LogWarning("🤖 AI анализ ОТКЛЮЧЕН: DOORMAN_OPENROUTER_API не настроен или равен 'test-api-key'");
        }
        else
        {
            _logger.LogInformation("🤖 AI анализ ВКЛЮЧЕН: OpenRouter API настроен");
        }
    }

    private static string CacheKey(long userId) => $"ai_profile_check:{userId}";

    /// <summary>
    /// Проверяет, является ли сообщение банальным приветствием
    /// </summary>
    public static bool IsBoringGreeting(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return false;
            
        var text = messageText.Trim().ToLowerInvariant();
        
        var boringGreetings = new[]
        {
            "привет", "hi", "hello", "здравствуйте", "добрый день", "добрый вечер",
            "доброе утро", "hola", "salut", "ciao", "hey", "qq", ".", "йо", "yo",
            "здарова", "приветик", "хай", "хелло", "хеллоу", "дарова", "здоров",
            "здравствуй", "приветствую", "салют", "хело", "hell", "хи", "q",
            "добро пожаловать", "good morning", "good evening", "good day",
            "доброго дня", "доброго вечера", "доброго утра", "всем привет",
            "всех приветствую", "всем хай", "всем здравствуйте"
        };
        
        // Сначала проверяем точное совпадение (включая пунктуацию)
        if (boringGreetings.Contains(text))
            return true;
        
        // Потом убираем знаки препинания и проверяем снова
        var textWithoutPunctuation = text.TrimEnd('.', '!', '?', ',');
        return boringGreetings.Contains(textWithoutPunctuation);
    }

    /// <summary>
    /// Отмечает пользователя как проверенного и безопасного
    /// </summary>
    public void MarkUserOkay(long userId)
    {
        var cacheItem = new CacheItem(CacheKey(userId), new SpamPhotoBio(new SpamProbability(), [], ""));
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(30) };
        MemoryCache.Default.Set(cacheItem, policy);
        _logger.LogInformation("Пользователь {UserId} отмечен как безопасный", userId);
    }

    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама
    /// </summary>
    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(Telegram.Bot.Types.User user, Func<string, Task>? ifChanged = default)
    {
        return await GetAttentionBaitProbability(user, null, ifChanged);
    }
    
    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама (с учетом первого сообщения)
    /// </summary>
    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(Telegram.Bot.Types.User user, string? messageText, Func<string, Task>? ifChanged = default)
    {
        if (user == null)
        {
            _logger.LogDebug("Пользователь null, возвращаем пустой результат");
            return new SpamPhotoBio(new SpamProbability(), [], "");
        }

        if (_api == null)
        {
            _logger.LogDebug("OpenAI API не настроен, пропускаем AI проверку профиля");
            return new SpamPhotoBio(new SpamProbability(), [], "");
        }

        var cached = MemoryCache.Default.Get(CacheKey(user.Id)) as SpamPhotoBio;
        if (cached != null)
        {
            _logger.LogDebug("🤖 AI анализ профиля: найден кэш для пользователя {UserId}: вероятность={Probability}, фото={PhotoSize} байт", 
                user.Id, cached.SpamProbability.Probability, cached.Photo.Length);
            return cached;
        }
        
        _logger.LogDebug("🤖 AI анализ профиля: кэш не найден для пользователя {UserId}, выполняем анализ", user.Id);

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();
        var nameBioUser = string.Empty;

        try
        {
            _logger.LogDebug("🤖 AI анализ профиля: получаем GetChatFullInfo для пользователя {UserId}", user.Id);
            var userChat = await _bot.GetChatFullInfo(user.Id);
            _logger.LogDebug("🤖 AI анализ профиля: GetChatFullInfo получен для пользователя {UserId}, Bio: {Bio}, LinkedChatId: {LinkedChatId}, Photo: {Photo}", 
                user.Id, userChat.Bio ?? "null", userChat.LinkedChatId?.ToString() ?? "null", userChat.Photo?.ToString() ?? "null");
            
            // ФИКС: Если у пользователя нет био и нет связанного канала, но есть первое сообщение - анализируем фото + сообщение
            if (userChat.Bio == null && userChat.LinkedChatId == null)
            {
                _logger.LogDebug("У пользователя {UserId} нет био и связанного канала", user.Id);
                if (userChat.Photo != null)
                {
                    // Если есть первое сообщение - делаем полный анализ фото + сообщение
                    if (!string.IsNullOrWhiteSpace(messageText))
                    {
                        _logger.LogDebug("🤖 AI анализ профиля: есть фото и первое сообщение, делаем полный анализ для пользователя {UserId}", user.Id);
                        // Продолжаем с полным анализом ниже (не return)
                    }
                    else
                    {
                        // Только фото, без сообщения - используем старую логику
                        return await GetEroticPhotoBaitProbability(user, userChat);
                    }
                }
                else
                {
                    // Нет ни био, ни фото - считаем безопасным
                    var result = new SpamPhotoBio(new SpamProbability(), [], "");
                    CacheResult(user.Id, result);
                    return result;
                }
            }

            _logger.LogDebug("Анализируем профиль пользователя {UserId} с помощью AI", user.Id);
            
            var sb = new StringBuilder();
            sb.Append($"Имя: {Utils.FullName(user)}");
            if (user.Username != null)
                sb.Append($"\nЮзернейм: @{user.Username}");
            if (userChat.Bio != null)
                sb.Append($"\nОписание: {userChat.Bio}");

            nameBioUser = sb.ToString();
            byte[]? photoBytes = null;
            ChatCompletionRequestUserMessage? photoMessage = null;

            // Загружаем фото профиля если есть
            _logger.LogDebug("🤖 AI анализ профиля: проверяем userChat.Photo для пользователя {UserId}, Photo: {Photo}", 
                user.Id, userChat.Photo?.ToString() ?? "null");
                
            if (userChat.Photo != null)
            {
                _logger.LogDebug("🤖 AI анализ профиля: загружаем фото для пользователя {UserId}, FileId: {FileId}", 
                    user.Id, userChat.Photo.BigFileId);
                    
                try
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                    photoBytes = ms.ToArray();
                    pic = photoBytes;
                    photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
                    _logger.LogDebug("🔍 PHOTO MESSAGE: Content создан, тип={Type}", photoMessage.Content.GetType().Name);
                    sb.Append($"\nФото: прикреплено");
                    
                    _logger.LogDebug("🤖 AI анализ профиля: фото загружено для пользователя {UserId}, размер: {Size} байт", 
                        user.Id, photoBytes.Length);
                    _logger.LogDebug("🔍 ФОТО: {Size} байт, fileId={FileId}", photoBytes.Length, userChat.Photo.BigFileId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "🤖 AI анализ профиля: не удалось загрузить фото для пользователя {UserId}", user.Id);
                }
            }
            else
            {
                _logger.LogDebug("🤖 AI анализ профиля: у пользователя {UserId} нет фото профиля", user.Id);
            }

            var prompt = $"""
                Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. 
                Отвечай вероятностью от 0 до 1. 
                
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения), без воды!
                ОБЯЗАТЕЛЬНО анализируй фото профиля если оно есть!
                ОБЯЗАТЕЛЬНО учитывай первое сообщение пользователя если оно предоставлено!
                
                Особенно внимательно учитывай признаки:
                • сексуализированные профили (эмодзи с двойным смыслом - 💦, 💋, 👄, 🍑, 🍆, 🍒, 🍓, 🍌 и прочих в имени, любой намёк на эротику и порно, голые фото)
                • подозрительные фото: слишком привлекательные, профессиональные, эротические
                • упоминания о курсах, заработке, трейдинге, арбитраже, привлечению трафика
                • ссылки на OnlyFans, соцсети
                • род занятий указан прямо в имени (например: HR, SMM, недвижимость, маркетинг)
                • спам-сообщения: реклама услуг, навязчивое предложение товаров/услуг, массовая рассылка
                • ⚠️ КРИТИЧЕСКИ ВАЖНО: спамеры пишут сообщения БЕЗ КОНКРЕТНОГО НАМЕРЕНИЯ - просто "привет", "как дела", "хай". Это проверка группы перед рекламой!
                • ПРАВИЛО: минимальный профиль + сообщение БЕЗ конкретного намерения = высокая вероятность спама (0.7-0.9)
                • ПРАВИЛО: подозрительное bio + сообщение БЕЗ конкретного намерения = высокая вероятность спама (0.7-0.9)
                • ПРАВИЛО: привлекательное фото + сообщение БЕЗ конкретного намерения = очень высокая вероятность спама (0.8-0.95)
                • АНАЛИЗИРУЙ НАМЕРЕНИЕ: есть ли конкретная цель/вопрос/просьба в сообщении?
                • ПРИМЕРЫ БЕЗ намерения (ПОДОЗРИТЕЛЬНО): "привет", "как дела", "хай", "всем привет" - и подобное
                • ПРИМЕРЫ С намерением (НОРМАЛЬНО): "привет, пришел по совету друга", "Привет. Хочу узнать о проекте", "Привет, есть вопрос по теме"
                
                Вот данные профиля:
                {nameBioUser}
                {(string.IsNullOrWhiteSpace(messageText) ? "" : $"\n\nПервое сообщение пользователя:\n{messageText}")}
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — опытный антиспам модератор. КЛЮЧЕВОЕ: анализируй НАМЕРЕНИЕ в сообщении! Спамеры пишут без конкретной цели ('привет', 'как дела', 'хай') для проверки группы. Обычные люди имеют конкретное намерение ('подскажите про...', 'хочу узнать...', 'есть вопрос...'). Если нет четкого намерения + подозрительный профиль = вероятный спамер. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage(),
            };
            
            if (photoMessage != null)
                messages.Add(photoMessage);

            // Анализируем связанный канал если есть
            if (userChat.LinkedChatId != null)
            {
                try
                {
                    var linkedChat = await _bot.GetChatFullInfo(userChat.LinkedChatId.Value);
                    var info = new StringBuilder();
                    info.Append($"Информация о привязанном канале:\nНазвание: {linkedChat.Title}");
                    if (linkedChat.Username != null)
                        info.Append($"\nЮзернейм: @{linkedChat.Username}");
                    if (linkedChat.Description != null)
                        info.Append($"\nОписание: {linkedChat.Description}");
                    
                    messages.Add(info.ToString().ToUserMessage());
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось получить информацию о связанном канале {ChannelId}", userChat.LinkedChatId);
                }
            }

            _logger.LogDebug("🤖 AI анализ профиля: отправляем запрос в API для пользователя {UserId}, количество сообщений: {MessagesCount}", 
                user.Id, messages.Count);
            _logger.LogDebug("🔍 ОТПРАВКА В AI: messages.Count={Count}, photoMessage={PhotoMessage}", 
                messages.Count, photoMessage != null ? "ЕСТЬ" : "НЕТ");
                
            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ профиля пользователя {UserId}: {Probability} - {Reason}", 
                    user.Id, probability.Probability, probability.Reason);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
        }

        var finalResult = new SpamPhotoBio(probability, pic, nameBioUser);
        CacheResult(user.Id, finalResult);
        return finalResult;
    }

    /// <summary>
    /// Анализирует фото профиля на предмет сексуализированного контента
    /// </summary>
    private async ValueTask<SpamPhotoBio> GetEroticPhotoBaitProbability(Telegram.Bot.Types.User user, ChatFullInfo userChat)
    {
        if (_api == null)
            return new SpamPhotoBio(new SpamProbability(), [], "");

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();

        try
        {
            _logger.LogDebug("🤖 AI анализ профиля: GetEroticPhotoBaitProbability для пользователя {UserId}, Photo: {Photo}", 
                user.Id, userChat.Photo?.ToString() ?? "null");
                
            var photo = userChat.Photo!;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
            var photoBytes = ms.ToArray();
            pic = photoBytes;
            
            _logger.LogDebug("🤖 AI анализ профиля: фото загружено в GetEroticPhotoBaitProbability для пользователя {UserId}, размер: {Size} байт", 
                user.Id, photoBytes.Length);
            
            var photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
            var prompt = "Проанализируй, выглядит ли эта аватарка пользователя сексуализированно или развратно. Отвечай вероятностью от 0 до 1.";
            
            var messages = new List<ChatCompletionRequestMessage> 
            { 
                prompt.ToUserMessage(), 
                photoMessage 
            };
            
            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );
            
            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ фото профиля пользователя {UserId}: {Probability}", 
                    user.Id, probability.Probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при анализе фото профиля пользователя {UserId}", user.Id);
        }

        return new SpamPhotoBio(probability, pic, Utils.FullName(user));
    }

    /// <summary>
    /// Анализирует сообщение на предмет спама с помощью AI
    /// </summary>
    /// <param name="message">Сообщение для анализа</param>
    /// <returns>Вероятность того, что сообщение является спамом</returns>
    /// <exception cref="AiServiceException">Выбрасывается при критических ошибках AI сервиса</exception>
    /// <exception cref="ArgumentNullException">Выбрасывается если message равен null</exception>
    public async ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message), "Сообщение не может быть null");

        if (_api == null)
        {
            _logger.LogDebug("AI API недоступен, возвращаем пустой результат");
            return new SpamProbability();
        }

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Текст сообщения пустой, пропускаем AI анализ");
                return new SpamProbability();
            }

            var prompt = $"""
                Проанализируй это сообщение на предмет спама. Отвечай вероятностью от 0 до 1.
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения)!
                
                Признаки спама:
                • Реклама товаров/услуг
                • Просьбы о переходах по ссылкам
                • Навязчивые предложения
                • Массовые рассылки
                • Мошенничество
                
                Сообщение: {text}
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — антиспам модератор. Анализируй сообщения на предмет спама. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage()
            };

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                _logger.LogDebug("AI анализ сообщения: {Probability} - {Reason}", 
                    response.Value1.Probability, response.Value1.Reason);
                return response.Value1;
            }
            else
            {
                _logger.LogWarning("Получен пустой ответ от AI API для анализа сообщения");
                return new SpamProbability();
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Таймаут при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ошибка сети при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ошибка парсинга JSON при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при AI анализе сообщения");
            return new SpamProbability();
        }
    }
    
    /// <summary>
    /// Специальный анализ сообщений от подозрительных пользователей с расширенным контекстом
    /// </summary>
    public async ValueTask<SpamProbability> GetSuspiciousUserSpamProbability(
        Message message, 
        Telegram.Bot.Types.User user, 
        List<string> firstMessages, 
        double mimicryScore)
    {
        if (_api == null)
            return new SpamProbability();

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return new SpamProbability();

            var userName = Utils.FullName(user);
            var username = user.Username != null ? $"@{user.Username}" : "нет";
            var firstMessagesText = string.Join("', '", firstMessages.Take(5));
            
            // Получаем расширенную информацию о пользователе как в основном методе
            var userChat = await _bot.GetChatFullInfo(user.Id);
            var bioInfo = !string.IsNullOrEmpty(userChat.Bio) ? $"\n• Биография: {userChat.Bio}" : "";
            var photoInfo = userChat.Photo != null ? "\n• Есть фото профиля" : "\n• Нет фото профиля";

            var prompt = $"""
                АНАЛИЗ ПОДОЗРИТЕЛЬНОГО ПОЛЬЗОВАТЕЛЯ НА СПАМ
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения)!
                
                КРИТИЧЕСКИЙ КОНТЕКСТ:
                • Пользователь уже помечен как ПОДОЗРИТЕЛЬНЫЙ
                • Его первые сообщения показали признаки мимикрии (скор: {mimicryScore:F2})
                • Это реальный анализ с последствиями (удаление/бан)
                
                ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
                • Имя: {userName}
                • Username: {username}{bioInfo}{photoInfo}
                • Первые сообщения: ['{firstMessagesText}']
                • Скор мимикрии: {mimicryScore:F2} (выше 0.7 = подозрительно)
                
                ТЕКУЩЕЕ СООБЩЕНИЕ: "{text}"
                
                ОСОБОЕ ВНИМАНИЕ К:
                • Предложения займов/денег (особенно после шаблонных приветствий)
                • Финансовые услуги от новых пользователей
                • Переход от невинных сообщений к коммерческим предложениям
                • Типичные схемы мошенников: "Могу дать в долг", "Помогу с деньгами"
                • Несоответствие между первыми сообщениями и текущим
                • Подозрительные фото профиля (слишком привлекательные, эротические)
                • Биография с упоминанием финансов, заработка, услуг
                
                УЧИТЫВАЙ, ЧТО:
                - Высокий скор мимикрии + финансовые предложения = очень вероятный спам
                - Обычные пользователи не предлагают займы незнакомцам
                - Шаблонные приветствия + деньги = классическая схема
                
                Оцени вероятность спама от 0 до 1, учитывая ВСЕ факторы.
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — специализированный антиспам эксперт для анализа подозрительных пользователей. Твоя задача — выявлять мошенников-хамелеонов, которые маскируются под обычных пользователей. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage()
            };

            // Добавляем фото профиля если есть (как в основном методе)
            if (userChat.Photo != null)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                    var photoBytes = ms.ToArray();
                    var photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
                    messages.Add(photoMessage);
                    _logger.LogDebug("🔍 Добавлено фото профиля для анализа подозрительного пользователя {User}", userName);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Не удалось загрузить фото профиля для {User}: {Error}", userName, ex.Message);
                }
            }

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                _logger.LogDebug("🔍 Специальный AI анализ подозрительного пользователя: {Probability} - {Reason}", 
                    response.Value1.Probability, response.Value1.Reason);
                return response.Value1;
            }

            return new SpamProbability();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при специальном AI анализе подозрительного пользователя");
            return new SpamProbability();
        }
    }



    private void CacheResult(long userId, SpamPhotoBio result)
    {
        _logger.LogDebug("🤖 AI анализ профиля: кэшируем результат для пользователя {UserId}: вероятность={Probability}, фото={PhotoSize} байт", 
            userId, result.SpamProbability.Probability, result.Photo.Length);
        var cacheItem = new CacheItem(CacheKey(userId), result);
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(24) };
        MemoryCache.Default.Set(cacheItem, policy);
    }
}

// Модели данных для AI ответов
public class SpamProbability
{
    public double Probability { get; set; }
    public string Reason { get; set; } = "";
}

public sealed record SpamPhotoBio(SpamProbability SpamProbability, byte[] Photo, string NameBio); 