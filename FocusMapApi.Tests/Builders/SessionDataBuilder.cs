using FocusMapApi.Models;

namespace FocusMapApi.Tests.Builders;

public static class SessionDataBuilder
{
    public static SessionDataModel WithAttention(Guid sessionId, int attentionValue, DateTime? timestamp = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            AttentionValue = attentionValue,
            MeditationValue = 50,
            TimestampOfRecord = timestamp ?? DateTime.UtcNow,
        };

    public static SessionDataModel WithCoords(
        Guid sessionId,
        double latitude,
        double longitude,
        int attentionValue = 50,
        DateTime? timestamp = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            AttentionValue = attentionValue,
            MeditationValue = 50,
            Latitude = latitude,
            Longitude = longitude,
            TimestampOfRecord = timestamp ?? DateTime.UtcNow,
        };

    public static IEnumerable<SessionDataModel> WithAttentionBuckets(Guid sessionId)
    {
        var baseTime = DateTime.UtcNow;

        // low_count: < 25
        yield return WithAttention(sessionId, 0,  baseTime.AddSeconds(1));
        yield return WithAttention(sessionId, 10, baseTime.AddSeconds(2));
        yield return WithAttention(sessionId, 24, baseTime.AddSeconds(3));

        // medium_low_count: 25–49
        yield return WithAttention(sessionId, 25, baseTime.AddSeconds(4));
        yield return WithAttention(sessionId, 30, baseTime.AddSeconds(5));
        yield return WithAttention(sessionId, 49, baseTime.AddSeconds(6));

        // medium_count: 50–74
        yield return WithAttention(sessionId, 50, baseTime.AddSeconds(7));
        yield return WithAttention(sessionId, 60, baseTime.AddSeconds(8));
        yield return WithAttention(sessionId, 74, baseTime.AddSeconds(9));

        // high_count: >= 75
        yield return WithAttention(sessionId, 75,  baseTime.AddSeconds(10));
        yield return WithAttention(sessionId, 90,  baseTime.AddSeconds(11));
        yield return WithAttention(sessionId, 100, baseTime.AddSeconds(12));
    }
}
