using System;
using System.Security.Claims;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using FocusMapApi.Services.Auth;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FocusMapApi.Services.User;

public class UserService : IUserService
{
    private AppDbContext _context;
    private IAuthInterface _authService;

    public UserService(AppDbContext context, IAuthInterface authService)
    {
        _context = context;
        _authService = authService;
    }

    // Usado pela tela de admin (POST /User/CreateUser, [Authorize(Roles = "Admin")])
    // pra cadastrar um Professional. Não é mais autocadastro público — por isso não
    // devolve token de login: quem chama é o admin, não a conta sendo criada.
    public async Task<ResponseModel<object>> CreateUser(CreateUserDto user)
    {
        try
        {
            if (user == null)
                return new ResponseModel<object>
                {
                    Success = false,
                    Message = "DTO chegou nulo — o JSON não está sendo mapeado.",
                    StatusCode = 400,
                };

            if (string.IsNullOrWhiteSpace(user.Password))
                return new ResponseModel<object>
                {
                    Success = false,
                    Message = "Senha obrigatória.",
                    StatusCode = 400,
                };

            var exists = await _context.Profiles.AnyAsync(u => u.Email == user.Email);
            if (exists)
                return new ResponseModel<object>
                {
                    Success = false,
                    Message = "Email já cadastrado",
                    StatusCode = 400,
                };

            // O ID deve vir do Supabase Auth (auth.users.id).
            // Se ainda não integrado, gera um novo Guid provisoriamente.
            var newUser = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Name = user.Name,
                Role = UserRole.Professional,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
            };

            _context.Profiles.Add(newUser);
            await _context.SaveChangesAsync();

            return new ResponseModel<object>
            {
                Success = true,
                Message = "Usuário cadastrado com sucesso",
                StatusCode = 200,
                Data = new { id = newUser.Id },
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<object>
            {
                Success = false,
                Message = $"Erro ao cadastrar usuario: {ex.Message}",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<string>> CreateUserPatient(
        CreatePatientDto patient,
        Guid professionalId
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patient.password))
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Senha obrigatória.",
                    StatusCode = 400,
                };

            var exists = await _context
                .Profiles.AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == patient.email.ToLower());

            if (exists)
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Email já cadastrado",
                    StatusCode = 400,
                };

            var newPatient = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = patient.email,
                Name = patient.name,
                Role = UserRole.Patient,
                ProfessionalId = professionalId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(patient.password),
            };

            _context.Profiles.Add(newPatient);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Usuário cadastrado com sucesso",
                StatusCode = 200,
            };
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = "Erro ao cadastrar usuario: Email já cadastrado",
                StatusCode = 400,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Erro ao cadastrar usuario: {ex}",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<List<ListPatientsDto>>> ListPatients(
        Guid professionalId,
        string? searchTerm = null
    )
    {
        try
        {
            var query = _context.Profiles.Where(p =>
                p.Role == UserRole.Patient && p.ProfessionalId == professionalId
            );

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()));

            var patients = await query
                .Select(p => new ListPatientsDto
                {
                    id = p.Id,
                    name = p.Name,
                    email = p.Email,
                })
                .ToListAsync();

            return new ResponseModel<List<ListPatientsDto>>
            {
                Success = true,
                Message = "Lista de pacientes obtida com sucesso",
                Data = patients,
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<List<ListPatientsDto>>
            {
                Success = false,
                Message = $"Erro ao obter lista de pacientes: {ex.Message}",
                Data = null,
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<object>> GoogleSignUp(GoogleAuthDto dto)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.Token);

        var existingUser = await _context.Profiles.FirstOrDefaultAsync(u =>
            u.Email == payload.Email
        );

        UserModel user;

        if (existingUser == null)
        {
            user = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = payload.Email,
                Name = payload.Name,
                Role = UserRole.Professional,
            };

            _context.Profiles.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            user = existingUser;
        }

        var token = _authService.GenerateJwtToken(user);

        return new ResponseModel<object>
        {
            Success = true,
            Message = "Autenticado via Google",
            StatusCode = 200,
            Data = new { token, id = user.Id },
        };
    }

    public async Task<ResponseModel<string>> UpdateUser(UpdateUserDto dto, Guid id)
    {
        try
        {
            var user = await _context.Profiles.FirstOrDefaultAsync(a => a.Id == id);
            if (user == null)
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    StatusCode = 404,
                };

            user.Name = dto.name ?? user.Name;
            // Antes esse campo era ignorado por completo — a troca de senha no
            // "Meu Perfil" dava "sucesso" na tela sem nunca mudar o hash de verdade.
            if (!string.IsNullOrWhiteSpace(dto.password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.password);
            user.UpdatedAt = DateTime.UtcNow;

            _context.Profiles.Update(user);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Atualizado com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Erro ao realizar atualização: {ex}",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<string>> UpdateUserPatient(UpdateUserDto dto, Guid id)
    {
        try
        {
            var patient = await _context.Profiles.FirstOrDefaultAsync(a =>
                a.Id == id && a.Role == UserRole.Patient
            );

            if (patient == null)
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    StatusCode = 404,
                };

            patient.Name = dto.name ?? patient.Name;
            if (!string.IsNullOrWhiteSpace(dto.password))
                patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.password);
            patient.UpdatedAt = DateTime.UtcNow;

            _context.Profiles.Update(patient);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Atualizado com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = $"Erro ao realizar atualização: {ex}",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<object>> TotalPatients(Guid professionalId)
    {
        try
        {
            var total = await _context.Profiles.CountAsync(p =>
                p.Role == UserRole.Patient && p.ProfessionalId == professionalId
            );

            return new ResponseModel<object>
            {
                Success = true,
                Message = "Total de pacientes obtido com sucesso",
                Data = new { total },
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<object>
            {
                Success = false,
                Message = $"Erro ao obter total de pacientes: {ex.Message}",
                Data = null,
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<string>> DeletePatient(Guid patientId, Guid professionalId)
    {
        try
        {
            var patient = await _context.Profiles.FirstOrDefaultAsync(p =>
                p.Id == patientId
                && p.ProfessionalId == professionalId
                && p.Role == UserRole.Patient
                && !p.IsDeleted
            );

            if (patient == null)
                return ResponseModel<string>.Fail("Paciente não encontrado", 404);

            patient.IsDeleted = true;
            patient.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Paciente excluído com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return ResponseModel<string>.Fail($"Erro ao excluir paciente: {ex.Message}", 500);
        }
    }

    public async Task<ResponseModel<object>> GetById(Guid id)
    {
        try
        {
            var user = await _context.Profiles.FirstOrDefaultAsync(a => a.Id == id);
            if (user == null)
                return new ResponseModel<object>
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    StatusCode = 404,
                };

            return new ResponseModel<object>
            {
                Success = true,
                Message = "Usuário obtido com sucesso",
                Data = user.Name,
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<object>
            {
                Success = false,
                Message = $"Erro ao obter usuário: {ex.Message}",
                Data = null,
                StatusCode = 500,
            };
        }
    }

    // ── Admin: gestão de contas Professional ────────────────────────────────

    public async Task<ResponseModel<List<ListProfessionalsDto>>> ListProfessionals(
        string? searchTerm = null
    )
    {
        try
        {
            var query = _context.Profiles.Where(p => p.Role == UserRole.Professional);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm.ToLower())
                    || p.Email.ToLower().Contains(searchTerm.ToLower())
                );

            var professionals = await query
                .OrderBy(p => p.Name)
                .Select(p => new ListProfessionalsDto
                {
                    id = p.Id,
                    name = p.Name,
                    email = p.Email,
                })
                .ToListAsync();

            return new ResponseModel<List<ListProfessionalsDto>>
            {
                Success = true,
                Message = "Lista de profissionais obtida com sucesso",
                Data = professionals,
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new ResponseModel<List<ListProfessionalsDto>>
            {
                Success = false,
                Message = $"Erro ao obter lista de profissionais: {ex.Message}",
                Data = null,
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<string>> DeleteProfessional(Guid professionalId)
    {
        try
        {
            var professional = await _context.Profiles.FirstOrDefaultAsync(p =>
                p.Id == professionalId && p.Role == UserRole.Professional && !p.IsDeleted
            );

            if (professional == null)
                return ResponseModel<string>.Fail("Profissional não encontrado", 404);

            professional.IsDeleted = true;
            professional.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Profissional excluído com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return ResponseModel<string>.Fail($"Erro ao excluir profissional: {ex.Message}", 500);
        }
    }

    public async Task<ResponseModel<string>> ResetProfessionalPassword(Guid professionalId)
    {
        try
        {
            var professional = await _context.Profiles.FirstOrDefaultAsync(p =>
                p.Id == professionalId && p.Role == UserRole.Professional && !p.IsDeleted
            );

            if (professional == null)
                return ResponseModel<string>.Fail("Profissional não encontrado", 404);

            var temporaryPassword = GenerateTemporaryPassword();
            professional.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            professional.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // A senha só existe em texto puro aqui, nesta resposta — não fica salva
            // em lugar nenhum além do hash. O admin precisa repassar pro profissional agora.
            return new ResponseModel<string>
            {
                Success = true,
                Message = "Senha provisória gerada com sucesso",
                Data = temporaryPassword,
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return ResponseModel<string>.Fail($"Erro ao resetar senha: {ex.Message}", 500);
        }
    }

    private static string GenerateTemporaryPassword()
    {
        // Atende aos requisitos de força já usados no cadastro (maiúscula, minúscula,
        // número, caractere especial, mínimo 6 caracteres).
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%*";
        const string all = upper + lower + digits;

        static char Pick(string chars) =>
            chars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(chars.Length)];

        var required = new[] { Pick(upper), Pick(lower), Pick(digits), Pick(special) };
        var rest = Enumerable.Range(0, 6).Select(_ => Pick(all));

        return new string(
            required
                .Concat(rest)
                .OrderBy(_ =>
                    System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue)
                )
                .ToArray()
        );
    }
}
