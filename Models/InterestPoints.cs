using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FocusMapApi.Models;

public class InterestPoints
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid id { get; set; }
    public string name { get; set; }
    public Guid patient_id { get; set; }

    [ForeignKey("patient_id")]
    public PatientModel patient { get; set; }

    public bool is_deleted { get; set; } = false;
}
