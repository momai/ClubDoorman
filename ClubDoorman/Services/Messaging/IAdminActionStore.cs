using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services.Messaging;

public interface IAdminActionStore
{
    string PutProfileReview(AiProfileAnalysisData data, TimeSpan ttl);
    AiProfileAnalysisData? TakeProfileReview(string token);
}
