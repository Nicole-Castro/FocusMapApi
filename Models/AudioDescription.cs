using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FocusMapApi.Models;

[Table("audio_description")]
public class AudioDescriptionModel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Required]
    [Column("start_of_audio")]
    public DateTime StartOfAudio { get; set; }

    [Required]
    [Column("end_of_audio")]
    public DateTime EndOfAudio { get; set; }

    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("id_point_of_interest")]
    public Guid? InterestPointId { get; set; }

    [ForeignKey(nameof(SessionId))]
    [JsonIgnore]
    public virtual SessionModel? Session { get; set; }

    [ForeignKey(nameof(InterestPointId))]
    [JsonIgnore]
    public virtual InterestPointModel? InterestPoint { get; set; }
}
