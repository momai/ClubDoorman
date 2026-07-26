using System.Text.Json;
using System.Text;
using Telegram.Bot;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services.Statistics;

public class GlobalStatsManager
{
    private readonly string _baseDir;
    private readonly string _jsonPath;
    private readonly string _htmlPath;
    private readonly string _globalJsonPath;
    private readonly object _lock = new();
    private StatsRoot _stats;

    public GlobalStatsManager()
    {
        // If DOORMAN_DATA_ROOT is set (e.g. golden baseline harness), write stats inside that sandbox instead of repo-level /data.
        var envRoot = Environment.GetEnvironmentVariable("DOORMAN_DATA_ROOT");
        _baseDir = string.IsNullOrWhiteSpace(envRoot) ? "data" : Path.Combine(envRoot!, "stats-data");
        Directory.CreateDirectory(_baseDir);
        _jsonPath = Path.Combine(_baseDir, "global_stats.json");
        _htmlPath = Path.Combine(_baseDir, "stats.html");
        _globalJsonPath = Path.Combine(_baseDir, "stats_global.json");
        _stats = Load();
    }

    // Гарантирует, что чат есть в статистике, и обновляет title
    public async Task EnsureChatAsync(long chatId, string chatTitle, ITelegramBotClientWrapper bot)
    {
        bool isNew = false;
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
            {
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
                isNew = true;
            }
            else if (!string.IsNullOrWhiteSpace(chatTitle))
            {
                _stats.Chats[chatId].Title = chatTitle;
            }
            Save();
        }
        if (isNew)
        {
            try
            {
                var count = await bot.GetChatMemberCount(chatId);
                lock (_lock)
                {
                    _stats.Chats[chatId].Members = count;
                    Save();
                }
            }
            catch { }
        }
    }

    // Обновляет количество участников для всех чатов
    public async Task UpdateAllMembersAsync(ITelegramBotClientWrapper bot)
    {
        List<long> chatIds;
        lock (_lock)
        {
            chatIds = _stats.Chats.Keys.ToList();
        }
        foreach (var chatId in chatIds)
        {
            try
            {
                var count = await bot.GetChatMemberCount(chatId);
                lock (_lock)
                {
                    if (_stats.Chats.TryGetValue(chatId, out var chat))
                        chat.Members = count;
                }
            }
            catch { }
        }
        lock (_lock) { Save(); }
    }

    // Обновляет количество участников только в чатах, где сейчас 0 участников
    public async Task UpdateZeroMemberChatsAsync(ITelegramBotClientWrapper bot)
    {
        List<long> zeroChats;
        lock (_lock)
        {
            zeroChats = _stats.Chats.Where(x => x.Value.Members == 0).Select(x => x.Key).ToList();
        }
        foreach (var chatId in zeroChats)
        {
            try
            {
                var count = await bot.GetChatMemberCount(chatId);
                lock (_lock)
                {
                    if (_stats.Chats.TryGetValue(chatId, out var chat))
                        chat.Members = count;
                    Save();
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    // Инкрементирует показ капчи
    public void IncCaptcha(long chatId, string chatTitle = "")
    {
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
            _stats.Chats[chatId].CaptchaShown++;
            Save();
        }
    }

    // Инкрементирует бан
    public void IncBan(long chatId, string chatTitle = "")
    {
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
            _stats.Chats[chatId].Bans++;
            Save();
        }
    }

    public void GenerateHtml()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset=\"UTF-8\"><title>Статистика ClubDoorman</title></head><body>");
            sb.AppendLine("<h1>Статистика ClubDoorman</h1>");
            sb.AppendLine($"<b>Обслуживаемых чатов: {_stats.Chats.Count}</b><br><br>");
            sb.AppendLine("<table border=1 cellpadding=4><tr><th>Группа</th><th>Участников</th><th>Показано капч</th><th>Банов</th></tr>");
            foreach (var chat in _stats.Chats.Values.OrderByDescending(c => c.Members))
            {
                sb.AppendLine($"<tr><td>{System.Web.HttpUtility.HtmlEncode(chat.Title)}</td><td>{chat.Members}</td><td>{chat.CaptchaShown}</td><td>{chat.Bans}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(_htmlPath, sb.ToString());
        }
    }

    public void GenerateGlobalJson()
    {
        lock (_lock)
        {
            var totalMembers = _stats.Chats.Values.Sum(c => c.Members);
            var totalBans = _stats.Chats.Values.Sum(c => c.Bans);
            var totalCaptcha = _stats.Chats.Values.Sum(c => c.CaptchaShown);
            var totalChats = _stats.Chats.Count;
            var global = new
            {
                start_date = _stats.StartDate,
                total_members = totalMembers,
                total_bans = totalBans,
                total_captcha_shown = totalCaptcha,
                total_chats = totalChats
            };
            File.WriteAllText(_globalJsonPath, JsonSerializer.Serialize(global, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private StatsRoot Load()
    {
        try
        {
            if (File.Exists(_jsonPath))
            {
                var json = File.ReadAllText(_jsonPath);
                var stats = JsonSerializer.Deserialize<StatsRoot>(json);
                if (stats != null)
                {
                    if (string.IsNullOrWhiteSpace(stats.StartDate))
                        stats.StartDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    return stats;
                }
            }
        }
        catch { }
        var s = new StatsRoot();
        s.StartDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return s;
    }

    private void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_jsonPath, JsonSerializer.Serialize(_stats, options));
        GenerateHtml();
        GenerateGlobalJson();
    }
}

public class StatsRoot
{
    public string StartDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public Dictionary<long, ChatStat> Chats { get; set; } = new();
}

public class ChatStat
{
    public string Title { get; set; } = string.Empty;
    public int Members { get; set; } = 0;
    public int CaptchaShown { get; set; } = 0;
    public int Bans { get; set; } = 0;
}
