using System;
using FocusMapApi.DTO.Session;
using FocusMapApi.Models;

namespace FocusMapApi.Services.Sessions;

public interface ISessionService
{
    Task<ResponseModel<string>> CreateSessionAsync(SessionCreateDto sessionCreateDto);
    Task<ResponseModel<string>> UpdateSessionAsync(Guid id);
    Task<ResponseModel<SessionDto>> GetSessionByIdAsync(Guid id, Guid callerId, string callerRole);
    Task<ResponseModel<List<SessionDto>>> GetSessionsByPatientIdAsync(Guid patientId, Guid callerId, string callerRole);
    Task<ResponseModel<SessionPagedDto>> GetSessionsByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int pageSize,
        string? status,
        Guid? patientId,
        DateTime? dateFrom,
        DateTime? dateTo
    );
    Task<ResponseModel<SessionDashboardDto>> getSessionDashboardAsync(Guid id, Guid callerId, string callerRole);
}
