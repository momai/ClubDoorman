using System.Runtime.Caching;
using System.Security.Cryptography;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services.Messaging;

public sealed class AdminActionStore : IAdminActionStore, IDisposable
{
    private const string ProfileReviewKeyPrefix = "profile-review:";
    private readonly MemoryCache _cache = new(nameof(AdminActionStore));

    private sealed record ProfileReviewEntry(AiProfileAnalysisData Data, DateTimeOffset ExpiresAt);

    public string PutProfileReview(AiProfileAnalysisData data, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), ttl, "TTL must be positive.");

        while (true)
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
                .Replace('+', '-')
                .Replace('/', '_');
            var added = _cache.Add(
                ProfileReviewKeyPrefix + token,
                new ProfileReviewEntry(data, expiresAt),
                new CacheItemPolicy { AbsoluteExpiration = expiresAt });

            if (added)
                return token;
        }
    }

    public AiProfileAnalysisData? TakeProfileReview(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var entry = _cache.Remove(ProfileReviewKeyPrefix + token) as ProfileReviewEntry;
        return entry?.ExpiresAt > DateTimeOffset.UtcNow ? entry.Data : null;
    }

    public void Dispose() => _cache.Dispose();
}
