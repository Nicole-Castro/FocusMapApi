using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.AudioDescription;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FocusMapApi.Services.AudioDescription;

public class AudioDescriptionService : IAudioDescriptionService
{
    private readonly AppDbContext _context;

    public AudioDescriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateAudioDescription(
        AudioDescriptionCreateDto audioDescriptionCreateDto
    )
    {
        try
        {
            var audioDescription = new Models.AudioDescription
            {
                session_id = audioDescriptionCreateDto.session_id,
                start_of_audio = audioDescriptionCreateDto.start_of_audio,
                end_of_audio = audioDescriptionCreateDto.end_of_audio,
                description = audioDescriptionCreateDto.description,
                id_point_of_interest = audioDescriptionCreateDto.id_point_of_interest,
            };
            _context.audio_description.Add(audioDescription);
            await _context.SaveChangesAsync();
            return new ResponseModel<string>
            {
                Success = true,
                Data = "Audio description created successfully.",
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Error creating audio description: {ex.Message}",
            };
        }
    }

    public async Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionById(
        Guid audioDescriptionId
    )
    {
        try
        {
            var audioDescription = await _context.audio_description.FindAsync(audioDescriptionId);

            if (audioDescription == null)
            {
                return new ResponseModel<AudioDescriotionDto>
                {
                    Success = false,
                    Message = "Audio description not found.",
                };
            }

            var audioDescriptionDto = new AudioDescriotionDto
            {
                id = audioDescription.id,
                session_id = audioDescription.session_id,
                start_of_audio = audioDescription.start_of_audio,
                end_of_audio = audioDescription.end_of_audio,
                description = audioDescription.description,
                id_point_of_interest = audioDescription.id_point_of_interest,
            };

            return new ResponseModel<AudioDescriotionDto>
            {
                Success = true,
                Data = audioDescriptionDto,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<AudioDescriotionDto>
            {
                Success = false,
                Message = $"Error retrieving audio description: {ex.Message}",
            };
        }
    }

    public async Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionBySessionId(
        Guid sessionId
    )
    {
        try
        {
            var audioDescription = await _context.audio_description.FirstOrDefaultAsync(ad =>
                ad.session_id == sessionId
            );

            if (audioDescription == null)
            {
                return new ResponseModel<AudioDescriotionDto>
                {
                    Success = false,
                    Message = "Audio description not found.",
                };
            }

            var audioDescriptionDto = new AudioDescriotionDto
            {
                id = audioDescription.id,
                session_id = audioDescription.session_id,
                start_of_audio = audioDescription.start_of_audio,
                end_of_audio = audioDescription.end_of_audio,
                description = audioDescription.description,
                id_point_of_interest = audioDescription.id_point_of_interest,
            };

            return new ResponseModel<AudioDescriotionDto>
            {
                Success = true,
                Data = audioDescriptionDto,
                Message = "Audio description retrieved successfully",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<AudioDescriotionDto>
            {
                Success = false,
                Message = $"Error retrieving audio description: {ex.Message}",
            };
        }
    }
}
