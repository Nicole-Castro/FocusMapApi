using System;

namespace FocusMapApi.DTO.InterestPoints;

public class InterestPointCreateDto
{
    public List<string> name { get; set; }
    public Guid patient_id { get; set; }
}
