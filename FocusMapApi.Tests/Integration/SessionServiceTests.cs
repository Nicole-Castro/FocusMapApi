using FocusMapApi.Data;
using FocusMapApi.DTO.Session;
using FocusMapApi.Services.Sessions;
using FocusMapApi.Tests.Builders;
using FocusMapApi.Tests.Infrastructure;
using FluentAssertions;

namespace FocusMapApi.Tests.Integration;

[Collection("Database")]
public class SessionServiceTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SessionService _service = null!;

    public async Task InitializeAsync()
    {
        _context = postgres.CreateContext();
        _service = new SessionService(_context);
        await postgres.CleanupAsync(_context);
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // ── CreateSessionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_ShouldReturnSuccessAndSessionId()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.CreateSessionAsync(new SessionCreateDto { patient_id = patient.Id });

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Data, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldSetSessionName()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.CreateSessionAsync(new SessionCreateDto { patient_id = patient.Id });

        var sessionId = Guid.Parse(result.Data!);
        var session = await _context.Sessions.FindAsync(sessionId);
        session!.SessionName.Should().StartWith("session-");
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldLeaveSessionEndTimeNull()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.CreateSessionAsync(new SessionCreateDto { patient_id = patient.Id });

        var session = await _context.Sessions.FindAsync(Guid.Parse(result.Data!));
        session!.SessionEndTime.Should().BeNull();
    }

    // ── UpdateSessionAsync (end session) ──────────────────────────────────────

    [Fact]
    public async Task UpdateSessionAsync_ShouldSetSessionEndTime()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var before = DateTime.UtcNow;
        var result = await _service.UpdateSessionAsync(session.Id);
        var after = DateTime.UtcNow;

        result.Success.Should().BeTrue();
        var updated = await _context.Sessions.FindAsync(session.Id);
        updated!.SessionEndTime.Should().NotBeNull();
        updated.SessionEndTime!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithInvalidId_ShouldReturnNotFound()
    {
        var result = await _service.UpdateSessionAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── GetSessionByIdAsync – duration formatting ─────────────────────────────

    [Fact]
    public async Task GetSessionById_DurationLessThanOneMinute_ShouldFormatAsSeconds()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var start = DateTime.UtcNow.AddSeconds(-45);
        var session = SessionBuilder.Finished(patient.Id, startTime: start);
        session.SessionEndTime = start.AddSeconds(45);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, pro.Id, "Professional");

        result.Data!.session_duration.Should().EndWith("s");
        result.Data.session_duration.Should().NotContain("m ");
        result.Data.session_duration.Should().NotContain("h ");
    }

    [Fact]
    public async Task GetSessionById_DurationBetween1And60Minutes_ShouldFormatAsMinutesAndSeconds()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var session = SessionBuilder.Finished(patient.Id, startTime: start);
        session.SessionEndTime = start.AddMinutes(30);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, pro.Id, "Professional");

        result.Data!.session_duration.Should().Contain("m ");
        result.Data.session_duration.Should().NotContain("h ");
    }

    [Fact]
    public async Task GetSessionById_DurationMoreThanOneHour_ShouldFormatAsHoursMinutesSeconds()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var start = DateTime.UtcNow.AddHours(-2);
        var session = SessionBuilder.Finished(patient.Id, startTime: start);
        session.SessionEndTime = start.AddHours(2);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, pro.Id, "Professional");

        result.Data!.session_duration.Should().Contain("h ");
        result.Data.session_duration.Should().Contain("m ");
    }

    [Fact]
    public async Task GetSessionById_InProgressSession_ShouldHaveNullDuration()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, pro.Id, "Professional");

        result.Data!.session_duration.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionById_WithInvalidId_ShouldReturnNotFound()
    {
        var result = await _service.GetSessionByIdAsync(Guid.NewGuid(), Guid.NewGuid(), "Professional");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── GetSessionByIdAsync / getSessionDashboardAsync — controle de acesso ───

    [Fact]
    public async Task GetSessionById_AsOwningProfessional_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, pro.Id, "Professional");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionById_AsOwnPatient_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, patient.Id, "Patient");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionById_AsUnrelatedProfessional_ShouldReturnForbidden()
    {
        var pro = UserBuilder.Professional();
        var otherPro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, otherPro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, otherPro.Id, "Professional");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSessionById_AsUnrelatedPatient_ShouldReturnForbidden()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var otherPatient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient, otherPatient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, otherPatient.Id, "Patient");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSessionById_AsAdmin_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionByIdAsync(session.Id, Guid.NewGuid(), "Admin");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionDashboard_AsUnrelatedPatient_ShouldReturnForbidden()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var otherPatient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient, otherPatient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.getSessionDashboardAsync(session.Id, otherPatient.Id, "Patient");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSessionDashboard_AsOwnPatient_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.getSessionDashboardAsync(session.Id, patient.Id, "Patient");

        result.Success.Should().BeTrue();
    }

    // ── GetSessionsByPatientIdAsync — controle de acesso ──────────────────────

    [Fact]
    public async Task GetSessionsByPatientId_AsOwnPatient_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var session = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByPatientIdAsync(patient.Id, patient.Id, "Patient");

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle(s => s.id == session.Id);
    }

    [Fact]
    public async Task GetSessionsByPatientId_AsOwningProfessional_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByPatientIdAsync(patient.Id, pro.Id, "Professional");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionsByPatientId_AsUnrelatedPatient_ShouldReturnForbidden()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var otherPatient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient, otherPatient);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByPatientIdAsync(patient.Id, otherPatient.Id, "Patient");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSessionsByPatientId_AsUnrelatedProfessional_ShouldReturnForbidden()
    {
        var pro = UserBuilder.Professional();
        var otherPro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, otherPro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByPatientIdAsync(patient.Id, otherPro.Id, "Professional");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSessionsByPatientId_AsAdmin_ShouldSucceed()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByPatientIdAsync(patient.Id, Guid.NewGuid(), "Admin");

        result.Success.Should().BeTrue();
    }

    // ── GetSessionsByProfessionalIdAsync – pagination & filters ───────────────

    [Fact]
    public async Task GetSessionsByProfessionalId_ShouldReturnOnlyOwnPatientsSessions()
    {
        var pro1 = UserBuilder.Professional();
        var pro2 = UserBuilder.Professional();
        var p1 = UserBuilder.Patient(pro1.Id);
        var p2 = UserBuilder.Patient(pro2.Id);
        var s1 = SessionBuilder.Finished(p1.Id);
        var s2 = SessionBuilder.Finished(p2.Id);
        _context.Profiles.AddRange(pro1, pro2, p1, p2);
        _context.Sessions.AddRange(s1, s2);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro1.Id, page: 1, pageSize: 10, status: null, patientId: null, dateFrom: null, dateTo: null);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].id.Should().Be(s1.Id);
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_FilterByStatusFinished_ShouldOnlyReturnFinished()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var finished = SessionBuilder.Finished(patient.Id);
        var active = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(finished, active);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, 1, 10, status: "finished", null, null, null);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].session_end_time.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_FilterByStatusInProgress_ShouldOnlyReturnActive()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var finished = SessionBuilder.Finished(patient.Id);
        var active = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(finished, active);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, 1, 10, status: "in_progress", null, null, null);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].session_end_time.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_ShouldReturnAggregatedCounts()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var f1 = SessionBuilder.Finished(patient.Id);
        var f2 = SessionBuilder.Finished(patient.Id);
        var a1 = SessionBuilder.InProgress(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(f1, f2, a1);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, 1, 10, null, null, null, null);

        result.Data!.TotalFinished.Should().Be(2);
        result.Data.TotalInProgress.Should().Be(1);
        result.Data.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_Pagination_ShouldRespectPageSize()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        var sessions = Enumerable.Range(0, 5)
            .Select(_ => SessionBuilder.Finished(patient.Id))
            .ToList();
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, page: 1, pageSize: 2, null, null, null, null);

        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalPages.Should().Be(3);
        result.Data.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_FilterByPatientId_ShouldReturnOnlyThatPatientsessions()
    {
        var pro = UserBuilder.Professional();
        var p1 = UserBuilder.Patient(pro.Id);
        var p2 = UserBuilder.Patient(pro.Id);
        var s1 = SessionBuilder.Finished(p1.Id);
        var s2 = SessionBuilder.Finished(p2.Id);
        _context.Profiles.AddRange(pro, p1, p2);
        _context.Sessions.AddRange(s1, s2);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, 1, 10, null, patientId: p1.Id, null, null);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].patient_id.Should().Be(p1.Id);
    }

    [Fact]
    public async Task GetSessionsByProfessionalId_DeletedPatient_ShouldNotBeReturned()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        patient.IsDeleted = true;
        var session = SessionBuilder.Finished(patient.Id);
        _context.Profiles.AddRange(pro, patient);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var result = await _service.GetSessionsByProfessionalIdAsync(
            pro.Id, 1, 10, null, null, null, null);

        result.Data!.Items.Should().BeEmpty();
    }
}
