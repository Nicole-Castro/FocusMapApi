using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserModel> Profiles { get; set; }
    public DbSet<SessionModel> Sessions { get; set; }
    public DbSet<SessionDataModel> SessionData { get; set; }
    public DbSet<InterestPointModel> InterestPoints { get; set; }
    public DbSet<AudioDescriptionModel> AudioDescriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Converte o enum UserRole para string no banco (coluna "role" é varchar)
        modelBuilder.Entity<UserModel>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // Self-referencing: paciente → profissional
        modelBuilder.Entity<UserModel>()
            .HasOne(u => u.Professional)
            .WithMany(u => u.Patients)
            .HasForeignKey(u => u.ProfessionalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índice composto para queries de EEG por sessão + tempo
        modelBuilder.Entity<SessionDataModel>()
            .HasIndex(s => new { s.SessionId, s.TimestampOfRecord });

        // Soft delete global — EF ignora registros deletados por padrão
        modelBuilder.Entity<UserModel>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<InterestPointModel>().HasQueryFilter(i => !i.IsDeleted);
    }
}
