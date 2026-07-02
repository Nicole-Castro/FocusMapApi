using System;
using Newtonsoft.Json;

namespace FocusMapApi.DTO.User;

public class CreateUserDto
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }
}
