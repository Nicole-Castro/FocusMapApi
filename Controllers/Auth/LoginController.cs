using System.Reflection.Metadata;
using System.Security.Claims;
using ACGSimBack.Services.Auth;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IAuthInterface _authService;
        private readonly AppDbContext _context;

        public LoginController(IAuthInterface authInterface, AppDbContext appDbContext)
        {
            _context = appDbContext;
            _authService = authInterface;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginWithMicrosoft([FromBody] LoginDto loginDto)
        {
            dynamic result = await _authService.Login(loginDto);

            if (result.Status == false)
                return Unauthorized(result.Message);

            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var type = User.FindFirst("UserType")?.Value;

            if (!Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized(new { Message = "Token inválido." });

            var patient = _context.patients.FirstOrDefault(x => x.id == userId);
            var professional = _context.professionals.FirstOrDefault(x => x.id == userId);

            if (patient != null)
                return Ok(
                    new
                    {
                        patient.id,
                        patient.name,
                        patient.email,
                        type,
                    }
                );

            if (professional != null)
                return Ok(
                    new
                    {
                        professional.id,
                        professional.name,
                        professional.email,
                        type,
                    }
                );

            return NotFound(new { Message = "Usuário não encontrado." });
        }
    }
}
