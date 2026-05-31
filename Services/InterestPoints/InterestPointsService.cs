using System;
using FocusMapApi.Data;
using FocusMapApi.DTO.InterestPoints;
using FocusMapApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Services.InterestPoints;

public class InterestPointsService : IInterestPointsService
{
    private readonly AppDbContext _context;

    public InterestPointsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel<string>> CreateInterestPointsAsync(
        InterestPointCreateDto interestPointCreateDto,
        Guid patientId
    )
    {
        try
        {
            var point = new InterestPointModel
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Name = interestPointCreateDto.name,
                IsDeleted = false,
            };

            await _context.InterestPoints.AddAsync(point);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Pontos de interesse criados com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = "Erro ao criar pontos de interesse",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<string>> DeleteInterestPointsAsync(Guid id)
    {
        try
        {
            var interestPoint = await _context.InterestPoints.FindAsync(id);
            if (interestPoint == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Ponto de interesse não encontrado",
                    StatusCode = 404,
                };
            }

            interestPoint.IsDeleted = true;
            _context.InterestPoints.Update(interestPoint);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Ponto de interesse deletado com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = "Erro ao deletar ponto de interesse",
                StatusCode = 500,
            };
        }
    }

    public async Task<ResponseModel<InterestPointDto>> GetInterestPointsByIdAsync(Guid id)
    {
        try
        {
            var interestPoint = await _context.InterestPoints.FindAsync(id);
            if (interestPoint == null || interestPoint.IsDeleted)
            {
                return ResponseModel<InterestPointDto>.Fail("Ponto de interesse não encontrado", 404);
            }

            var interestPointDto = new InterestPointDto
            {
                id = interestPoint.Id,
                name = interestPoint.Name,
            };

            return ResponseModel<InterestPointDto>.Ok(interestPointDto, "Ponto de interesse encontrado", 200);
        }
        catch (Exception)
        {
            return ResponseModel<InterestPointDto>.Fail("Erro ao buscar ponto de interesse", 500);
        }
    }

    public async Task<ResponseModel<List<InterestPointDto>>> GetInterestPointsByPatientIdAsync(Guid patientId)
    {
        try
        {
            var interestPoints = await _context.InterestPoints
                .Where(ip => ip.PatientId == patientId && !ip.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            var interestPointDtos = interestPoints
                .Select(ip => new InterestPointDto
                {
                    id = ip.Id,
                    name = ip.Name,
                })
                .ToList();

            return ResponseModel<List<InterestPointDto>>.Ok(interestPointDtos, "Pontos de interesse encontrados", 200);
        }
        catch (Exception)
        {
            return ResponseModel<List<InterestPointDto>>.Fail("Erro ao buscar pontos de interesse", 500);
        }
    }

    public async Task<ResponseModel<string>> UpdateInterestPointsAsync(
        InterestPointUpdateDto interestPointUpdateDto,
        Guid id
    )
    {
        try
        {
            var interestPoint = await _context.InterestPoints.FindAsync(id);
            if (interestPoint == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Ponto de interesse não encontrado",
                    StatusCode = 404,
                };
            }

            interestPoint.Name = interestPointUpdateDto.name ?? interestPoint.Name;
            _context.InterestPoints.Update(interestPoint);
            await _context.SaveChangesAsync();

            return new ResponseModel<string>
            {
                Success = true,
                Message = "Ponto de interesse atualizado com sucesso",
                StatusCode = 200,
            };
        }
        catch (Exception)
        {
            return new ResponseModel<string>
            {
                Success = false,
                Message = "Erro ao atualizar ponto de interesse",
                StatusCode = 500,
            };
        }
    }
}
