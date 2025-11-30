using System;

namespace FocusMapApi.DTO.User;

public class ListPatientsDto
{
    public Guid id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
}
