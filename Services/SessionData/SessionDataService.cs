using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.SessionData;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Services.SessionData;

public class SessionDataService : ISessionDataService
{
    private readonly AppDbContext _context;
    private const double LocationClusterRadiusDeg = 0.0005;

    public SessionDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateSessionData(SessionDataCreateDto sessionDataCreateDto)
    {
        try
        {
            var sessionData = new SessionDataModel
            {
                Id = Guid.NewGuid(),
                SessionId = sessionDataCreateDto.session_id,
                DeltaPower = sessionDataCreateDto.delta_power,
                ThetaPower = sessionDataCreateDto.theta_power,
                LowAlphaPower = sessionDataCreateDto.low_alpha_power,
                HighAlphaPower = sessionDataCreateDto.high_alpha_power,
                LowBetaPower = sessionDataCreateDto.low_beta_power,
                HighBetaPower = sessionDataCreateDto.high_beta_power,
                LowGammaPower = sessionDataCreateDto.low_gamma_power,
                MiddleGammaPower = sessionDataCreateDto.middle_gamma_power,
                AttentionValue = sessionDataCreateDto.attention_value,
                MeditationValue = sessionDataCreateDto.meditation_value,
                RawEegValue = sessionDataCreateDto.raw_eeg_value,
                TimestampOfRecord = sessionDataCreateDto.timestamp_of_record,
                Latitude = sessionDataCreateDto.latitude,
                Longitude = sessionDataCreateDto.longitude,
            };

            await _context.SessionData.AddAsync(sessionData);
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
            var sessionData = await _context.SessionData
                .Include(sd => sd.Session)
                .Where(sd => sd.Session.PatientId == patientId)
                .AsNoTracking()
                .ToListAsync();

            var sessionDataDtos = sessionData
                .Select(sd => new SessionDataDto
                {
                    id = sd.Id,
                    session_id = sd.SessionId,
                    delta_power = sd.DeltaPower,
                    theta_power = sd.ThetaPower,
                    low_alpha_power = sd.LowAlphaPower,
                    high_alpha_power = sd.HighAlphaPower,
                    low_beta_power = sd.LowBetaPower,
                    high_beta_power = sd.HighBetaPower,
                    low_gamma_power = sd.LowGammaPower,
                    middle_gamma_power = sd.MiddleGammaPower,
                    attention_value = sd.AttentionValue,
                    meditation_value = sd.MeditationValue,
                    raw_eeg_value = sd.RawEegValue,
                    timestamp_of_record = sd.TimestampOfRecord,
                    latitude = sd.Latitude,
                    longitude = sd.Longitude,
                })
                .ToList();

            return ResponseModel<List<SessionDataDto>>.Ok(sessionDataDtos, "Session data retrieved successfully", 200);
        }
        catch (Exception ex)
        {
            return ResponseModel<List<SessionDataDto>>.Fail($"Error retrieving session data: {ex.Message}", 500);
        }
    }

    public async Task<ResponseModel<List<SessionDataDto>>> GetSessionDataBySessionId(Guid sessionId)
    {
        try
        {
            var sessionData = await _context.SessionData
                .Include(sd => sd.Session)
                .Where(sd => sd.SessionId == sessionId)
                .AsNoTracking()
                .ToListAsync();

            var sessionDataDtos = sessionData
                .Select(sd => new SessionDataDto
                {
                    id = sd.Id,
                    session_id = sd.SessionId,
                    delta_power = sd.DeltaPower,
                    theta_power = sd.ThetaPower,
                    low_alpha_power = sd.LowAlphaPower,
                    high_alpha_power = sd.HighAlphaPower,
                    low_beta_power = sd.LowBetaPower,
                    high_beta_power = sd.HighBetaPower,
                    low_gamma_power = sd.LowGammaPower,
                    middle_gamma_power = sd.MiddleGammaPower,
                    attention_value = sd.AttentionValue,
                    meditation_value = sd.MeditationValue,
                    raw_eeg_value = sd.RawEegValue,
                    timestamp_of_record = sd.TimestampOfRecord,
                    latitude = sd.Latitude,
                    longitude = sd.Longitude,
                })
                .ToList();

            return ResponseModel<List<SessionDataDto>>.Ok(sessionDataDtos, "Session data retrieved successfully", 200);
        }
        catch (Exception ex)
        {
            return ResponseModel<List<SessionDataDto>>.Fail($"Error retrieving session data: {ex.Message}", 500);
        }
    }

    public async Task<ResponseModel<PatientProgressDto>> GetPatientProgress(Guid patientId)
    {
        try
        {
            var patient = await _context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                return ResponseModel<PatientProgressDto>.Fail("Patient not found.", 404);

            var sessions = await _context.Sessions
                .Where(s => s.PatientId == patientId)
                .OrderBy(s => s.SessionStartTime)
                .AsNoTracking()
                .ToListAsync();

            if (!sessions.Any())
            {
                return ResponseModel<PatientProgressDto>.Ok(
                    new PatientProgressDto
                    {
                        patient_id = patientId,
                        patient_name = patient.Name,
                        total_sessions = 0,
                    },
                    "No sessions found for this patient.",
                    200
                );
            }

            var sessionIds = sessions.Select(s => s.Id).ToList();

            var allEeg = await _context.SessionData
                .Where(sd => sessionIds.Contains(sd.SessionId))
                .AsNoTracking()
                .ToListAsync();

            var allPoi = await _context.InterestPoints
                .Where(p => p.PatientId == patientId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            var allAudio = await _context.AudioDescriptions
                .Where(a => sessionIds.Contains(a.SessionId))
                .OrderBy(a => a.StartOfAudio)
                .AsNoTracking()
                .ToListAsync();

            var sessionSummaries = new List<SessionSummaryDto>();

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];

                var eeg = allEeg.Where(e => e.SessionId == session.Id).ToList();
                var audio = allAudio.Where(a => a.SessionId == session.Id).ToList();

                var attValues = eeg.Where(e => e.AttentionValue.HasValue)
                    .Select(e => e.AttentionValue!.Value)
                    .ToList();

                var medValues = eeg.Where(e => e.MeditationValue.HasValue)
                    .Select(e => e.MeditationValue!.Value)
                    .ToList();

                var poiIdsFromAudio = audio
                    .Where(a => a.InterestPointId.HasValue)
                    .Select(a => a.InterestPointId!.Value)
                    .Distinct()
                    .ToList();

                var poi = allPoi.Where(p => poiIdsFromAudio.Contains(p.Id)).ToList();

                var coords = eeg.Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
                    .Select(e => (lat: e.Latitude!.Value, lng: e.Longitude!.Value))
                    .ToList();

                double? sessionLat = coords.Any() ? coords.Average(c => c.lat) : null;
                double? sessionLng = coords.Any() ? coords.Average(c => c.lng) : null;

                int? firstHalfAvg = null;
                int? secondHalfAvg = null;
                int? trendDiff = null;

                if (attValues.Count >= 10)
                {
                    int half = attValues.Count / 2;
                    firstHalfAvg = (int)Math.Round(attValues.Take(half).Average());
                    secondHalfAvg = (int)Math.Round(attValues.Skip(half).Average());
                    trendDiff = secondHalfAvg - firstHalfAvg;
                }

                var distribution = new AttentionDistributionDto
                {
                    low_count = attValues.Count(v => v < 25),
                    medium_low_count = attValues.Count(v => v >= 25 && v < 50),
                    medium_count = attValues.Count(v => v >= 50 && v < 75),
                    high_count = attValues.Count(v => v >= 75),
                };

                var audioSummaries = audio
                    .Select(a =>
                    {
                        var audioEeg = eeg.Where(e =>
                                e.TimestampOfRecord >= a.StartOfAudio
                                && e.TimestampOfRecord <= a.EndOfAudio)
                            .ToList();

                        var audioAtt = audioEeg.Where(e => e.AttentionValue.HasValue)
                            .Select(e => e.AttentionValue!.Value)
                            .ToList();

                        var audioMed = audioEeg.Where(e => e.MeditationValue.HasValue)
                            .Select(e => e.MeditationValue!.Value)
                            .ToList();

                        return new AudioTopicSummaryDto
                        {
                            id = a.Id,
                            description = a.Description,
                            start_of_audio = a.StartOfAudio,
                            end_of_audio = a.EndOfAudio,
                            avg_attention = audioAtt.Any() ? (int?)Math.Round(audioAtt.Average()) : null,
                            avg_meditation = audioMed.Any() ? (int?)Math.Round(audioMed.Average()) : null,
                        };
                    })
                    .ToList();

                var poiSummaries = poi.Select(p => new PointOfInterestSummaryDto
                    {
                        id = p.Id,
                        name = p.Name,
                        avg_attention_in_range = attValues.Any() ? (int?)Math.Round(attValues.Average()) : null,
                        avg_meditation_in_range = medValues.Any() ? (int?)Math.Round(medValues.Average()) : null,
                    })
                    .ToList();

                int? duration = null;

                if (session.SessionEndTime.HasValue)
                {
                    duration = (int)(session.SessionEndTime.Value - session.SessionStartTime).TotalSeconds;
                }

                sessionSummaries.Add(new SessionSummaryDto
                {
                    session_id = session.Id,
                    session_index = i + 1,
                    session_start = session.SessionStartTime,
                    session_end = session.SessionEndTime,
                    duration_seconds = duration,
                    avg_attention = attValues.Any() ? (int?)Math.Round(attValues.Average()) : null,
                    avg_meditation = medValues.Any() ? (int?)Math.Round(medValues.Average()) : null,
                    max_attention = attValues.Any() ? attValues.Max() : null,
                    min_attention = attValues.Any() ? attValues.Min() : null,
                    eeg_record_count = eeg.Count,
                    latitude = sessionLat,
                    longitude = sessionLng,
                    location_label = null,
                    points_of_interest = poiSummaries,
                    audio_topics = audioSummaries,
                    attention_distribution = distribution,
                    first_half_avg_attention = firstHalfAvg,
                    second_half_avg_attention = secondHalfAvg,
                    attention_trend_diff = trendDiff,
                });
            }

            var locationGroups = BuildLocationGroups(sessionSummaries);
            foreach (var group in locationGroups)
            {
                foreach (var sessionId in group.session_ids)
                {
                    var summary = sessionSummaries.FirstOrDefault(s => s.session_id == sessionId);
                    if (summary != null)
                        summary.location_label = group.location_label;
                }
            }

            var locationAttention = locationGroups
                .Select(g => new LocationAttentionDto
                {
                    location_label = g.location_label,
                    representative_latitude = g.representative_latitude,
                    representative_longitude = g.representative_longitude,
                    session_count = g.session_ids.Count,
                    avg_attention = g.avg_attention,
                    avg_meditation = g.avg_meditation,
                })
                .ToList();

            var hourlyAttention = BuildHourlyAttention(allEeg);

            var trend = sessionSummaries
                .Select(s => new SessionTrendPointDto
                {
                    session_index = s.session_index,
                    session_id = s.session_id,
                    session_date = s.session_start,
                    avg_attention = s.avg_attention,
                    avg_meditation = s.avg_meditation,
                })
                .ToList();

            var allAttValues = allEeg.Where(e => e.AttentionValue.HasValue)
                .Select(e => e.AttentionValue!.Value)
                .ToList();

            var allMedValues = allEeg.Where(e => e.MeditationValue.HasValue)
                .Select(e => e.MeditationValue!.Value)
                .ToList();

            var result = new PatientProgressDto
            {
                patient_id = patientId,
                patient_name = patient.Name,
                first_session_date = sessions.First().SessionStartTime,
                last_session_date = sessions.Last().SessionStartTime,
                total_sessions = sessions.Count,
                overall_avg_attention = allAttValues.Any() ? (int?)Math.Round(allAttValues.Average()) : null,
                overall_avg_meditation = allMedValues.Any() ? (int?)Math.Round(allMedValues.Average()) : null,
                overall_max_attention = allAttValues.Any() ? allAttValues.Max() : null,
                overall_min_attention = allAttValues.Any() ? allAttValues.Min() : null,
                sessions = sessionSummaries,
                attention_by_location = locationAttention,
                attention_by_hour = hourlyAttention,
                attention_trend = trend,
            };

            return ResponseModel<PatientProgressDto>.Ok(result, "Patient progress retrieved successfully.", 200);
        }
        catch (Exception ex)
        {
            return ResponseModel<PatientProgressDto>.Fail($"Error retrieving patient progress: {ex.Message}", 500);
        }
    }

    // ─── Helpers privados ─────────────────────────────────────────────────────

    private record LocationGroup(
        string location_label,
        double representative_latitude,
        double representative_longitude,
        List<Guid> session_ids,
        int? avg_attention,
        int? avg_meditation
    );

    private List<LocationGroup> BuildLocationGroups(List<SessionSummaryDto> sessions)
    {
        var groups = new List<(double lat, double lng, List<SessionSummaryDto> items)>();

        foreach (var session in sessions)
        {
            if (!session.latitude.HasValue || !session.longitude.HasValue)
                continue;

            double lat = session.latitude.Value;
            double lng = session.longitude.Value;

            var existing = groups.FirstOrDefault(g =>
                Math.Sqrt(Math.Pow(g.lat - lat, 2) + Math.Pow(g.lng - lng, 2)) <= LocationClusterRadiusDeg
            );

            if (existing == default)
                groups.Add((lat, lng, new List<SessionSummaryDto> { session }));
            else
                existing.items.Add(session);
        }

        var result = new List<LocationGroup>();
        int labelIndex = 1;

        foreach (var g in groups)
        {
            var attVals = g.items.Where(s => s.avg_attention.HasValue)
                .Select(s => s.avg_attention!.Value).ToList();
            var medVals = g.items.Where(s => s.avg_meditation.HasValue)
                .Select(s => s.avg_meditation!.Value).ToList();

            result.Add(new LocationGroup(
                location_label: $"Local {labelIndex++}",
                representative_latitude: g.items.Average(s => s.latitude ?? 0),
                representative_longitude: g.items.Average(s => s.longitude ?? 0),
                session_ids: g.items.Select(s => s.session_id).ToList(),
                avg_attention: attVals.Any() ? (int?)Math.Round(attVals.Average()) : null,
                avg_meditation: medVals.Any() ? (int?)Math.Round(medVals.Average()) : null
            ));
        }

        var noCoord = sessions.Where(s => !s.latitude.HasValue).ToList();
        if (noCoord.Any())
        {
            var attVals = noCoord.Where(s => s.avg_attention.HasValue).Select(s => s.avg_attention!.Value).ToList();
            var medVals = noCoord.Where(s => s.avg_meditation.HasValue).Select(s => s.avg_meditation!.Value).ToList();
            result.Add(new LocationGroup(
                location_label: "Local não registrado",
                representative_latitude: 0,
                representative_longitude: 0,
                session_ids: noCoord.Select(s => s.session_id).ToList(),
                avg_attention: attVals.Any() ? (int?)Math.Round(attVals.Average()) : null,
                avg_meditation: medVals.Any() ? (int?)Math.Round(medVals.Average()) : null
            ));
        }

        return result;
    }

    private List<HourlyAttentionDto> BuildHourlyAttention(List<SessionDataModel> eeg)
    {
        return eeg.Where(e => e.AttentionValue.HasValue)
            .GroupBy(e => e.TimestampOfRecord.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new HourlyAttentionDto
            {
                hour_start = g.Key,
                hour_range = $"{g.Key:D2}h–{g.Key + 1:D2}h",
                avg_attention = (int)Math.Round(g.Average(e => e.AttentionValue!.Value)),
                record_count = g.Count(),
            })
            .ToList();
    }
}
