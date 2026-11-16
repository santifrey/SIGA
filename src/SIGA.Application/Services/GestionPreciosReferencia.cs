using SIGA.Application.Common;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionPreciosReferencia : IGestionPreciosReferencia
{
    private readonly IPrecioClaseRepository _precioClaseRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly ClaseBulbo[] TodasLasClases =
    {
        ClaseBulbo.Clase0,
        ClaseBulbo.Clase1,
        ClaseBulbo.Clase2,
        ClaseBulbo.Clase3,
        ClaseBulbo.Clase4,
        ClaseBulbo.MIX
    };

    public GestionPreciosReferencia(
        IPrecioClaseRepository precioClaseRepository,
        ITemporadaRepository temporadaRepository,
        IUnitOfWork unitOfWork)
    {
        _precioClaseRepository = precioClaseRepository;
        _temporadaRepository = temporadaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PrecioReferenciaDto>> ConfigurarPrecioReferenciaAsync(
        ConfigurarPrecioReferenciaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(ClaseBulbo), dto.Clase))
            throw new ValidationException("La clase de bulbo especificada no es válida.");

        if (dto.PrecioReferencia <= 0)
            throw new OperacionInvalidaException("El precio de referencia debe ser mayor a cero.");

        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        var existente = await _precioClaseRepository.GetByTemporadaAndClaseAsync(dto.TemporadaId, dto.ClaseBulbo, cancellationToken);
        if (existente != null)
        {
            existente.ActualizarPrecio(dto.PrecioReferencia);
            await _precioClaseRepository.UpdateAsync(existente, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<PrecioReferenciaDto>.Ok(MapPrecioToDto(existente, temporada.Nombre), "Precio de referencia actualizado exitosamente.");
        }

        var nuevo = new PrecioClase(dto.TemporadaId, dto.ClaseBulbo, dto.PrecioReferencia);
        await _precioClaseRepository.AddAsync(nuevo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PrecioReferenciaDto>.Created(MapPrecioToDto(nuevo, temporada.Nombre), "Precio de referencia registrado exitosamente.");
    }

    public async Task<ApiResponse<IEnumerable<PrecioReferenciaDto>>> ConfigurarPreciosTemporadaAsync(
        ConfigurarPreciosTemporadaDto dto,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(dto.TemporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), dto.TemporadaId);

        var existentes = (await _precioClaseRepository.GetByTemporadaAsync(dto.TemporadaId, cancellationToken))
            .ToDictionary(p => p.ClaseBulbo, p => p);

        foreach (var item in dto.Precios)
        {
            if (item.PrecioReferencia <= 0)
                throw new OperacionInvalidaException($"El precio de referencia para {item.ClaseBulbo} debe ser mayor a cero.");

            if (existentes.TryGetValue(item.ClaseBulbo, out var existente))
            {
                existente.ActualizarPrecio(item.PrecioReferencia);
                await _precioClaseRepository.UpdateAsync(existente, cancellationToken);
            }
            else
            {
                var nuevo = new PrecioClase(dto.TemporadaId, item.ClaseBulbo, item.PrecioReferencia);
                await _precioClaseRepository.AddAsync(nuevo, cancellationToken);
                existentes[item.ClaseBulbo] = nuevo;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultado = await ListarPreciosPorTemporadaAsync(dto.TemporadaId, cancellationToken);
        return ApiResponse<IEnumerable<PrecioReferenciaDto>>.Ok(resultado.Data ?? Enumerable.Empty<PrecioReferenciaDto>(), "Precios de temporada configurados exitosamente.");
    }

    public async Task<ApiResponse<IEnumerable<PrecioReferenciaDto>>> ListarPreciosPorTemporadaAsync(
        int temporadaId,
        CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var precios = await _precioClaseRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var preciosDict = precios.ToDictionary(p => p.ClaseBulbo, p => p);

        var dtos = TodasLasClases.Select(clase =>
        {
            if (preciosDict.TryGetValue(clase, out var precio))
            {
                return MapPrecioToDto(precio, temporada.Nombre);
            }

            return new PrecioReferenciaDto
            {
                Id = 0,
                TemporadaId = temporada.Id,
                NombreTemporada = temporada.Nombre,
                ClaseBulbo = clase,
                PrecioReferencia = 0m,
                FechaVigencia = DateTime.UtcNow
            };
        }).ToList();

        return ApiResponse<IEnumerable<PrecioReferenciaDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<PrecioReferenciaDto>> ObtenerPrecioVigenteAsync(
        int temporadaId,
        ClaseBulbo clase,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(ClaseBulbo), clase))
            return ApiResponse<PrecioReferenciaDto>.Failure($"La clase de bulbo especificada ({clase}) no es válida.", 400);

        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var precio = await _precioClaseRepository.GetByTemporadaAndClaseAsync(temporadaId, clase, cancellationToken);
        if (precio != null)
        {
            return ApiResponse<PrecioReferenciaDto>.Ok(MapPrecioToDto(precio, temporada.Nombre));
        }

        return ApiResponse<PrecioReferenciaDto>.Ok(new PrecioReferenciaDto
        {
            Id = 0,
            TemporadaId = temporada.Id,
            NombreTemporada = temporada.Nombre,
            ClaseBulbo = clase,
            PrecioReferencia = 0m,
            FechaVigencia = DateTime.UtcNow
        });
    }

    public Task<ApiResponse<PrecioReferenciaDto>> ObtenerPrecioReferenciaAsync(
        int temporadaId,
        ClaseBulbo clase,
        CancellationToken cancellationToken = default)
    {
        return ObtenerPrecioVigenteAsync(temporadaId, clase, cancellationToken);
    }

    private static PrecioReferenciaDto MapPrecioToDto(PrecioClase precio, string nombreTemporada)
    {
        return new PrecioReferenciaDto
        {
            Id = precio.Id,
            TemporadaId = precio.TemporadaId,
            NombreTemporada = nombreTemporada,
            ClaseBulbo = precio.ClaseBulbo,
            PrecioReferencia = precio.PrecioReferencia,
            FechaVigencia = precio.FechaVigencia
        };
    }
}
