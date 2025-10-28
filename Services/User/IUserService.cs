using System;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;

namespace FocusMapApi.Services.User;

public interface IUserService
{
    Task<string> CreateUser(CreateUserDto user);
    Task<string> UpdateUser(UpdateUserDto user, Guid id);
    Task<bool> CreateUserPatient(CreatePatientDto patient, Guid userId);
    Task<string> UpdateUserPatient(UpdateUserDto patient, Guid id);
}
