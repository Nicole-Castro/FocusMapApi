using Bogus;
using FocusMapApi.Models;

namespace FocusMapApi.Tests.Builders;

public static class SessionBuilder
{
    public static SessionModel InProgress(Guid patientId) =>
        new Faker<SessionModel>()
            .RuleFor(s => s.Id, _ => Guid.NewGuid())
            .RuleFor(s => s.PatientId, _ => patientId)
            .RuleFor(s => s.SessionStartTime, f => f.Date.Recent(days: 30).ToUniversalTime())
            .RuleFor(s => s.SessionEndTime, _ => (DateTime?)null)
            .RuleFor(s => s.SessionName, f => $"session-{f.Date.Recent().ToUniversalTime():yyyyMMddHHmmss}")
            .Generate();

    public static SessionModel Finished(Guid patientId, DateTime? startTime = null)
    {
        var start = startTime ?? DateTime.UtcNow.AddHours(-2);
        return new Faker<SessionModel>()
            .RuleFor(s => s.Id, _ => Guid.NewGuid())
            .RuleFor(s => s.PatientId, _ => patientId)
            .RuleFor(s => s.SessionStartTime, _ => start)
            .RuleFor(s => s.SessionEndTime, _ => (DateTime?)start.AddHours(1))
            .RuleFor(s => s.SessionName, _ => $"session-{start:yyyyMMddHHmmss}")
            .Generate();
    }
}
