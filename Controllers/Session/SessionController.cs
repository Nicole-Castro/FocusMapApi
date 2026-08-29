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
            var (callerId, callerRole) = GetCaller();
            var result = await _sessionService.getSessionDashboardAsync(id, callerId, callerRole);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var (callerId, callerRole) = GetCaller();
            var result = await _sessionService.GetSessionByIdAsync(id, callerId, callerRole);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetSessionsByPatientId(Guid patientId)
        {
            var (callerId, callerRole) = GetCaller();
            var result = await _sessionService.GetSessionsByPatientIdAsync(patientId, callerId, callerRole);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        // Só pra não repetir a leitura de claims em cada action acima.
        private (Guid callerId, string callerRole) GetCaller()
        {
            Guid.TryParse(User.FindFirst("UserId")?.Value, out var callerId);
            var callerRole = User.FindFirst("UserRole")?.Value ?? string.Empty;
            return (callerId, callerRole);
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
