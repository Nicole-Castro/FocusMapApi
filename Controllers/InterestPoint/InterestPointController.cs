using FocusMapApi.DTO.InterestPoints;
using FocusMapApi.Services.InterestPoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.InterestPoint
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InterestPointController : ControllerBase
    {
        private readonly IInterestPointsService _interestPointsService;

        public InterestPointController(IInterestPointsService interestPointsService)
        {
            _interestPointsService = interestPointsService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateInterestPoints(
            [FromBody] InterestPointCreateDto interestPointDto
        )
        {
            var result = await _interestPointsService.CreateInterestPointsAsync(interestPointDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInterestPoints(Guid id)
        {
            var result = await _interestPointsService.DeleteInterestPointsAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInterestPointsById(Guid id)
        {
            var result = await _interestPointsService.GetInterestPointsByIdAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetInterestPointsByPatientId(Guid patientId)
        {
            var result = await _interestPointsService.GetInterestPointsByPatientIdAsync(patientId);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInterestPoints(
            Guid id,
            [FromBody] InterestPointUpdateDto interestPointDto
        )
        {
            var result = await _interestPointsService.UpdateInterestPointsAsync(
                interestPointDto,
                id
            );
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
