using System;

namespace FocusMapApi.DTO.User;

public class CreateUserDto
{
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
}
