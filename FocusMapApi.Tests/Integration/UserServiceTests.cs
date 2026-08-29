using FocusMapApi.Services.Auth;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using FocusMapApi.Services.User;
using FocusMapApi.Tests.Builders;
using FocusMapApi.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FocusMapApi.Tests.Integration;

[Collection("Database")]
public class UserServiceTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private UserService _service = null!;

    public async Task InitializeAsync()
    {
        _context = postgres.CreateContext();

        var authMock = Substitute.For<IAuthInterface>();
        authMock.GenerateJwtToken(Arg.Any<object>()).Returns("fake-jwt-token");

        _service = new UserService(_context, authMock);
        await postgres.CleanupAsync(_context);
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // ── CreateUser ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_WithNewEmail_ShouldReturnSuccess()
    {
        var dto = new CreateUserDto { Email = "new@test.com", Name = "New User", Password = "Test@1234" };

        var result = await _service.CreateUser(dto);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CreateUser_WithoutPassword_ShouldReturnError()
    {
        var dto = new CreateUserDto { Email = "nopass@test.com", Name = "No Password" };

        var result = await _service.CreateUser(dto);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateUser_ShouldPersistProfessionalInDatabase()
    {
        var dto = new CreateUserDto { Email = "persisted@test.com", Name = "Persisted User", Password = "Test@1234" };

        await _service.CreateUser(dto);

        var exists = await _context.Profiles.AnyAsync(u => u.Email == dto.Email);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_ShouldHashPasswordBeforeStoring()
    {
        var dto = new CreateUserDto { Email = "hashed@test.com", Name = "Hashed", Password = "Test@1234" };

        await _service.CreateUser(dto);

        var saved = await _context.Profiles.FirstAsync(u => u.Email == dto.Email);
        saved.PasswordHash.Should().NotBeNullOrEmpty();
        saved.PasswordHash.Should().NotBe(dto.Password);
        BCrypt.Net.BCrypt.Verify(dto.Password, saved.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturnError()
    {
        var dto = new CreateUserDto { Email = "dup@test.com", Name = "First", Password = "Test@1234" };
        await _service.CreateUser(dto);

        var result = await _service.CreateUser(new CreateUserDto { Email = "dup@test.com", Name = "Second", Password = "Test@1234" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    // ── CreateUserPatient ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserPatient_ShouldLinkPatientToProfessional()
    {
        var professional = UserBuilder.Professional();
        _context.Profiles.Add(professional);
        await _context.SaveChangesAsync();

        var dto = new CreatePatientDto { name = "Patient One", email = "patient@test.com", password = "Test@1234" };

        var result = await _service.CreateUserPatient(dto, professional.Id);

        result.Success.Should().BeTrue();
        var patient = await _context.Profiles
            .FirstOrDefaultAsync(u => u.Email == dto.email);
        patient.Should().NotBeNull();
        patient!.ProfessionalId.Should().Be(professional.Id);
        patient.Role.Should().Be(UserRole.Patient);
    }

    [Fact]
    public async Task CreateUserPatient_WithDuplicateEmail_ShouldReturnError()
    {
        var professional = UserBuilder.Professional();
        _context.Profiles.Add(professional);
        await _context.SaveChangesAsync();

        var dto = new CreatePatientDto { name = "P", email = "dup-patient@test.com", password = "Test@1234" };
        await _service.CreateUserPatient(dto, professional.Id);

        var result = await _service.CreateUserPatient(dto, professional.Id);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserPatient_WithoutPassword_ShouldReturnError()
    {
        var professional = UserBuilder.Professional();
        _context.Profiles.Add(professional);
        await _context.SaveChangesAsync();

        var dto = new CreatePatientDto { name = "No Password", email = "nopass-patient@test.com" };

        var result = await _service.CreateUserPatient(dto, professional.Id);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    // ── DeletePatient ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePatient_ShouldSetIsDeletedTrue_AndKeepRecordInDatabase()
    {
        var professional = UserBuilder.Professional();
        var patient = UserBuilder.Patient(professional.Id);
        _context.Profiles.AddRange(professional, patient);
        await _context.SaveChangesAsync();

        await _service.DeletePatient(patient.Id, professional.Id);

        var rawRecord = await _context.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == patient.Id);

        rawRecord.Should().NotBeNull();
        rawRecord!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePatient_ShouldNotAppearInListPatients_AfterDeletion()
    {
        var professional = UserBuilder.Professional();
        var patient = UserBuilder.Patient(professional.Id);
        _context.Profiles.AddRange(professional, patient);
        await _context.SaveChangesAsync();

        await _service.DeletePatient(patient.Id, professional.Id);
        var list = await _service.ListPatients(professional.Id, null);

        list.Data.Should().NotContain(p => p.id == patient.Id);
    }

    [Fact]
    public async Task DeletePatient_WithWrongProfessionalId_ShouldReturnNotFound()
    {
        var professional1 = UserBuilder.Professional();
        var professional2 = UserBuilder.Professional();
        var patient = UserBuilder.Patient(professional1.Id);
        _context.Profiles.AddRange(professional1, professional2, patient);
        await _context.SaveChangesAsync();

        // professional2 trying to delete a patient belonging to professional1
        var result = await _service.DeletePatient(patient.Id, professional2.Id);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeletePatient_AlreadyDeleted_ShouldReturnNotFound()
    {
        var professional = UserBuilder.Professional();
        var patient = UserBuilder.Patient(professional.Id);
        _context.Profiles.AddRange(professional, patient);
        await _context.SaveChangesAsync();

        await _service.DeletePatient(patient.Id, professional.Id);
        var result = await _service.DeletePatient(patient.Id, professional.Id);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── ListPatients ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPatients_ShouldOnlyReturnPatientsOfTheProfessional()
    {
        var pro1 = UserBuilder.Professional();
        var pro2 = UserBuilder.Professional();
        var p1 = UserBuilder.Patient(pro1.Id);
        var p2 = UserBuilder.Patient(pro2.Id);
        _context.Profiles.AddRange(pro1, pro2, p1, p2);
        await _context.SaveChangesAsync();

        var result = await _service.ListPatients(pro1.Id, null);

        result.Data.Should().HaveCount(1);
        result.Data![0].id.Should().Be(p1.Id);
    }

    [Fact]
    public async Task ListPatients_WithSearchTerm_ShouldFilterByName()
    {
        var pro = UserBuilder.Professional();
        var matching = UserBuilder.Patient(pro.Id);
        matching.Name = "Ana Paula";
        var other = UserBuilder.Patient(pro.Id);
        other.Name = "Carlos Eduardo";
        _context.Profiles.AddRange(pro, matching, other);
        await _context.SaveChangesAsync();

        var result = await _service.ListPatients(pro.Id, "ana");

        result.Data.Should().HaveCount(1);
        result.Data![0].name.Should().Be("Ana Paula");
    }

    // ── UpdateUser ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_ShouldChangeName()
    {
        var user = UserBuilder.Professional();
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateUser(new UpdateUserDto { name = "New Name" }, user.Id);

        result.Success.Should().BeTrue();
        var updated = await _context.Profiles.FindAsync(user.Id);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateUser_WithPassword_ShouldUpdatePasswordHashAndAllowLoginWithNewPassword()
    {
        var user = UserBuilder.Professional();
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateUser(new UpdateUserDto { name = "New Name", password = "NovaSenha@123" }, user.Id);

        result.Success.Should().BeTrue();
        var updated = await _context.Profiles.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NovaSenha@123", updated!.PasswordHash).Should().BeTrue();
        // A senha antiga não pode continuar funcionando.
        BCrypt.Net.BCrypt.Verify(UserBuilder.DefaultPassword, updated.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUser_WithoutPassword_ShouldKeepOldPasswordHash()
    {
        var user = UserBuilder.Professional();
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();
        var originalHash = user.PasswordHash;

        await _service.UpdateUser(new UpdateUserDto { name = "New Name" }, user.Id);

        var updated = await _context.Profiles.FindAsync(user.Id);
        updated!.PasswordHash.Should().Be(originalHash);
    }

    [Fact]
    public async Task UpdateUser_WithNullName_ShouldKeepOldName()
    {
        var user = UserBuilder.Professional();
        var originalName = user.Name;
        _context.Profiles.Add(user);
        await _context.SaveChangesAsync();

        await _service.UpdateUser(new UpdateUserDto { name = null }, user.Id);

        var updated = await _context.Profiles.FindAsync(user.Id);
        updated!.Name.Should().Be(originalName);
    }

    [Fact]
    public async Task UpdateUser_NonExistentId_ShouldReturnNotFound()
    {
        var result = await _service.UpdateUser(new UpdateUserDto { name = "X" }, Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── ListProfessionals ────────────────────────────────────────────────────

    [Fact]
    public async Task ListProfessionals_ShouldReturnOnlyProfessionals()
    {
        var pro1 = UserBuilder.Professional();
        var pro2 = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro1.Id);
        _context.Profiles.AddRange(pro1, pro2, patient);
        await _context.SaveChangesAsync();

        var result = await _service.ListProfessionals();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(p => p.id == pro1.Id || p.id == pro2.Id);
    }

    [Fact]
    public async Task ListProfessionals_WithSearchTerm_ShouldFilterByNameOrEmail()
    {
        var match = UserBuilder.Professional();
        match.Name = "Doutora Ana";
        match.Email = "doutora.ana@test.com";
        var other = UserBuilder.Professional();
        other.Name = "Carlos";
        other.Email = "carlos@test.com";
        _context.Profiles.AddRange(match, other);
        await _context.SaveChangesAsync();

        var result = await _service.ListProfessionals("ana");

        result.Data.Should().HaveCount(1);
        result.Data![0].id.Should().Be(match.Id);
    }

    [Fact]
    public async Task ListProfessionals_ShouldNotIncludeDeleted()
    {
        var pro = UserBuilder.Professional();
        pro.IsDeleted = true;
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        var result = await _service.ListProfessionals();

        result.Data.Should().BeEmpty();
    }

    // ── DeleteProfessional ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteProfessional_ShouldSetIsDeletedTrue()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteProfessional(pro.Id);

        result.Success.Should().BeTrue();
        var rawRecord = await _context.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == pro.Id);
        rawRecord!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProfessional_NonExistentId_ShouldReturnNotFound()
    {
        var result = await _service.DeleteProfessional(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteProfessional_OnPatientId_ShouldReturnNotFound()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        // Endpoint é só pra Professional — não deve deletar um Patient por engano.
        var result = await _service.DeleteProfessional(patient.Id);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── ResetProfessionalPassword ────────────────────────────────────────────

    [Fact]
    public async Task ResetProfessionalPassword_ShouldReturnNewPasswordThatWorks()
    {
        var pro = UserBuilder.Professional();
        _context.Profiles.Add(pro);
        await _context.SaveChangesAsync();

        var result = await _service.ResetProfessionalPassword(pro.Id);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();

        var updated = await _context.Profiles.FirstAsync(u => u.Id == pro.Id);
        BCrypt.Net.BCrypt.Verify(result.Data, updated.PasswordHash).Should().BeTrue();
        // Senha antiga não deve mais funcionar.
        BCrypt.Net.BCrypt.Verify(UserBuilder.DefaultPassword, updated.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task ResetProfessionalPassword_NonExistentId_ShouldReturnNotFound()
    {
        var result = await _service.ResetProfessionalPassword(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── UpdateUserPatient ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserPatient_WithPassword_ShouldUpdatePasswordHash()
    {
        var pro = UserBuilder.Professional();
        var patient = UserBuilder.Patient(pro.Id);
        _context.Profiles.AddRange(pro, patient);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateUserPatient(new UpdateUserDto { password = "NovaSenha@123" }, patient.Id);

        result.Success.Should().BeTrue();
        var updated = await _context.Profiles.FindAsync(patient.Id);
        BCrypt.Net.BCrypt.Verify("NovaSenha@123", updated!.PasswordHash).Should().BeTrue();
    }

    // ── TotalPatients ─────────────────────────────────────────────────────────

    [Fact]
    public async Task TotalPatients_ShouldCountOnlyActivePatients()
    {
        var pro = UserBuilder.Professional();
        var active1 = UserBuilder.Patient(pro.Id);
        var active2 = UserBuilder.Patient(pro.Id);
        var deleted = UserBuilder.Patient(pro.Id);
        deleted.IsDeleted = true;
        _context.Profiles.AddRange(pro, active1, active2, deleted);
        await _context.SaveChangesAsync();

        var result = await _service.TotalPatients(pro.Id);

        result.Success.Should().BeTrue();
        // soft delete filter excludes deleted → only 2 active patients
        // Anonymous type is internal to the main assembly; use reflection to read the value
        var total = result.Data!.GetType().GetProperty("total")?.GetValue(result.Data);
        total.Should().Be(2);
    }
}
