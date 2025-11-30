using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FocusMapApi.Models;

public class PatientModel
{
    [Key]
    public Guid id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }

    [Column("professional_id")]
    public Guid? professional_id { get; set; }

    [ForeignKey("professional_id")]
    [JsonIgnore]
    public UsersModel Professional { get; set; }
}
