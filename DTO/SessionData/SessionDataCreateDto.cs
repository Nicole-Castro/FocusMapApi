using System;

namespace FocusMapApi.DTO.SessionData;

public class SessionDataCreateDto
{
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
