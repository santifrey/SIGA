using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionFilas : IGestionFilas
{
    private readonly IFilaRepository _filaRepository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly ITemporadaRepository _temporadaRepository;

    public GestionFilas(
        IFilaRepository filaRepository,
        ISucursalRepository sucursalRepository,
        ITemporadaRepository temporadaRepository)
    {
        _filaRepository = filaRepository;
        _sucursalRepository = sucursalRepository;
        _temporadaRepository = temporadaRepository;
    }

    public async Task<ApiResponse<IEnumerable<FilaDto>>> ListarPorSucursalYTemporadaAsync(
        int sucursalId, 
        int temporadaId, 
        CancellationToken cancellationToken = default)
    {
        var sucursal = await _sucursalRepository.GetByIdAsync(sucursalId, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), sucursalId);

        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var filas = await _filaRepository.GetBySucursalAndTemporadaAsync(sucursalId, temporadaId, cancellationToken);
        var dtos = filas.Select(MapToDto);
        return ApiResponse<IEnumerable<FilaDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<FilaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var fila = await _filaRepository.GetByIdWithLotesAsync(id, cancellationToken);
        if (fila == null)
            throw new NotFoundException(nameof(Fila), id);

        return ApiResponse<FilaDto>.Ok(MapToDto(fila));
    }

    private static FilaDto MapToDto(Fila f)
    {
        return new FilaDto
        {
            Id = f.Id,
            SucursalId = f.SucursalId,
            NombreSucursal = f.Sucursal?.Nombre ?? string.Empty,
            TemporadaId = f.TemporadaId,
            NombreTemporada = f.Temporada?.Nombre ?? string.Empty,
            Identificacion = f.Identificacion,
            Capacidad = f.Capacidad,
            Estado = f.Estado,
            TotalLotes = f.Lotes?.Count ?? 0,
            FechaCreacion = f.FechaCreacion
        };
    }
}
