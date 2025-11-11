using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FocusMapApi.Models;

public class AudioDescription
{
    [Key]
    [Column("id")]
    public Guid id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid session_id { get; set; }

    [Required]
    [Column("start_of_audio")]
    public DateTime start_of_audio { get; set; }

    [Required]
    [Column("end_of_audio")]
    public DateTime end_of_audio { get; set; }

    [Column("description")]
    public string? description { get; set; }

    [Column("id_point_of_interest")]
    public Guid? id_point_of_interest { get; set; }

    [ForeignKey(nameof(session_id))]
    public virtual Sessions? session { get; set; }

    [ForeignKey(nameof(id_point_of_interest))]
    public virtual InterestPoints? point_of_interest { get; set; }
}
