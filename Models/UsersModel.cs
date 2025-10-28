using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FocusMapApi.Models;

public class UsersModel
{
    [Key]
    public Guid id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public DateTime registration_date { get; set; }
    public string? professional_license { get; set; }

    [JsonIgnore]
    public ICollection<PatientModel> patients { get; set; } = new List<PatientModel>();
}
