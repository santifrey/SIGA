using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.Exceptions;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionFilas : IGestionFilas
{
    private readonly IFilaRepository _filaRepository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IUnitOfWork? _unitOfWork;

    public GestionFilas(
        IFilaRepository filaRepository,
        ISucursalRepository sucursalRepository,
        ITemporadaRepository temporadaRepository,
        IUnitOfWork? unitOfWork = null)
    {
        _filaRepository = filaRepository;
        _sucursalRepository = sucursalRepository;
        _temporadaRepository = temporadaRepository;
        _unitOfWork = unitOfWork;
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

    public async Task<ApiResponse<FilaDto>> CrearFilaAsync(
        CreateFilaDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (dto.Capacidad < 0)
            throw new ValidationException("La capacidad no puede ser negativa.");

        if (dto.NumeroFila.HasValue && dto.NumeroFila.Value <= 0)
            throw new ValidationException("El número de fila debe ser un entero mayor a cero.");

        var sucursal = await _sucursalRepository.GetByIdAsync(dto.SucursalId, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), dto.SucursalId);

        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        if (temporada.Estado == EstadoTemporada.Cerrada)
            throw new ValidationException($"No se pueden crear filas en una temporada cerrada (Temporada '{temporada.Nombre}').");

        var identificacion = !string.IsNullOrWhiteSpace(dto.Identificacion)
            ? dto.Identificacion.Trim()
            : (dto.NumeroFila.HasValue ? dto.NumeroFila.Value.ToString() : string.Empty);

        if (string.IsNullOrWhiteSpace(identificacion))
            throw new ValidationException("Debe especificar la identificación o número de la fila.");

        int? numeroFila = dto.NumeroFila;
        if (!numeroFila.HasValue && int.TryParse(identificacion, out var numParsed) && numParsed > 0)
        {
            numeroFila = numParsed;
        }

        var existente = await _filaRepository.GetByIdentificacionAsync(dto.SucursalId, dto.TemporadaId, identificacion, cancellationToken);
        if (existente != null)
            throw new ValidationException($"Ya existe una fila con la identificación '{identificacion}' para esta sucursal y temporada.");

        var fila = new Fila(dto.SucursalId, dto.TemporadaId, identificacion, dto.Capacidad)
        {
            NumeroFila = numeroFila
        };

        await _filaRepository.AddAsync(fila, cancellationToken);
        if (_unitOfWork != null)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<FilaDto>.Created(MapToDto(fila), "Fila creada exitosamente.");
    }

    private static FilaDto MapToDto(Fila f)
    {
        var ocupacion = f.Lotes?.Where(l => !l.IsDeleted).Sum(l => l.CantidadPlantadaTotal) ?? 0;
        var capacidadDisponible = f.Capacidad > 0 ? Math.Max(0, f.Capacidad - ocupacion) : f.Capacidad;

        int? numeroFila = f.NumeroFila;
        if (!numeroFila.HasValue && int.TryParse(f.Identificacion, out var parsed))
        {
            numeroFila = parsed;
        }

        return new FilaDto
        {
            Id = f.Id,
            SucursalId = f.SucursalId,
            NombreSucursal = f.Sucursal?.Nombre ?? string.Empty,
            TemporadaId = f.TemporadaId,
            NombreTemporada = f.Temporada?.Nombre ?? string.Empty,
            NumeroFila = numeroFila,
            Identificacion = f.Identificacion,
            Capacidad = f.Capacidad,
            CapacidadDisponible = capacidadDisponible,
            OcupacionActual = ocupacion,
            Estado = f.Estado,
            TotalLotes = f.Lotes?.Count(l => !l.IsDeleted) ?? 0,
            FechaCreacion = f.FechaCreacion
        };
    }
}
