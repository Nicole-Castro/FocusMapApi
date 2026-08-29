using Bogus;
using FocusMapApi.Models;

namespace FocusMapApi.Tests.Builders;

public static class UserBuilder
{
    // Senha em texto puro usada por todo usuário gerado por este builder — os testes
    // de login usam essa constante em vez de uma senha qualquer.
    public const string DefaultPassword = "Test@1234";

    public static UserModel Professional() =>
        new Faker<UserModel>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Role, _ => UserRole.Professional)
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword(DefaultPassword))
            .RuleFor(u => u.IsDeleted, _ => false)
            .RuleFor(u => u.CreatedAt, _ => DateTime.UtcNow)
            .Generate();

    public static UserModel Patient(Guid professionalId) =>
        new Faker<UserModel>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Role, _ => UserRole.Patient)
            .RuleFor(u => u.ProfessionalId, _ => professionalId)
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword(DefaultPassword))
            .RuleFor(u => u.IsDeleted, _ => false)
            .RuleFor(u => u.CreatedAt, _ => DateTime.UtcNow)
            .Generate();

    public static UserModel Admin() =>
        new Faker<UserModel>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Role, _ => UserRole.Admin)
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword(DefaultPassword))
            .RuleFor(u => u.IsDeleted, _ => false)
            .RuleFor(u => u.CreatedAt, _ => DateTime.UtcNow)
            .Generate();
}
