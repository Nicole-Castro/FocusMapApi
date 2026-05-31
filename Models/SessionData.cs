using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Models;

[Table("session_data")]
[Index(nameof(SessionId), nameof(TimestampOfRecord))]
public class SessionDataModel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("delta_power")]
    public int? DeltaPower { get; set; }

    [Column("theta_power")]
    public int? ThetaPower { get; set; }

    [Column("low_alpha_power")]
    public int? LowAlphaPower { get; set; }

    [Column("high_alpha_power")]
    public int? HighAlphaPower { get; set; }

    [Column("low_beta_power")]
    public int? LowBetaPower { get; set; }

    [Column("high_beta_power")]
    public int? HighBetaPower { get; set; }

    [Column("low_gamma_power")]
    public int? LowGammaPower { get; set; }

    [Column("middle_gamma_power")]
    public int? MiddleGammaPower { get; set; }

    [Column("attention_value")]
    public int? AttentionValue { get; set; }

    [Column("meditation_value")]
    public int? MeditationValue { get; set; }

    [Column("raw_eeg_value")]
    public int? RawEegValue { get; set; }

    [Required]
    [Column("timestamp_of_record")]
    public DateTime TimestampOfRecord { get; set; }

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [ForeignKey(nameof(SessionId))]
    public virtual SessionModel? Session { get; set; }
}
