using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FocusMapApi.Models;

[Table("points_of_interest")]
public class InterestPointModel
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    [JsonIgnore]
    public virtual UserModel? Patient { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [JsonIgnore]
    public virtual ICollection<AudioDescriptionModel> AudioDescriptions { get; set; } = new List<AudioDescriptionModel>();
}
