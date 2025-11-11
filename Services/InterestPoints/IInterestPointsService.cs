using System;
using FocusMapApi.DTO.InterestPoints;
using FocusMapApi.Models;

namespace FocusMapApi.Services.InterestPoints;

public interface IInterestPointsService
{
    Task<ResponseModel<string>> CreateInterestPointsAsync(
        InterestPointCreateDto interestPointCreateDto
    );
    Task<ResponseModel<string>> DeleteInterestPointsAsync(Guid id);
    Task<ResponseModel<string>> UpdateInterestPointsAsync(
        InterestPointUpdateDto interestPointUpdateDto,
        Guid id
    );
    Task<ResponseModel<InterestPointDto>> GetInterestPointsByIdAsync(Guid id);
    Task<ResponseModel<List<InterestPointDto>>> GetInterestPointsByPatientIdAsync(Guid patientId);
    
}
