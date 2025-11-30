using System;
using System.Security.Claims;
using ACGSimBack.Services.Auth;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FocusMapApi.Services.User;

public class UserService : IUserService
{
    private AppDbContext _context;
    private IAuthInterface _authService;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateUser(CreateUserDto user)
    {
        try
        {
            var getUsers = await _context.professionals.FirstOrDefaultAsync(u =>
                u.email == user.email
            );
            if (getUsers != null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Email já cadastrado",
                    StatusCode = 400,
                };
            }

            var u = new UsersModel
            {
                id = Guid.NewGuid(),
                email = user.email,
                name = user.name,
                password = BCrypt.Net.BCrypt.HashPassword(user.password),
            };
            _context.professionals.Add(u);
            await _context.SaveChangesAsync();
            return new ResponseModel<string>
            {
                Success = true,
                Message = "Usuário Cadastrado com sucesso",
                StatusCode = 200,
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

    public async Task<ResponseModel<string>> CreateUserPatient(
        CreatePatientDto patient,
        Guid userId
    )
    {
        try
        {
            var exists = await _context
                .patients.AsNoTracking()
                .AnyAsync(u => u.email.ToLower() == patient.email.ToLower());

            if (exists)
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Email já cadastrado",
                    StatusCode = 400,
                };

            var u = new PatientModel
            {
                id = Guid.NewGuid(),
                email = patient.email,
                name = patient.name,
                password = BCrypt.Net.BCrypt.HashPassword(patient.password),
                professional_id = userId,
            };

            _context.patients.Add(u);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Usuário Cadastrado com sucesso",
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

    public async Task<ResponseModel<List<ListPatientsDto>>> ListPatients(Guid id)
    {
        try
        {
            var patients = await _context
                .patients.Where(p => p.professional_id == id)
                .Select(p => new ListPatientsDto
                {
                    id = p.id,
                    name = p.name,
                    email = p.email,
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

        var existingUser = await _context.professionals.FirstOrDefaultAsync(u =>
            u.email == payload.Email
        );

        UsersModel user;

        if (existingUser == null)
        {
            user = new UsersModel
            {
                id = Guid.NewGuid(),
                email = payload.Email,
                name = payload.Name,
                password = null,
            };

            _context.professionals.Add(user);
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
            Data = new { token, user.id },
        };
    }

    public async Task<ResponseModel<string>> UpdateUser(UpdateUserDto user, Guid id)
    {
        try
        {
            var getUser = await _context.professionals.FirstOrDefaultAsync(a => a.id == id);
            if (getUser == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    StatusCode = 404,
                };
            }
            getUser.name = user.name ?? getUser.name;
            getUser.password = BCrypt.Net.BCrypt.HashPassword(user.password) ?? getUser.password;

            _context.professionals.Update(getUser);
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

    public async Task<ResponseModel<string>> UpdateUserPatient(UpdateUserDto patient, Guid id)
    {
        try
        {
            var getUser = await _context.patients.FirstOrDefaultAsync(a => a.id == id);
            if (getUser == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    StatusCode = 404,
                };
            }
            getUser.name = patient.name ?? getUser.name;
            getUser.password = BCrypt.Net.BCrypt.HashPassword(patient.password) ?? getUser.password;

            _context.patients.Update(getUser);
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
}
