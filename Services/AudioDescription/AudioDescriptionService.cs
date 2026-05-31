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
            var audioDescription = new AudioDescriptionModel
            {
                SessionId = audioDescriptionCreateDto.session_id,
                StartOfAudio = audioDescriptionCreateDto.start_of_audio,
                EndOfAudio = audioDescriptionCreateDto.end_of_audio,
                Description = audioDescriptionCreateDto.description,
                InterestPointId = audioDescriptionCreateDto.id_point_of_interest,
            };
            _context.AudioDescriptions.Add(audioDescription);
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

    public async Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionById(Guid audioDescriptionId)
    {
        try
        {
            var audioDescription = await _context.AudioDescriptions.FindAsync(audioDescriptionId);

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
                id = audioDescription.Id,
                session_id = audioDescription.SessionId,
                start_of_audio = audioDescription.StartOfAudio,
                end_of_audio = audioDescription.EndOfAudio,
                description = audioDescription.Description,
                id_point_of_interest = audioDescription.InterestPointId,
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

    public async Task<ResponseModel<AudioDescriotionDto>> GetAudioDescriptionBySessionId(Guid sessionId)
    {
        try
        {
            var audioDescription = await _context.AudioDescriptions
                .FirstOrDefaultAsync(ad => ad.SessionId == sessionId);

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
                id = audioDescription.Id,
                session_id = audioDescription.SessionId,
                start_of_audio = audioDescription.StartOfAudio,
                end_of_audio = audioDescription.EndOfAudio,
                description = audioDescription.Description,
                id_point_of_interest = audioDescription.InterestPointId,
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
