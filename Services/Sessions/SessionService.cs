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

    // Só o Admin, o próprio paciente da sessão, ou o profissional dono do paciente
    // podem ver a sessão. Sem isso, qualquer usuário autenticado veria a sessão de
    // qualquer outra pessoa só sabendo o GUID.
    private static bool CanAccessPatientData(Guid patientId, Guid? patientsProfessionalId, Guid callerId, string callerRole) =>
        callerRole == "Admin" || callerId == patientId || (patientsProfessionalId.HasValue && callerId == patientsProfessionalId.Value);

    public async Task<ResponseModel<SessionDto>> GetSessionByIdAsync(Guid id, Guid callerId, string callerRole)
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

            if (!CanAccessPatientData(session.PatientId, session.Patient?.ProfessionalId, callerId, callerRole))
            {
                return new ResponseModel<SessionDto>
                {
                    Success = false,
                    Message = "Acesso negado.",
                    StatusCode = 403,
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

    public async Task<ResponseModel<SessionDashboardDto>> getSessionDashboardAsync(Guid id, Guid callerId, string callerRole)
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

            if (!CanAccessPatientData(session.PatientId, session.Patient?.ProfessionalId, callerId, callerRole))
            {
                return new ResponseModel<SessionDashboardDto>
                {
                    Success = false,
                    Message = "Acesso negado.",
                    StatusCode = 403,
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

    public async Task<ResponseModel<List<SessionDto>>> GetSessionsByPatientIdAsync(Guid patientId, Guid callerId, string callerRole)
    {
        try
        {
            if (callerRole != "Admin" && callerId != patientId)
            {
                var patient = await _context.Profiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                    return new ResponseModel<List<SessionDto>>
                    {
                        Success = false,
                        Message = "Patient not found",
                        StatusCode = 404,
                        Data = null,
                    };

                if (patient.ProfessionalId != callerId)
                    return new ResponseModel<List<SessionDto>>
                    {
                        Success = false,
                        Message = "Acesso negado.",
                        StatusCode = 403,
                        Data = null,
                    };
            }

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

    public async Task<ResponseModel<SessionPagedDto>> GetSessionsByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int pageSize,
        string? status,
        Guid? patientId,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        try
        {
            // Query base sem Include — usada para counts (EF ignora Include em COUNT)
            var baseQuery = _context.Sessions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.Patient != null && s.Patient.ProfessionalId == professionalId && !s.Patient.IsDeleted);

            // Aplica filtros de paciente e data na base (sem filtro de status)
            if (patientId.HasValue)
                baseQuery = baseQuery.Where(s => s.PatientId == patientId.Value);
            if (dateFrom.HasValue)
            {
                var from = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(s => s.SessionStartTime >= from);
            }
            if (dateTo.HasValue)
            {
                var to = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
                baseQuery = baseQuery.Where(s => s.SessionStartTime < to);
            }

            // Totais agregados independem do filtro de status
            var totalFinished   = await baseQuery.CountAsync(s => s.SessionEndTime != null);
            var totalInProgress = await baseQuery.CountAsync(s => s.SessionEndTime == null);
            var totalCount      = totalFinished + totalInProgress;

            // Query para itens — aplica também filtro de status
            var query = baseQuery;
            if (status == "finished")
                query = query.Where(s => s.SessionEndTime != null);
            else if (status == "in_progress")
                query = query.Where(s => s.SessionEndTime == null);

            var filteredCount = (status == "finished") ? totalFinished
                              : (status == "in_progress") ? totalInProgress
                              : totalCount;
            var totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize);

            var items = await query
                .Include(s => s.Patient)
                .OrderByDescending(s => s.SessionStartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new ResponseModel<SessionPagedDto>
            {
                Success = true,
                Message = "Sessions retrieved successfully",
                StatusCode = 200,
                Data = new SessionPagedDto
                {
                    Items = items,
                    TotalCount = filteredCount,
                    TotalFinished = totalFinished,
                    TotalInProgress = totalInProgress,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                },
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<SessionPagedDto>
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
