using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using FocusMapApi.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace FocusMapApi.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private IUserService _user;

        public UserController(IUserService user)
        {
            _user = user;
        }

        // Não é mais autocadastro público: só um Admin (funcionário da empresa) pode
        // cadastrar um Professional. Ver Services/User/UserService.cs:CreateUser.
        [Authorize(Roles = "Admin")]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto user)
        {
            if (user == null)
                return BadRequest("Usuário inválido.");

            var result = await _user.CreateUser(user);

            if (result == null)
                return StatusCode(500, "Erro ao criar usuário.");

            return Ok(result);
        }

        [HttpPost("CreatePatient")]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto user)
        {
            if (user == null)
                return BadRequest("Usuário inválido.");

            var userIdClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(userIdClaim, out var userId);
            var result = await _user.CreateUserPatient(user, userId);

            if (result == null)
                return StatusCode(500, "Erro ao criar usuário.");

            return Ok(result);
        }

        [HttpPatch("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto user, Guid id)
        {
            if (user == null)
                return BadRequest("Usuário inválido.");

            // Só pode editar a própria conta, a menos que seja Admin editando outra.
            var userIdClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(userIdClaim, out var callerId);
            if (callerId != id && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _user.UpdateUser(user, id);

            if (result == null)
                return StatusCode(500, "Erro ao criar usuário.");

            return Ok(result);
        }

        [HttpPatch("UpdatePatient/{id}")]
        public async Task<IActionResult> UpdatePatient([FromBody] UpdateUserDto user, Guid id)
        {
            if (user == null)
                return BadRequest("Usuário inválido.");

            var result = await _user.UpdateUserPatient(user, id);

            if (result == null)
                return StatusCode(500, "Erro ao criar usuário.");

            return Ok(result);
        }

        [HttpGet("ListPatients")]
        public async Task<IActionResult> ListPatients([FromQuery] string? searchTerm = null)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(userIdClaim, out var userId);
            var result = await _user.ListPatients(userId, searchTerm);
            if (result == null)
                return StatusCode(500, "Erro ao listar pacientes.");

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("GoogleSignUp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleSignUp([FromBody] GoogleAuthDto dto)
        {
            if (dto == null)
                return BadRequest("Dados inválidos.");

            var result = await _user.GoogleSignUp(dto);

            if (result == null)
                return StatusCode(500, "Erro ao autenticar usuário.");

            return Ok(result);
        }

        [HttpGet("TotalPatients")]
        public async Task<IActionResult> TotalPatients()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(userIdClaim, out var userId);
            var result = await _user.TotalPatients(userId);
            if (result == null)
                return StatusCode(500, "Erro ao obter total de pacientes.");

            return Ok(result);
        }

        [HttpDelete("DeletePatient/{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            Guid.TryParse(userIdClaim, out var userId);
            var result = await _user.DeletePatient(id, userId);
            if (result == null)
                return StatusCode(500, "Erro ao excluir paciente.");
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var result = await _user.GetById(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        // ── Admin: gestão de contas Professional ────────────────────────────

        [Authorize(Roles = "Admin")]
        [HttpGet("ListProfessionals")]
        public async Task<IActionResult> ListProfessionals([FromQuery] string? searchTerm = null)
        {
            var result = await _user.ListProfessionals(searchTerm);
            if (result == null)
                return StatusCode(500, "Erro ao listar profissionais.");

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteProfessional/{id}")]
        public async Task<IActionResult> DeleteProfessional(Guid id)
        {
            var result = await _user.DeleteProfessional(id);
            if (result == null)
                return StatusCode(500, "Erro ao excluir profissional.");

            return StatusCode(result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("ResetProfessionalPassword/{id}")]
        public async Task<IActionResult> ResetProfessionalPassword(Guid id)
        {
            var result = await _user.ResetProfessionalPassword(id);
            if (result == null)
                return StatusCode(500, "Erro ao resetar senha.");

            return StatusCode(result.StatusCode, result);
        }
    }
}