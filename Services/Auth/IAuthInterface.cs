using System;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;

namespace ACGSimBack.Services.Auth;

public interface IAuthInterface
{
    Task<ResponseModel<object>> Login(LoginDto loginDto);
}
