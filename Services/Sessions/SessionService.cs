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
            var session = await _context
                .sessions.Include(s => s.Patient)
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

            // calcula duração
            string? formattedDuration = null;

            if (session.SessionEndTime.HasValue)
            {
                var duration = session.SessionEndTime.Value - session.SessionStartTime;

                // FORMATO BONITO: "1h 23m 45s"
                if (duration.TotalHours >= 1)
                    formattedDuration =
                        $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
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
                session_duration = formattedDuration, // <<< agora é string formatada
                session_name = session.SessionName,
                patient_name = session.Patient.name,
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
            var session = await _context
                .sessions.Include(s => s.Patient)
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

            var sessionData = await _context
                .session_data.Where(sd => sd.session_id == id)
                .ToListAsync();

            var audioData = await _context
                .audio_description.Where(ad => ad.session_id == id)
                .ToListAsync();

            var sessionDashboardDto = new SessionDashboardDto
            {
                AudioTopics = audioData
                    .Select(ad => new AudioTopicDto
                    {
                        Id = ad.id,
                        StartOfAudio = ad.start_of_audio,
                        EndOfAudio = ad.end_of_audio,
                        Description = ad.description,
                        PointOfInterestId = ad.id_point_of_interest,
                    })
                    .ToList(),

                Eeg = sessionData
                    .Select(sd => new EegDataDto
                    {
                        AttentionValue = sd.attention_value,
                        DeltaPower = sd.delta_power,
                        HighAlphaPower = sd.high_alpha_power,
                        HighBetaPower = sd.high_beta_power,
                        LowAlphaPower = sd.low_alpha_power,
                        LowBetaPower = sd.low_beta_power,
                        LowGammaPower = sd.low_gamma_power,
                        MeditationValue = sd.meditation_value,
                        MiddleGammaPower = sd.middle_gamma_power,
                        RawEegValue = sd.raw_eeg_value,
                        ThetaPower = sd.theta_power,
                        Timestamp = sd.timestamp_of_record,
                        Latitude = sd.latitude,
                        Longitude = sd.longitude,
                    })
                    .ToList(),

                Session = new SessionDto
                {
                    id = session.Id,
                    patient_id = session.PatientId,
                    session_start_time = session.SessionStartTime,
                    session_end_time = session.SessionEndTime,
                    session_name = session.SessionName,
                    patient_name = session.Patient?.name,
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
            var sessions = await _context
                .sessions.AsNoTracking()
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

    public async Task<ResponseModel<List<SessionDto>>> GetSessionsByProfessionalIdAsync(
        Guid professionalId
    )
    {
        try
        {
            var sessions = await _context
                .sessions.AsNoTracking()
                .Include(s => s.Patient)
                .Where(s => s.Patient.professional_id == professionalId)
                .Select(s => new SessionDto
                {
                    id = s.Id,
                    patient_id = s.PatientId,
                    session_start_time = s.SessionStartTime,
                    session_end_time = s.SessionEndTime,
                    session_name = s.SessionName,
                    patient_name = s.Patient.name,
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
