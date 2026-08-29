using System;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;

namespace FocusMapApi.Services.User;

public interface IUserService
{
    Task<ResponseModel<object>> CreateUser(CreateUserDto user);
    Task<ResponseModel<string>> UpdateUser(UpdateUserDto user, Guid id);
    Task<ResponseModel<string>> CreateUserPatient(CreatePatientDto patient, Guid userId);
    Task<ResponseModel<string>> UpdateUserPatient(UpdateUserDto patient, Guid id);
    Task<ResponseModel<List<ListPatientsDto>>> ListPatients(Guid id, string? searchTerm = null);
    Task<ResponseModel<object>> GoogleSignUp(GoogleAuthDto dto);
    Task<ResponseModel<object>> TotalPatients(Guid id);

    Task<ResponseModel<object>> GetById(Guid id);
    Task<ResponseModel<string>> DeletePatient(Guid patientId, Guid professionalId);

    // ── Admin: gestão de contas Professional ────────────────────────────────
    Task<ResponseModel<List<ListProfessionalsDto>>> ListProfessionals(string? searchTerm = null);
    Task<ResponseModel<string>> DeleteProfessional(Guid professionalId);
    Task<ResponseModel<string>> ResetProfessionalPassword(Guid professionalId);
}
