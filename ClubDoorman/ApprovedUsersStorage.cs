using System.Collections.Concurrent;
using System.Text.Json;

namespace ClubDoorman;

public class ApprovedUsersStorage
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<long, byte> _approvedUsers;
    private readonly ILogger<ApprovedUsersStorage> _logger;

    public ApprovedUsersStorage(ILogger<ApprovedUsersStorage> logger)
    {
        _logger = logger;
        _filePath = Path.Combine("data", "approved_users.json");
        _approvedUsers = LoadFromFile();
    }

    private ConcurrentDictionary<long, byte> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new ConcurrentDictionary<long, byte>();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new ConcurrentDictionary<long, byte>();

            try
            {
                // Пробуем новый формат (список)
                var list = JsonSerializer.Deserialize<List<long>>(json);
                if (list != null)
                {
                    return new ConcurrentDictionary<long, byte>(
                        list.ToDictionary(x => x, _ => (byte)1)
                    );
                }
            }
            catch (JsonException)
            {
                // Если не получилось, пробуем старый формат (словарь с DateTime)
                try
                {
                    var oldFormat = JsonSerializer.Deserialize<Dictionary<long, DateTime>>(json);
                    if (oldFormat != null)
                    {
                        _logger.LogInformation("Успешно загружен файл в старом формате, будет сконвертирован при следующем сохранении");
                        return new ConcurrentDictionary<long, byte>(
                            oldFormat.Keys.ToDictionary(x => x, _ => (byte)1)
                        );
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Не удалось прочитать файл ни в новом, ни в старом формате");
                }
            }

            return new ConcurrentDictionary<long, byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка одобренных пользователей");
            return new ConcurrentDictionary<long, byte>();
        }
    }

    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Создаем временный файл
            var tempPath = Path.GetTempFileName();
            var json = JsonSerializer.Serialize(_approvedUsers.Keys.ToList());
            
            // Сначала записываем во временный файл
            File.WriteAllText(tempPath, json);
            
            // Затем атомарно заменяем целевой файл
            File.Move(tempPath, _filePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении списка одобренных пользователей");
        }
    }

    public bool IsApproved(long userId)
    {
        return _approvedUsers.ContainsKey(userId);
    }

    public void ApproveUser(long userId)
    {
        try
        {
            if (_approvedUsers.TryAdd(userId, 1))
            {
                _logger.LogInformation("Пользователь {UserId} добавлен в список одобренных", userId);
                SaveToFile();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении пользователя {UserId} в список одобренных", userId);
            throw;
        }
    }

    public bool RemoveApproval(long userId)
    {
        try
        {
            if (_approvedUsers.TryRemove(userId, out _))
            {
                _logger.LogInformation("Пользователь {UserId} удален из списка одобренных", userId);
                SaveToFile();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из списка одобренных", userId);
            throw;
        }
    }
} 