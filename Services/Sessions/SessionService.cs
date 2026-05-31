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
            var session = new SessionModel
            {
                PatientId = sessionCreateDto.patient_id,
                SessionStartTime = DateTime.UtcNow,
                SessionName = "session-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            };

            await _context.Sessions.AddAsync(session);
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
            var session = await _context.Sessions
                .Include(s => s.Patient)
                .Where(s => s.Id == id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

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

            string? formattedDuration = null;

            if (session.SessionEndTime.HasValue)
            {
                var duration = session.SessionEndTime.Value - session.SessionStartTime;

                if (duration.TotalHours >= 1)
                    formattedDuration = $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
                else if (duration.TotalMinutes >= 1)
                    formattedDuration = $"{duration.Minutes}m {duration.Seconds}s";
                else
                    formattedDuration = $"{duration.Seconds}s";
            }

            var sessionDto = new SessionDto
            {
                id = session.Id,
                patient_id = session.PatientId,
                session_start_time = session.SessionStartTime,
                session_end_time = session.SessionEndTime,
                session_duration = formattedDuration,
                session_name = session.SessionName,
                patient_name = session.Patient.Name,
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

    public async Task<ResponseModel<SessionDashboardDto>> getSessionDashboardAsync(Guid id)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Patient)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return new ResponseModel<SessionDashboardDto>
                {
                    Success = false,
                    Message = "Session not found",
                    StatusCode = 404,
                    Data = null,
                };
            }

            var sessionData = await _context.SessionData
                .Where(sd => sd.SessionId == id)
                .ToListAsync();

            var audioData = await _context.AudioDescriptions
                .Where(ad => ad.SessionId == id)
                .ToListAsync();

            var sessionDashboardDto = new SessionDashboardDto
            {
                AudioTopics = audioData
                    .Select(ad => new AudioTopicDto
                    {
                        Id = ad.Id,
                        StartOfAudio = ad.StartOfAudio,
                        EndOfAudio = ad.EndOfAudio,
                        Description = ad.Description,
                        PointOfInterestId = ad.InterestPointId,
                    })
                    .ToList(),

                Eeg = sessionData
                    .Select(sd => new EegDataDto
                    {
                        AttentionValue = sd.AttentionValue,
                        DeltaPower = sd.DeltaPower,
                        HighAlphaPower = sd.HighAlphaPower,
                        HighBetaPower = sd.HighBetaPower,
                        LowAlphaPower = sd.LowAlphaPower,
                        LowBetaPower = sd.LowBetaPower,
                        LowGammaPower = sd.LowGammaPower,
                        MeditationValue = sd.MeditationValue,
                        MiddleGammaPower = sd.MiddleGammaPower,
                        RawEegValue = sd.RawEegValue,
                        ThetaPower = sd.ThetaPower,
                        Timestamp = sd.TimestampOfRecord,
                        Latitude = sd.Latitude,
                        Longitude = sd.Longitude,
                    })
                    .ToList(),

                Session = new SessionDto
                {
                    id = session.Id,
                    patient_id = session.PatientId,
                    session_start_time = session.SessionStartTime,
                    session_end_time = session.SessionEndTime,
                    session_name = session.SessionName,
                    patient_name = session.Patient?.Name,
                },
            };

            return new ResponseModel<SessionDashboardDto>
            {
                Success = true,
                Message = "Session retrieved successfully",
                StatusCode = 200,
                Data = sessionDashboardDto,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<SessionDashboardDto>
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
            var sessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.PatientId == patientId)
                .Select(s => new SessionDto
                {
                    id = s.Id,
                    patient_id = s.PatientId,
                    session_start_time = s.SessionStartTime,
                    session_end_time = s.SessionEndTime,
                    session_name = s.SessionName,
                })
                .ToListAsync();

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

    public async Task<ResponseModel<List<SessionDto>>> GetSessionsByProfessionalIdAsync(Guid professionalId)
    {
        try
        {
            var sessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Patient)
                .Where(s => s.Patient.ProfessionalId == professionalId)
                .Select(s => new SessionDto
                {
                    id = s.Id,
                    patient_id = s.PatientId,
                    session_start_time = s.SessionStartTime,
                    session_end_time = s.SessionEndTime,
                    session_name = s.SessionName,
                    patient_name = s.Patient.Name,
                })
                .ToListAsync();

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

    public async Task<ResponseModel<string>> UpdateSessionAsync(Guid id)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(id);
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
            _context.Sessions.Update(session);
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
