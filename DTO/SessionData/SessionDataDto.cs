using System;

namespace FocusMapApi.DTO.SessionData;

public class SessionDataDto
{
    public Guid id { get; set; }
    public Guid session_id { get; set; }

    public int? delta_power { get; set; }
    public int? theta_power { get; set; }
    public int? low_alpha_power { get; set; }
    public int? high_alpha_power { get; set; }
    public int? low_beta_power { get; set; }
    public int? high_beta_power { get; set; }
    public int? low_gamma_power { get; set; }
    public int? middle_gamma_power { get; set; }

    public int? attention_value { get; set; }
    public int? meditation_value { get; set; }
    public int? raw_eeg_value { get; set; }

    public DateTime timestamp_of_record { get; set; }

    public double? latitude { get; set; }
    public double? longitude { get; set; }
}

// ─── Resumo geral do paciente ─────────────────────────────────────────────────
public class PatientProgressDto
{
    public Guid patient_id { get; set; }
    public string patient_name { get; set; } = string.Empty;
    public DateTime? first_session_date { get; set; }
    public DateTime? last_session_date { get; set; }

    // Métricas agregadas de todas as sessões
    public int total_sessions { get; set; }
    public int? overall_avg_attention { get; set; }
    public int? overall_avg_meditation { get; set; }
    public int? overall_max_attention { get; set; }
    public int? overall_min_attention { get; set; }

    // Lista de sessões com seus agregados
    public List<SessionSummaryDto> sessions { get; set; } = new();

    // Atenção média agrupada por local (lat/lng cluster → label)
    public List<LocationAttentionDto> attention_by_location { get; set; } = new();

    // Atenção média agrupada por faixa de horário
    public List<HourlyAttentionDto> attention_by_hour { get; set; } = new();

    // Evolução: um ponto por sessão (para o gráfico de linha)
    public List<SessionTrendPointDto> attention_trend { get; set; } = new();
}

// ─── Resumo de cada sessão ────────────────────────────────────────────────────
public class SessionSummaryDto
{
    public Guid session_id { get; set; }
    public int session_index { get; set; } // 1, 2, 3…
    public DateTime? session_start { get; set; }
    public DateTime? session_end { get; set; }
    public int? duration_seconds { get; set; }

    // Métricas EEG da sessão
    public int? avg_attention { get; set; }
    public int? avg_meditation { get; set; }
    public int? max_attention { get; set; }
    public int? min_attention { get; set; }
    public int eeg_record_count { get; set; }

    // Localização predominante da sessão (moda de lat/lng)
    public double? latitude { get; set; }
    public double? longitude { get; set; }
    public string? location_label { get; set; } // "Escola", "Clínica" etc. (se cadastrado)

    // Pontos de interesse vinculados
    public List<PointOfInterestSummaryDto> points_of_interest { get; set; } = new();

    // Tópicos de áudio detectados
    public List<AudioTopicSummaryDto> audio_topics { get; set; } = new();

    // Atenção distribuída por faixa
    public AttentionDistributionDto attention_distribution { get; set; } = new();

    // Comparação 1ª vs 2ª metade
    public int? first_half_avg_attention { get; set; }
    public int? second_half_avg_attention { get; set; }
    public int? attention_trend_diff { get; set; } // second - first
}

// ─── Ponto de interesse resumido ─────────────────────────────────────────────
public class PointOfInterestSummaryDto
{
    public Guid id { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int? avg_attention_in_range { get; set; }
    public int? avg_meditation_in_range { get; set; }
}

// ─── Tópico de áudio resumido ─────────────────────────────────────────────────
public class AudioTopicSummaryDto
{
    public Guid id { get; set; }
    public string description { get; set; } = string.Empty;
    public DateTime? start_of_audio { get; set; }
    public DateTime? end_of_audio { get; set; }
    public int? avg_attention { get; set; }
    public int? avg_meditation { get; set; }
}

// ─── Distribuição de atenção em faixas ───────────────────────────────────────
public class AttentionDistributionDto
{
    public int low_count { get; set; } // 0–24
    public int medium_low_count { get; set; } // 25–49
    public int medium_count { get; set; } // 50–74
    public int high_count { get; set; } // 75–100
}

// ─── Atenção por local ────────────────────────────────────────────────────────
public class LocationAttentionDto
{
    public string location_label { get; set; } = string.Empty;
    public double representative_latitude { get; set; }
    public double representative_longitude { get; set; }
    public int session_count { get; set; }
    public int? avg_attention { get; set; }
    public int? avg_meditation { get; set; }
}

// ─── Atenção por faixa de horário ─────────────────────────────────────────────
public class HourlyAttentionDto
{
    public string hour_range { get; set; } = string.Empty; // "08h–09h"
    public int hour_start { get; set; } // 8
    public int? avg_attention { get; set; }
    public int record_count { get; set; }
}

// ─── Ponto na linha do tempo de evolução ─────────────────────────────────────
public class SessionTrendPointDto
{
    public int session_index { get; set; }
    public Guid session_id { get; set; }
    public DateTime? session_date { get; set; }
    public int? avg_attention { get; set; }
    public int? avg_meditation { get; set; }
}
