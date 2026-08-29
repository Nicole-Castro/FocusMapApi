using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FocusMapApi.Services.Auth;

public class AuthService : IAuthInterface
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<ResponseModel<object>> Login(LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.email))
            return new ResponseModel<object>
            {
                Success = false,
                Message = "E-mail inválido ou não informado.",
                StatusCode = 400,
            };

        var user = await _context.Profiles
            .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.email.ToLower());

        // Mensagem genérica de propósito nos dois casos (usuário não existe / senha errada)
        // pra não revelar pra quem está tentando adivinhar se um e-mail está cadastrado.
        if (user == null || !VerifyPassword(loginDto.password, user.PasswordHash))
            return new ResponseModel<object>
            {
                Success = false,
                Message = "E-mail ou senha inválidos.",
                StatusCode = 401,
            };

        var jwt = GenerateJwtToken(user);

        return new ResponseModel<object>
        {
            Success = true,
            Data = new
            {
                id = user.Id,
                token = jwt,
                name = user.Name,
                email = user.Email,
                role = user.Role.ToString(),
            },
            Message = "Usuário logado com sucesso.",
            StatusCode = 200,
        };
    }

    /// <summary>
    /// Verifica a senha em texto puro contra o hash BCrypt salvo. Contas antigas sem
    /// hash (criadas antes dessa coluna existir) não conseguem mais logar até
    /// definirem uma senha — retorna false nesse caso em vez de aceitar qualquer senha.
    /// </summary>
    private static bool VerifyPassword(string? plainPassword, string? passwordHash)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash salvo em formato inválido/corrompido — trata como senha incorreta.
            return false;
        }
    }

    public string GenerateJwtToken(object userObj)
    {
        if (userObj is not UserModel user)
            throw new ArgumentException("Tipo de usuário inválido ao gerar o token.");

        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UserRole", user.Role.ToString()),
            // Claim padrão do ASP.NET Core — permite usar [Authorize(Roles = "Admin")]
            // nos controllers em vez de checar a claim "UserRole" manualmente.
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<ResponseModel<object>> GoogleLogin(GoogleLoginDto dto)
    {
        var user = await _context.Profiles.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (user == null)
        {
            return new ResponseModel<object>
            {
                Success = false,
                Message = "Usuário não encontrado. Por favor, faça o cadastro.",
                StatusCode = 404,
            };
        }

        var token = GenerateJwtToken(user);

        return new ResponseModel<object>
        {
            Success = true,
            Message = "Autenticado via Google",
            StatusCode = 200,
            Data = new { token, id = user.Id },
        };
    }
}
