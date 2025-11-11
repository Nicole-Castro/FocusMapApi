using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FocusMapApi.Models;

public class Sessions
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [Column("session_start_time")]
    public DateTime SessionStartTime { get; set; }

    [Column("session_end_time")]
    public DateTime? SessionEndTime { get; set; }

    [Column("poor_signal")]
    public int? PoorSignal { get; set; }

    [Column("session_name")]
    public string? SessionName { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual PatientModel? Patient { get; set; }
}
