using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FocusMapApi.Models;

public class SessionData
{
    [Key]
    [Column("id")]
    public Guid id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid session_id { get; set; }

    [Column("delta_power")]
    public int? delta_power { get; set; }

    [Column("theta_power")]
    public int? theta_power { get; set; }

    [Column("low_alpha_power")]
    public int? low_alpha_power { get; set; }

    [Column("high_alpha_power")]
    public int? high_alpha_power { get; set; }

    [Column("low_beta_power")]
    public int? low_beta_power { get; set; }

    [Column("high_beta_power")]
    public int? high_beta_power { get; set; }

    [Column("low_gamma_power")]
    public int? low_gamma_power { get; set; }

    [Column("middle_gamma_power")]
    public int? middle_gamma_power { get; set; }

    [Column("attention_value")]
    public int? attention_value { get; set; }

    [Column("meditation_value")]
    public int? meditation_value { get; set; }

    [Column("raw_eeg_value")]
    public int? raw_eeg_value { get; set; }

    [Required]
    [Column("timestamp_of_record")]
    public DateTime timestamp_of_record { get; set; }

    [Column("latitude")]
    public double? latitude { get; set; }

    [Column("longitude")]
    public double? longitude { get; set; }

    [ForeignKey(nameof(session_id))]
    public virtual Sessions? session { get; set; }
}
