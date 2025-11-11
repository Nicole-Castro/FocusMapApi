using FocusMapApi.DTO.SessionData;
using FocusMapApi.Services.SessionData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.SessionData
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionDataController : ControllerBase
    {
        private readonly ISessionDataService _sessionDataService;

        public SessionDataController(ISessionDataService sessionDataService)
        {
            _sessionDataService = sessionDataService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSessionData(
            [FromBody] SessionDataCreateDto sessionDataCreateDto
        )
        {
            var result = await _sessionDataService.CreateSessionData(sessionDataCreateDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSessionDataBySessionId(Guid sessionId)
        {
            var result = await _sessionDataService.GetSessionDataBySessionId(sessionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetSessionDataByPatientId(Guid patientId)
        {
            var result = await _sessionDataService.GetSessionDataByPatientId(patientId);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
