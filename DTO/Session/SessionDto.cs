using System;

namespace FocusMapApi.DTO.Session;

public class SessionDto
{
    public Guid id { get; set; }
    public Guid patient_id { get; set; }
    public DateTime session_start_time { get; set; }
    public DateTime? session_end_time { get; set; }
    public string? session_duration { get; set; }
    public string patient_name { get; set; }
    public string session_name { get; set; }
}
