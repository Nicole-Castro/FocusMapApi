using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.User;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FocusMapApi.Services.User;

public class UserService : IUserService
{
    private AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateUser(CreateUserDto user)
    {
        try
        {
            var getUsers = await _context.professionals.FirstOrDefaultAsync(u =>
                u.email == user.email
            );
            if (getUsers != null)
            {
                return "Email já cadastrado";
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
            return "Usuário Cadastrado com sucesso";
        }
        catch (Exception ex)
        {
            return $"Erro ao cadastrar usuario: {ex}";
        }
    }

    public async Task<bool> CreateUserPatient(CreatePatientDto patient, Guid userId)
    {
        try
        {
            var exists = await _context
                .patients.AsNoTracking()
                .AnyAsync(u => u.email.ToLower() == patient.email.ToLower());

            if (exists)
                return false;

            var u = new PatientModel
            {
                id = Guid.NewGuid(),
                email = patient.email,
                name = patient.name,
                password = BCrypt.Net.BCrypt.HashPassword(patient.password),
                professional_id = userId
            };

            _context.patients.Add(u);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<string> UpdateUser(UpdateUserDto user, Guid id)
    {
        try
        {
            var getUser = await _context.professionals.FirstOrDefaultAsync(a => a.id == id);
            if (getUser == null)
            {
                return "Usuário não encontrado";
            }
            getUser.name = user.name ?? getUser.name;
            getUser.password = BCrypt.Net.BCrypt.HashPassword(user.password) ?? getUser.password;

            _context.professionals.Update(getUser);
            await _context.SaveChangesAsync();
            return "Atualizado com sucesso";
        }
        catch (Exception ex)
        {
            return $"Erro ao realizar atualização: {ex}";
        }
    }

    public async Task<string> UpdateUserPatient(UpdateUserDto patient, Guid id)
    {
        try
        {
            var getUser = await _context.patients.FirstOrDefaultAsync(a => a.id == id);
            if (getUser == null)
            {
                return "Usuário não encontrado";
            }
            getUser.name = patient.name ?? getUser.name;
            getUser.password = BCrypt.Net.BCrypt.HashPassword(patient.password) ?? getUser.password;

            _context.patients.Update(getUser);
            await _context.SaveChangesAsync();
            return "Atualizado com sucesso";
        }
        catch (Exception ex)
        {
            return $"Erro ao realizar atualização: {ex}";
        }
    }
}
