using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ACGSimBack.Services.Auth;

public class AuthService : IAuthInterface
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<object> Login(LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.email))
            return new { Status = false, Message = "E-mail inválido ou não informado." };

        var email = loginDto.email.ToLower();

        var patient = await _context.patients.FirstOrDefaultAsync(u => u.email.ToLower() == email);

        var professional =
            patient == null
                ? await _context.professionals.FirstOrDefaultAsync(u => u.email.ToLower() == email)
                : null;

        if (patient == null && professional == null)
            return new { Status = false, Message = "Usuário não encontrado." };

        var user = (object)patient ?? professional;

        string storedPassword = patient?.password ?? professional?.password;

        if (!BCrypt.Net.BCrypt.Verify(loginDto.password, storedPassword))
            return new { Status = false, Message = "Senha inválida." };

        var jwt = GenerateJwtToken(user);

        return new
        {
            Status = true,
            Data = new
            {
                id = patient?.id ?? professional?.id,
                token = jwt,
                name = patient?.name ?? professional?.name,
                email = patient?.email ?? professional?.email,
                type = patient != null ? "patient" : "professional",
            },
        };
    }

    private string GenerateJwtToken(object user)
    {
        string userId = string.Empty;
        string email = string.Empty;
        string userType = string.Empty;

        // Detecta o tipo do usuário
        switch (user)
        {
            case PatientModel patient:
                userId = patient.id?.ToString() ?? "";
                email = patient.email;
                userType = "patient";
                break;

            case UsersModel professional:
                userId = professional.id.ToString() ?? "";
                email = professional.email;
                userType = "professional";
                break;

            default:
                throw new ArgumentException("Tipo de usuário inválido ao gerar o token.");
        }

        // Cria as claims
        var claims = new List<Claim>
        {
            new Claim("UserId", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("UserType", userType),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
