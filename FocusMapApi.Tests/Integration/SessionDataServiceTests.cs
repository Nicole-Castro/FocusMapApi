using FocusMapApi.Data;
using FocusMapApi.Services.SessionData;
using FocusMapApi.Tests.Builders;
using FocusMapApi.Tests.Infrastructure;
using FluentAssertions;

namespace FocusMapApi.Tests.Integration;

[Collection("Database")]
public class SessionDataServiceTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SessionDataService _service = null!;

    public async Task InitializeAsync()
    {
        _context = postgres.CreateContext();
        _service = new SessionDataService(_context);
        await postgres.CleanupAsync(_context);
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // ── GetPatientProgress – patient not found ────────────────────────────────

    [Fact]
    public async Task GetPatientProgress_UnknownPatient_ShouldReturnNotFound()
    {
        var result = await _service.GetPatientProgress(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── GetPatientProgress – no sessions ─────────────────────────────────────

    [Fact]
    public async Task GetPatientProgress_PatientWithNoSessions_ShouldReturnZeroTotalSessions()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        result.Success.Should().BeTrue();
        result.Data!.total_sessions.Should().Be(0);
        result.Data.sessions.Should().BeEmpty();
    }

    // ── GetPatientProgress – attention distribution ───────────────────────────

    [Fact]
    public async Task GetPatientProgress_AttentionDistribution_ShouldCountBoundaryValuesCorrectly()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        _context.SessionData.AddRange(SessionDataBuilder.WithAttentionBuckets(session.Id));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var dist = result.Data!.sessions[0].attention_distribution;
        dist.low_count.Should().Be(3,        "valores 0, 10, 24 devem cair em low (< 25)");
        dist.medium_low_count.Should().Be(3, "valores 25, 30, 49 devem cair em medium_low (25–49)");
        dist.medium_count.Should().Be(3,     "valores 50, 60, 74 devem cair em medium (50–74)");
        dist.high_count.Should().Be(3,       "valores 75, 90, 100 devem cair em high (>= 75)");
    }

    [Theory]
    [InlineData(0,   "low")]
    [InlineData(24,  "low")]
    [InlineData(25,  "medium_low")]
    [InlineData(49,  "medium_low")]
    [InlineData(50,  "medium")]
    [InlineData(74,  "medium")]
    [InlineData(75,  "high")]
    [InlineData(100, "high")]
    public async Task GetPatientProgress_AttentionBoundaryValue_ShouldFallInCorrectBucket(
        int attentionValue, string expectedBucket)
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        _context.SessionData.Add(SessionDataBuilder.WithAttention(session.Id, attentionValue));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var dist = result.Data!.sessions[0].attention_distribution;
        if (expectedBucket == "low")        dist.low_count.Should().Be(1);
        if (expectedBucket == "medium_low") dist.medium_low_count.Should().Be(1);
        if (expectedBucket == "medium")     dist.medium_count.Should().Be(1);
        if (expectedBucket == "high")       dist.high_count.Should().Be(1);
    }

    // ── GetPatientProgress – aggregated metrics ───────────────────────────────

    [Fact]
    public async Task GetPatientProgress_ShouldCalculateOverallAvgAttention()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        _context.SessionData.AddRange(
            SessionDataBuilder.WithAttention(session.Id, 40),
            SessionDataBuilder.WithAttention(session.Id, 60));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        result.Data!.overall_avg_attention.Should().Be(50);
    }

    [Fact]
    public async Task GetPatientProgress_ShouldTrackMaxAndMinAttention()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        _context.SessionData.AddRange(
            SessionDataBuilder.WithAttention(session.Id, 10),
            SessionDataBuilder.WithAttention(session.Id, 55),
            SessionDataBuilder.WithAttention(session.Id, 90));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        result.Data!.overall_max_attention.Should().Be(90);
        result.Data.overall_min_attention.Should().Be(10);
    }

    // ── GetPatientProgress – trend (first half vs second half) ───────────────

    [Fact]
    public async Task GetPatientProgress_WithFewerThan10Records_TrendFieldsShouldBeNull()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        // Only 9 records — below the 10-record threshold
        for (int i = 0; i < 9; i++)
            _context.SessionData.Add(SessionDataBuilder.WithAttention(session.Id, 50));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var s = result.Data!.sessions[0];
        s.first_half_avg_attention.Should().BeNull();
        s.second_half_avg_attention.Should().BeNull();
        s.attention_trend_diff.Should().BeNull();
    }

    [Fact]
    public async Task GetPatientProgress_WithConstantAttention_TrendDiffShouldBeZero()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        // 10 records all with the same attention → trendDiff = 0
        for (int i = 0; i < 10; i++)
            _context.SessionData.Add(SessionDataBuilder.WithAttention(session.Id, 60));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var s = result.Data!.sessions[0];
        s.attention_trend_diff.Should().Be(0);
        s.first_half_avg_attention.Should().Be(s.second_half_avg_attention);
    }

    // ── GetPatientProgress – location clustering ──────────────────────────────

    [Fact]
    public async Task GetPatientProgress_TwoSessionsWithinClusterRadius_ShouldProduceSingleCluster()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);

        var session1 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-2));
        var session2 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-1));

        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(session1, session2);

        // Points within 0.0005° Euclidean distance → same cluster
        // distance = sqrt(0.0003² + 0.0002²) ≈ 0.00036 < 0.0005
        _context.SessionData.Add(SessionDataBuilder.WithCoords(session1.Id, -23.5489, -46.6388));
        _context.SessionData.Add(SessionDataBuilder.WithCoords(session2.Id, -23.5489 + 0.0003, -46.6388 + 0.0002));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var locationGroups = result.Data!.attention_by_location
            .Where(l => l.location_label != "Local não registrado")
            .ToList();

        locationGroups.Should().HaveCount(1);
        locationGroups[0].session_count.Should().Be(2);
    }

    [Fact]
    public async Task GetPatientProgress_TwoSessionsFarApart_ShouldProduceTwoClusters()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);

        var session1 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-2));
        var session2 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-1));

        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(session1, session2);

        // Points far apart → different clusters
        _context.SessionData.Add(SessionDataBuilder.WithCoords(session1.Id, -23.5489, -46.6388));
        _context.SessionData.Add(SessionDataBuilder.WithCoords(session2.Id, -23.6000, -46.7000));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var locationGroups = result.Data!.attention_by_location
            .Where(l => l.location_label != "Local não registrado")
            .ToList();

        locationGroups.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPatientProgress_SessionWithNoCoords_ShouldGoToUnregisteredLocation()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        // EEG data without coordinates
        _context.SessionData.Add(SessionDataBuilder.WithAttention(session.Id, 50));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        result.Data!.attention_by_location
            .Should().Contain(l => l.location_label == "Local não registrado");
    }

    // ── GetPatientProgress – hourly attention ─────────────────────────────────

    [Fact]
    public async Task GetPatientProgress_ShouldGroupRecordsByHour()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);

        var base8h = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var base14h = new DateTime(2026, 1, 1, 14, 0, 0, DateTimeKind.Utc);

        _context.SessionData.AddRange(
            SessionDataBuilder.WithAttention(session.Id, 40, base8h),
            SessionDataBuilder.WithAttention(session.Id, 60, base8h.AddMinutes(30)),
            SessionDataBuilder.WithAttention(session.Id, 80, base14h));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var hourly = result.Data!.attention_by_hour;
        hourly.Should().Contain(h => h.hour_start == 8 && h.record_count == 2);
        hourly.Should().Contain(h => h.hour_start == 14 && h.record_count == 1);

        var hour8 = hourly.First(h => h.hour_start == 8);
        hour8.avg_attention.Should().Be(50); // (40 + 60) / 2
        hour8.hour_range.Should().Be("08h–09h");
    }

    // ── GetPatientProgress – trend timeline ───────────────────────────────────

    [Fact]
    public async Task GetPatientProgress_ShouldBuildTrendWithOnePointPerSession()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var s1 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-3));
        var s2 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-2));
        var s3 = SessionBuilder.Finished(patient.Id, DateTime.UtcNow.AddDays(-1));
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(s1, s2, s3);
        _context.SessionData.AddRange(
            SessionDataBuilder.WithAttention(s1.Id, 30),
            SessionDataBuilder.WithAttention(s2.Id, 50),
            SessionDataBuilder.WithAttention(s3.Id, 70));
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientProgress(patient.Id);

        var trend = result.Data!.attention_trend;
        trend.Should().HaveCount(3);
        trend[0].session_index.Should().Be(1);
        trend[1].session_index.Should().Be(2);
        trend[2].session_index.Should().Be(3);
        trend[0].avg_attention.Should().Be(30);
        trend[1].avg_attention.Should().Be(50);
        trend[2].avg_attention.Should().Be(70);
    }
}
