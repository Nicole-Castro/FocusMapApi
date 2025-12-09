using System;

namespace FocusMapApi.DTO.Session;

public class SessionDashboardDto
{
    public SessionDto Session { get; set; }

    public List<EegDataDto> Eeg { get; set; }

    public List<AudioTopicDto> AudioTopics { get; set; }
}

public class EegDataDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    public int? DeltaPower { get; set; }
    public int? ThetaPower { get; set; }
    public int? LowAlphaPower { get; set; }
    public int? HighAlphaPower { get; set; }
    public int? LowBetaPower { get; set; }
    public int? HighBetaPower { get; set; }
    public int? LowGammaPower { get; set; }
    public int? MiddleGammaPower { get; set; }

    public int? AttentionValue { get; set; }
    public int? MeditationValue { get; set; }
    public int? RawEegValue { get; set; }

    public DateTime Timestamp { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class AudioTopicDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    public DateTime StartOfAudio { get; set; }
    public DateTime EndOfAudio { get; set; }

    public string? Description { get; set; } // assunto detectado
    public Guid? PointOfInterestId { get; set; }
}
