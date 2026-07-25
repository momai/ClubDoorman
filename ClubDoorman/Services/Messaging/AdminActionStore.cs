using System.Runtime.Caching;
using System.Security.Cryptography;

namespace ClubDoorman.Services.Messaging;

public sealed class AdminActionStore : IAdminActionStore, IDisposable
{
    private const string ProfileReviewKeyPrefix = "profile-review:";
    private readonly MemoryCache _cache = new(nameof(AdminActionStore));

    private sealed record ProfileReviewEntry(ProfileReviewActionState State, DateTimeOffset ExpiresAt);

    public string PutProfileReview(ProfileReviewActionState state, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(state);
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
                new ProfileReviewEntry(state, expiresAt),
                new CacheItemPolicy { AbsoluteExpiration = expiresAt });

            if (added)
                return token;
        }
    }

    public ProfileReviewActionState? TakeProfileReview(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var entry = _cache.Remove(ProfileReviewKeyPrefix + token) as ProfileReviewEntry;
        return entry?.ExpiresAt > DateTimeOffset.UtcNow ? entry.State : null;
    }

    public void DiscardProfileReview(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            _cache.Remove(ProfileReviewKeyPrefix + token);
    }

    public void Dispose() => _cache.Dispose();
}
