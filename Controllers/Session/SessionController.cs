using FocusMapApi.DTO.Session;
using FocusMapApi.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.Session
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] SessionCreateDto sessionCreateDto)
        {
            var result = await _sessionService.CreateSessionAsync(sessionCreateDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}/dashboard")]
        public async Task<IActionResult> GetSessionDashboard(Guid id)
        {
            var result = await _sessionService.getSessionDashboardAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var result = await _sessionService.GetSessionByIdAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetSessionsByPatientId(Guid patientId)
        {
            var result = await _sessionService.GetSessionsByPatientIdAsync(patientId);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(Guid id)
        {
            var result = await _sessionService.UpdateSessionAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("professional")]
        public async Task<IActionResult> GetSessionsByProfessionalId(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            var UserClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(UserClaim, out var userId);
            var result = await _sessionService.GetSessionsByProfessionalIdAsync(
                userId, page, pageSize, status, patientId, dateFrom, dateTo);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
