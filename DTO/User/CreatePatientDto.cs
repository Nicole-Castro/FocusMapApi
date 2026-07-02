using System;

namespace FocusMapApi.DTO.User;

public class CreatePatientDto
{
    public string name { get; set; }
    public string email { get; set; }
    public string? password { get; set; }
}
