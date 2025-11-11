using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.SessionData;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Services.SessionData;

public class SessionDataService : ISessionDataService
{
    private readonly AppDbContext _context;

    public SessionDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateSessionData(
        SessionDataCreateDto sessionDataCreateDto
    )
    {
        try
        {
            var sessionData = new Models.SessionData
            {
                id = Guid.NewGuid(),
                session_id = sessionDataCreateDto.session_id,
                delta_power = sessionDataCreateDto.delta_power,
                theta_power = sessionDataCreateDto.theta_power,
                low_alpha_power = sessionDataCreateDto.low_alpha_power,
                high_alpha_power = sessionDataCreateDto.high_alpha_power,
                low_beta_power = sessionDataCreateDto.low_beta_power,
                high_beta_power = sessionDataCreateDto.high_beta_power,
                low_gamma_power = sessionDataCreateDto.low_gamma_power,
                middle_gamma_power = sessionDataCreateDto.middle_gamma_power,
                attention_value = sessionDataCreateDto.attention_value,
                meditation_value = sessionDataCreateDto.meditation_value,
                raw_eeg_value = sessionDataCreateDto.raw_eeg_value,
                timestamp_of_record = sessionDataCreateDto.timestamp_of_record,
                latitude = sessionDataCreateDto.latitude,
                longitude = sessionDataCreateDto.longitude,
            };

            await _context.session_data.AddAsync(sessionData);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Session data created successfully.",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return ResponseModel<string>.Fail($"Error creating session data: {ex.Message}", 500);
        }
    }

    public async Task<ResponseModel<List<SessionDataDto>>> GetSessionDataByPatientId(Guid patientId)
    {
        try
        {
            var sessionData = await _context
                .session_data.Include(sd => sd.session)
                .Where(sd => sd.session.PatientId == patientId)
                .AsNoTracking()
                .ToListAsync();

            var sessionDataDtos = sessionData
                .Select(sd => new SessionDataDto
                {
                    id = sd.id,
                    session_id = sd.session_id,
                    delta_power = sd.delta_power,
                    theta_power = sd.theta_power,
                    low_alpha_power = sd.low_alpha_power,
                    high_alpha_power = sd.high_alpha_power,
                    low_beta_power = sd.low_beta_power,
                    high_beta_power = sd.high_beta_power,
                    low_gamma_power = sd.low_gamma_power,
                    middle_gamma_power = sd.middle_gamma_power,
                    attention_value = sd.attention_value,
                    meditation_value = sd.meditation_value,
                    raw_eeg_value = sd.raw_eeg_value,
                    timestamp_of_record = sd.timestamp_of_record,
                    latitude = sd.latitude,
                    longitude = sd.longitude,
                })
                .ToList();

            return ResponseModel<List<SessionDataDto>>.Ok(
                sessionDataDtos,
                "Session data retrieved successfully",
                200
            );
        }
        catch (Exception ex)
        {
            return ResponseModel<List<SessionDataDto>>.Fail(
                $"Error retrieving session data: {ex.Message}",
                500
            );
        }
    }

    public async Task<ResponseModel<List<SessionDataDto>>> GetSessionDataBySessionId(Guid sessionId)
    {
        try
        {
            var sessionData = await _context
                .session_data.Include(sd => sd.session)
                .Where(sd => sd.session_id == sessionId)
                .AsNoTracking()
                .ToListAsync();

            var sessionDataDtos = sessionData
                .Select(sd => new SessionDataDto
                {
                    id = sd.id,
                    session_id = sd.session_id,
                    delta_power = sd.delta_power,
                    theta_power = sd.theta_power,
                    low_alpha_power = sd.low_alpha_power,
                    high_alpha_power = sd.high_alpha_power,
                    low_beta_power = sd.low_beta_power,
                    high_beta_power = sd.high_beta_power,
                    low_gamma_power = sd.low_gamma_power,
                    middle_gamma_power = sd.middle_gamma_power,
                    attention_value = sd.attention_value,
                    meditation_value = sd.meditation_value,
                    raw_eeg_value = sd.raw_eeg_value,
                    timestamp_of_record = sd.timestamp_of_record,
                    latitude = sd.latitude,
                    longitude = sd.longitude,
                })
                .ToList();

            return ResponseModel<List<SessionDataDto>>.Ok(
                sessionDataDtos,
                "Session data retrieved successfully",
                200
            );
        }
        catch (Exception ex)
        {
            return ResponseModel<List<SessionDataDto>>.Fail(
                $"Error retrieving session data: {ex.Message}",
                500
            );
        }
    }
}
