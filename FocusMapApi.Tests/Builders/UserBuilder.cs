using Bogus;
using FocusMapApi.Models;

namespace FocusMapApi.Tests.Builders;

public static class UserBuilder
{
    public static UserModel Professional() =>
        new Faker<UserModel>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Role, _ => UserRole.Professional)
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
            .RuleFor(u => u.IsDeleted, _ => false)
            .RuleFor(u => u.CreatedAt, _ => DateTime.UtcNow)
            .Generate();
}
