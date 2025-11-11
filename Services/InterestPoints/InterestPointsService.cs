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
        InterestPointCreateDto interestPointCreateDto
    )
    {
        try
        {
            var list = new List<Models.InterestPoints>();

            foreach (var name in interestPointCreateDto.name)
            {
                var interestPoint = new Models.InterestPoints
                {
                    id = Guid.NewGuid(),
                    name = name,
                    patient_id = interestPointCreateDto.patient_id,
                    is_deleted = false,
                };
                list.Add(interestPoint);
            }

            await _context.points_of_interest.AddRangeAsync(list);
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
            var interestPoint = await _context.points_of_interest.FindAsync(id);
            if (interestPoint == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Ponto de interesse não encontrado",
                    StatusCode = 404,
                };
            }

            interestPoint.is_deleted = true;
            _context.points_of_interest.Update(interestPoint);
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
            var interestPoint = await _context.points_of_interest.FindAsync(id);
            if (interestPoint == null || interestPoint.is_deleted)
            {
                return ResponseModel<InterestPointDto>.Fail(
                    "Ponto de interesse não encontrado",
                    404
                );
            }

            var interestPointDto = new InterestPointDto
            {
                id = interestPoint.id,
                name = interestPoint.name,
                patient_id = interestPoint.patient_id,
            };

            return ResponseModel<InterestPointDto>.Ok(
                interestPointDto,
                "Ponto de interesse encontrado",
                200
            );
        }
        catch (Exception)
        {
            return ResponseModel<InterestPointDto>.Fail("Erro ao buscar ponto de interesse", 500);
        }
    }

    public async Task<ResponseModel<List<InterestPointDto>>> GetInterestPointsByPatientIdAsync(
        Guid patientId
    )
    {
        try
        {
            var interestPoints = await _context
                .points_of_interest.Where(ip => ip.patient_id == patientId && !ip.is_deleted)
                .AsNoTracking()
                .ToListAsync();

            var interestPointDtos = interestPoints
                .Select(interestPoint => new InterestPointDto
                {
                    id = interestPoint.id,
                    name = interestPoint.name,
                    patient_id = interestPoint.patient_id,
                })
                .ToList();

            return ResponseModel<List<InterestPointDto>>.Ok(
                interestPointDtos,
                "Pontos de interesse encontrados",
                200
            );
        }
        catch (Exception)
        {
            return ResponseModel<List<InterestPointDto>>.Fail(
                "Erro ao buscar pontos de interesse",
                500
            );
        }
    }

    public async Task<ResponseModel<string>> UpdateInterestPointsAsync(
        InterestPointUpdateDto interestPointUpdateDto,
        Guid id
    )
    {
        try
        {
            var interestPoint = await _context.points_of_interest.FindAsync(id);
            if (interestPoint == null)
            {
                return new ResponseModel<string>
                {
                    Success = false,
                    Message = "Ponto de interesse não encontrado",
                    StatusCode = 404,
                };
            }

            interestPoint.name = interestPointUpdateDto.name ?? interestPoint.name;
            _context.points_of_interest.Update(interestPoint);
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
