using System.Reflection.Metadata;
using System.Security.Claims;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Services.Auth;
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

            // Antes devolvia só a string da mensagem (Unauthorized(result.Message)),
            // que vira um JSON string solto no corpo — o frontend espera um objeto
            // com "message", então sempre caía no texto genérico de fallback dele.
            if (result.Success == false)
                return Unauthorized(result);

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

            var user = _context.Profiles.FirstOrDefault(x => x.Id == userId);

            if (user == null)
                return NotFound(new { Message = "Usuário não encontrado." });

            return Ok(
                new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role.ToString(),
                }
            );
        }

        [HttpPost("GoogleLogin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            dynamic result = await _authService.GoogleLogin(dto);

            if (result.Success == false)
                return Unauthorized(result.Message);

            return Ok(result);
        }
    }
}
