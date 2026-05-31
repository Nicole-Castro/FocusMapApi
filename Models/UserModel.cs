using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Models;

public enum UserRole
{
    Professional,
    Patient
}

[Table("profiles")]
[Index(nameof(Email), IsUnique = true)]
public class UserModel
{
    // Mesmo ID do auth.users do Supabase — não gerado pelo EF
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    public UserRole Role { get; set; }

    [MaxLength(50)]
    [Column("professional_license")]
    public string? ProfessionalLicense { get; set; }

    // Self-referencing: paciente aponta para seu profissional responsável
    [Column("professional_id")]
    public Guid? ProfessionalId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [ForeignKey(nameof(ProfessionalId))]
    [JsonIgnore]
    public virtual UserModel? Professional { get; set; }

    [JsonIgnore]
    public virtual ICollection<UserModel> Patients { get; set; } = new List<UserModel>();

    [JsonIgnore]
    public virtual ICollection<SessionModel> Sessions { get; set; } = new List<SessionModel>();

    [JsonIgnore]
    public virtual ICollection<InterestPointModel> InterestPoints { get; set; } = new List<InterestPointModel>();
}
