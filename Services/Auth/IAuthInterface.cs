using System;
using FocusMapApi.DTO.User;

namespace ACGSimBack.Services.Auth;

public interface IAuthInterface
{
    Task<object> Login(LoginDto loginDto);
}
