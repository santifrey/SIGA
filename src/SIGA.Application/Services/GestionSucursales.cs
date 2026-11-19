using SIGA.Application.Common;
using SIGA.Application.DTOs.Sucursales;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionSucursales : IGestionSucursales
{
    private readonly ISucursalRepository _sucursalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GestionSucursales(ISucursalRepository sucursalRepository, IUnitOfWork unitOfWork)
    {
        _sucursalRepository = sucursalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<SucursalDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sucursal = await _sucursalRepository.GetByIdAsync(id, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), id);

        return ApiResponse<SucursalDto>.Ok(MapToDto(sucursal));
    }

    public async Task<ApiResponse<IEnumerable<SucursalDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sucursales = await _sucursalRepository.GetAllAsync(cancellationToken);
        // Excluir la contraparte técnica 'Ajustes de Inventario' para que no figure como sucursal física operativa
        var dtos = sucursales
            .Where(s => !string.Equals(s.Nombre, "Ajustes de Inventario", StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto);
        return ApiResponse<IEnumerable<SucursalDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<SucursalDto>> CreateAsync(CreateSucursalDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre de la sucursal es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Ubicacion))
            throw new ValidationException("La ubicación es obligatoria.");

        var existing = await _sucursalRepository.GetByNombreAsync(dto.Nombre.Trim(), cancellationToken);
        if (existing != null)
            throw new ValidationException($"Ya existe una sucursal con el nombre '{dto.Nombre}'.");

        var sucursal = new Sucursal
        {
            Nombre = dto.Nombre.Trim(),
            Ubicacion = dto.Ubicacion.Trim(),
            CapacidadTotal = dto.CapacidadTotal,
            Activo = dto.Activo,
            RazonSocial = $"Sucursal {dto.Nombre.Trim()}",
            DireccionFiscal = dto.Ubicacion.Trim(),
            CUIT = "00-00000000-0",
            TipoParticipante = TipoParticipante.Sucursal,
            Estado = dto.Activo ? EstadoParticipante.Activo : EstadoParticipante.Inactivo,
            FechaRegistro = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        };

        await _sucursalRepository.AddAsync(sucursal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<SucursalDto>.Created(MapToDto(sucursal), "Sucursal creada exitosamente.");
    }

    public async Task<ApiResponse<SucursalDto>> UpdateAsync(int id, UpdateSucursalDto dto, CancellationToken cancellationToken = default)
    {
        var sucursal = await _sucursalRepository.GetByIdAsync(id, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), id);

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre de la sucursal es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Ubicacion))
            throw new ValidationException("La ubicación es obligatoria.");

        var existing = await _sucursalRepository.GetByNombreAsync(dto.Nombre.Trim(), cancellationToken);
        if (existing != null && existing.Id != id)
            throw new ValidationException($"El nombre '{dto.Nombre}' ya está siendo utilizado por otra sucursal.");

        sucursal.Nombre = dto.Nombre.Trim();
        sucursal.Ubicacion = dto.Ubicacion.Trim();
        sucursal.CapacidadTotal = dto.CapacidadTotal;
        sucursal.Activo = dto.Activo;
        sucursal.Estado = dto.Activo ? EstadoParticipante.Activo : EstadoParticipante.Inactivo;
        sucursal.FechaModificacion = DateTime.UtcNow;

        if (!sucursal.EsCasaMatriz)
        {
            sucursal.RazonSocial = $"Sucursal {sucursal.Nombre}";
            sucursal.DireccionFiscal = sucursal.Ubicacion;
        }

        await _sucursalRepository.UpdateAsync(sucursal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<SucursalDto>.Ok(MapToDto(sucursal), "Sucursal actualizada exitosamente.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var sucursal = await _sucursalRepository.GetByIdAsync(id, cancellationToken);
        if (sucursal == null)
            throw new NotFoundException(nameof(Sucursal), id);

        await _sucursalRepository.DeleteAsync(sucursal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Sucursal eliminada exitosamente.");
    }

    private static SucursalDto MapToDto(Sucursal sucursal)
    {
        return new SucursalDto
        {
            Id = sucursal.Id,
            Nombre = sucursal.Nombre,
            Ubicacion = sucursal.Ubicacion,
            Activo = sucursal.Activo,
            CapacidadTotal = sucursal.CapacidadTotal,
            ParticipanteComercialId = sucursal.Id, // El ID de la sucursal es su ID de participante comercial
            ParticipanteRazonSocial = sucursal.RazonSocial ?? sucursal.Nombre,
            FechaCreacion = sucursal.FechaCreacion,
            FechaModificacion = sucursal.FechaModificacion
        };
    }
}
