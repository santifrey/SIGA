using SIGA.Application.Common;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionTemporadas : IGestionTemporadas
{
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionTemporadas(
        ITemporadaRepository temporadaRepository, 
        IUnitOfWork unitOfWork)
    {
        _temporadaRepository = temporadaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<TemporadaDto>> CrearTemporadaAsync(
        CreateTemporadaDto dto, 
        int usuarioId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre de la temporada es obligatorio.");

        var existente = await _temporadaRepository.ExistsAsync(
            t => t.Nombre.ToLower() == dto.Nombre.Trim().ToLower() && t.Estado != EstadoTemporada.Cerrada, 
            cancellationToken);

        if (existente)
            throw new ValidationException($"Ya existe una temporada activa con el nombre '{dto.Nombre.Trim()}'.");

        var temporada = new Temporada(dto.Nombre, usuarioId, dto.FechaInicio);

        await _temporadaRepository.AddAsync(temporada, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var creada = await _temporadaRepository.GetByIdWithDetailsAsync(temporada.Id, cancellationToken);
        return ApiResponse<TemporadaDto>.Created(MapToDto(creada ?? temporada), "Temporada creada exitosamente.");
    }

    public async Task<ApiResponse<TemporadaDto>> ObtenerActivaAsync(CancellationToken cancellationToken = default)
    {
        var activa = await _temporadaRepository.GetActivaAsync(cancellationToken);
        if (activa == null)
            throw new NotFoundException("No existe ninguna temporada activa en el sistema.");

        return ApiResponse<TemporadaDto>.Ok(MapToDto(activa));
    }

    public async Task<ApiResponse<TemporadaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), id);

        return ApiResponse<TemporadaDto>.Ok(MapToDto(temporada));
    }

    public async Task<ApiResponse<IEnumerable<TemporadaDto>>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var temporadas = await _temporadaRepository.GetAllWithDetailsAsync(cancellationToken);
        var dtos = temporadas.Select(MapToDto);
        return ApiResponse<IEnumerable<TemporadaDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<TemporadaDto>> CambiarEstadoAsync(
        int id, 
        EstadoTemporada nuevoEstado, 
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), id);

        if (temporada.Estado == nuevoEstado)
            return ApiResponse<TemporadaDto>.Ok(MapToDto(temporada), "La temporada ya se encuentra en dicho estado.");

        switch (nuevoEstado)
        {
            case EstadoTemporada.Plantacion:
                temporada.IniciarPlantacion();
                break;
            case EstadoTemporada.EnCrecimiento:
                temporada.FinalizarPlantacion();
                break;
            case EstadoTemporada.Cosecha:
                temporada.IniciarCosecha();
                break;
            case EstadoTemporada.Comercializacion:
                temporada.HabilitarComercializacion();
                break;
            case EstadoTemporada.Cerrada:
                temporada.Cerrar();
                break;
            default:
                throw new OperacionInvalidaException($"Transición al estado '{nuevoEstado}' no permitida.");
        }

        await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<TemporadaDto>.Ok(MapToDto(temporada), $"Estado de la temporada actualizado a '{nuevoEstado}'.");
    }

    public async Task<ApiResponse<TemporadaDto>> CerrarAsync(int id, CancellationToken cancellationToken = default)
    {
        return await CambiarEstadoAsync(id, EstadoTemporada.Cerrada, cancellationToken);
    }

    private static TemporadaDto MapToDto(Temporada t)
    {
        return new TemporadaDto
        {
            Id = t.Id,
            Nombre = t.Nombre,
            FechaInicio = t.FechaInicio,
            FechaFin = t.FechaFin,
            Estado = t.Estado,
            UsuarioCreadorId = t.UsuarioCreadorId,
            TotalFilas = t.Filas?.Count ?? 0,
            TotalLotes = t.Lotes?.Count ?? 0,
            TotalPlantaciones = t.Plantaciones?.Count ?? 0,
            FechaCreacion = t.FechaCreacion
        };
    }
}
