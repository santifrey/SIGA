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
        if (dto.PorcentajeComisionCompra.HasValue)
        {
            temporada.ActualizarComisionCompra(dto.PorcentajeComisionCompra.Value);
        }

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

        // Regla de Negocio: Solo puede haber una temporada activa en el sistema.
        // Para avanzar de 'Planificada' a cualquier fase operativa (Plantacion, etc.),
        // la temporada anterior debe estar 'Cerrada'.
        if (temporada.Estado == EstadoTemporada.Planificada && nuevoEstado != EstadoTemporada.Planificada && nuevoEstado != EstadoTemporada.Cerrada)
        {
            var todas = await _temporadaRepository.GetAllAsync(cancellationToken);
            var activaAnterior = todas.FirstOrDefault(t => t.Id != id && t.Estado != EstadoTemporada.Cerrada && t.Estado != EstadoTemporada.Planificada);
            if (activaAnterior != null)
            {
                throw new OperacionInvalidaException(
                    $"Solo puede haber una temporada activa. Para avanzar '{temporada.Nombre}' a {nuevoEstado}, la temporada anterior '{activaAnterior.Nombre}' debe estar cerrada (Estado actual: {activaAnterior.Estado}).");
            }
        }

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

    public async Task<ApiResponse<TemporadaDto>> ActualizarComisionCompraAsync(
        int id, 
        decimal porcentaje, 
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), id);

        temporada.ActualizarComisionCompra(porcentaje);
        await _temporadaRepository.UpdateAsync(temporada, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<TemporadaDto>.Ok(MapToDto(temporada), "Porcentaje de comisión de compra actualizado exitosamente.");
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
            PorcentajeComisionCompra = t.PorcentajeComisionCompra,
            UsuarioCreadorId = t.UsuarioCreadorId,
            TotalFilas = t.Filas?.Count ?? 0,
            TotalLotes = t.Lotes?.Count ?? 0,
            TotalPlantaciones = t.Plantaciones?.Count ?? 0,
            FechaCreacion = t.FechaCreacion
        };
    }
}
