using System;
using FocusMapApi.DTO.AudioDescription;
using FocusMapApi.Models;

namespace FocusMapApi.Services.AudioDescription;

public interface IAudioDescriptionService
{
    public Task<ResponseModel<string>> CreateAudioDescription(
        AudioDescriptionCreateDto audioDescriptionCreateDto
    );
    public Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionById(
        Guid audioDescriptionId
    );
    public Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionBySessionId(Guid sessionId);
}
