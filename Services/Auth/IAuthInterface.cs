using System;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;

namespace ACGSimBack.Services.Auth;

public interface IAuthInterface
{
    Task<ResponseModel<object>> Login(LoginDto loginDto);
    string GenerateJwtToken(object user);
    Task<ResponseModel<object>> GoogleLogin( GoogleLoginDto dto);
}
