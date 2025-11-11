using FocusMapApi.DTO.AudioDescription;
using FocusMapApi.Services.AudioDescription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.AudioDescription
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AudioDescriptionController : ControllerBase
    {
        private readonly IAudioDescriptionService _audioDescriptionService;

        public AudioDescriptionController(IAudioDescriptionService audioDescriptionService)
        {
            _audioDescriptionService = audioDescriptionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAudioDescription(
            [FromBody] AudioDescriptionCreateDto audioDescriptionCreateDto
        )
        {
            var response = await _audioDescriptionService.CreateAudioDescription(
                audioDescriptionCreateDto
            );
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAudioDescriptionById(Guid id)
        {
            var response = await _audioDescriptionService.GetAudioDescriptionById(id);
            if (response.Success)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetAudioDescriptionBySessionId(Guid sessionId)
        {
            var response = await _audioDescriptionService.GetAudioDescriptionBySessionId(sessionId);
            if (response.Success)
            {
                return Ok(response);
            }
            return NotFound(response);
        }
    }
}
