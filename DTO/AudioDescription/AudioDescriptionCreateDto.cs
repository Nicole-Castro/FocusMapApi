using System;

namespace FocusMapApi.DTO.AudioDescription;

public class AudioDescriptionCreateDto
{
    public Guid session_id { get; set; }
    public DateTime start_of_audio { get; set; }
    public DateTime end_of_audio { get; set; }
    public string? description { get; set; }
    public Guid? id_point_of_interest { get; set; }
}
