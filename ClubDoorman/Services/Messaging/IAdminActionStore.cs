namespace ClubDoorman.Services.Messaging;

public sealed record ProfileReviewActionState(long ChatId, long UserId, long? MessageId);

public interface IAdminActionStore
{
    string PutProfileReview(ProfileReviewActionState state, TimeSpan ttl);
    ProfileReviewActionState? TakeProfileReview(string token);
    void DiscardProfileReview(string token);
}
