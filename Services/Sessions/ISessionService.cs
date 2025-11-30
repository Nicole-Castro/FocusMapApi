using System;
using FocusMapApi.DTO.Session;
using FocusMapApi.Models;

namespace FocusMapApi.Services.Sessions;

public interface ISessionService
{
    Task<ResponseModel<string>> CreateSessionAsync(SessionCreateDto sessionCreateDto);
    Task<ResponseModel<string>> UpdateSessionAsync(Guid id);
    Task<ResponseModel<SessionDto>> GetSessionByIdAsync(Guid id);
    Task<ResponseModel<List<SessionDto>>> GetSessionsByPatientIdAsync(Guid patientId);
    Task<ResponseModel<List<SessionDto>>> GetSessionsByProfessionalIdAsync(Guid professionalId);
}
