using System;
using FocusMapApi.DTO.SessionData;
using FocusMapApi.Models;

namespace FocusMapApi.Services.SessionData;

public interface ISessionDataService
{
    Task<ResponseModel<string>> CreateSessionData(SessionDataCreateDto sessionDataCreateDto);
    Task<ResponseModel<List<SessionDataDto>>> GetSessionDataBySessionId(Guid sessionId);
    Task<ResponseModel<List<SessionDataDto>>> GetSessionDataByPatientId(Guid patientId);
}
