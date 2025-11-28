using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.Session;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FocusMapApi.Services.Sessions;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateSessionAsync(SessionCreateDto sessionCreateDto)
    {
        try
        {
            var session = new Models.Sessions
            {
                PatientId = sessionCreateDto.patient_id,
                SessionStartTime = DateTime.UtcNow,
                SessionName = "session-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            };

            await _context.sessions.AddAsync(session);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Session created successfully",
                StatusCode = 201,
                Data = session.Id.ToString(),
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Error creating session: {ex.Message}",
                Data = null,
            };
        }
    }

    public async Task<ResponseModel<SessionDto>> GetSessionByIdAsync(Guid id)
    {
        try
        {
            var session = await _context.sessions.FindAsync(id);
            if (session == null)
            {
                return new ResponseModel<SessionDto>
                {
                    Success = false,
                    Message = "Session not found",
                    StatusCode = 404,
                    Data = null,
                };
            }

            var sessionDto = new SessionDto
            {
                id = session.Id,
                patient_id = session.PatientId,
                session_start_time = session.SessionStartTime,
                session_end_time = session.SessionEndTime,
                session_name = session.SessionName,
            };

            return new ResponseModel<SessionDto>
            {
                Success = true,
                Message = "Session retrieved successfully",
                StatusCode = 200,
                Data = sessionDto,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<SessionDto>
            {
                Success = false,
                Message = $"Error retrieving session: {ex.Message}",
                StatusCode = 500,
                Data = null,
            };
        }
    }

    public async Task<ResponseModel<List<SessionDto>>> GetSessionsByPatientIdAsync(Guid patientId)
    {
        try
        {
            var sessions = _context
                .sessions.Where(s => s.PatientId == patientId)
                .Select(s => new SessionDto
                {
                    id = s.Id,
                    patient_id = s.PatientId,
                    session_start_time = s.SessionStartTime,
                    session_end_time = s.SessionEndTime,
                    session_name = s.SessionName,
                })
                .ToList();

            return new ResponseModel<List<SessionDto>>
            {
                Success = true,
                Message = "Sessions retrieved successfully",
                StatusCode = 200,
                Data = sessions,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<List<SessionDto>>
            {
                Success = false,
                Message = $"Error retrieving sessions: {ex.Message}",
                StatusCode = 500,
                Data = null,
            };
        }
    }

    public async Task<ResponseModel<List<SessionDto>>> GetSessionsByProfessionalIdAsync(
        Guid professionalId
    )
    {
        try
        {
            var sessions = _context
                .sessions.Include(s => s.Patient)
                .Where(s => s.Patient.professional_id == professionalId)
                .Select(s => new SessionDto
                {
                    id = s.Id,
                    patient_id = s.PatientId,
                    session_start_time = s.SessionStartTime,
                    session_end_time = s.SessionEndTime,
                    session_name = s.SessionName,
                })
                .AsNoTracking()
                .ToList();
            return new ResponseModel<List<SessionDto>>
            {
                Success = true,
                Message = "Sessions retrieved successfully",
                StatusCode = 200,
                Data = sessions,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<List<SessionDto>>
            {
                Success = false,
                Message = $"Error retrieving sessions: {ex.Message}",
                StatusCode = 500,
                Data = null,
            };
        }
    }

    public async Task<ResponseModel<string>> UpdateSessionAsync(
        SessionUpdateDto sessionUpdateDto,
        Guid id
    )
    {
        try
        {
            var session = await _context.sessions.FindAsync(id);
            if (session == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Session not found",
                    StatusCode = 404,
                };
            }

            session.SessionEndTime = DateTime.UtcNow;
            _context.sessions.Update(session);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Session updated successfully",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Error updating session: {ex.Message}",
                StatusCode = 500,
            };
        }
    }
}
