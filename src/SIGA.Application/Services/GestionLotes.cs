using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionLotes : IGestionLotes
{
    private readonly ILoteRepository _loteRepository;
    private readonly ITemporadaRepository _temporadaRepository;
    private readonly IClienteRepository _clienteRepository;

    public GestionLotes(
        ILoteRepository loteRepository,
        ITemporadaRepository temporadaRepository,
        IClienteRepository clienteRepository)
    {
        _loteRepository = loteRepository;
        _temporadaRepository = temporadaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<ApiResponse<IEnumerable<LoteDto>>> ListarPorTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default)
    {
        var temporada = await _temporadaRepository.GetByIdAsync(temporadaId, cancellationToken);
        if (temporada == null)
            throw new NotFoundException(nameof(Temporada), temporadaId);

        var lotes = await _loteRepository.GetByTemporadaAsync(temporadaId, cancellationToken);
        var dtos = lotes.Select(MapToDto);
        return ApiResponse<IEnumerable<LoteDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<IEnumerable<LoteDto>>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
        if (cliente == null)
            throw new NotFoundException(nameof(Cliente), clienteId);

        var lotes = await _loteRepository.GetByClienteAsync(clienteId, cancellationToken);
        var dtos = lotes.Select(MapToDto);
        return ApiResponse<IEnumerable<LoteDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<LoteDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var lote = await _loteRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (lote == null)
            throw new NotFoundException(nameof(Lote), id);

        return ApiResponse<LoteDto>.Ok(MapToDto(lote));
    }

    private static LoteDto MapToDto(Lote l)
    {
        return new LoteDto
        {
            Id = l.Id,
            FilaId = l.FilaId,
            IdentificacionFila = l.Fila?.Identificacion ?? string.Empty,
            SucursalId = l.Fila?.SucursalId ?? 0,
            NombreSucursal = l.Fila?.Sucursal?.Nombre ?? string.Empty,
            ClienteId = l.ClienteId,
            CodigoCliente = l.Cliente?.CodigoCliente ?? string.Empty,
            NombreCliente = l.Cliente?.Persona != null ? l.Cliente.Persona.NombreCompleto : string.Empty,
            TitularVigenteId = l.ResolverTitularVigenteId(),
            TemporadaId = l.TemporadaId,
            NombreTemporada = l.Temporada?.Nombre ?? string.Empty,
            Orden = l.Orden,
            CantidadPlantadaTotal = l.CantidadPlantadaTotal,
            Estado = l.Estado,
            Detalles = l.DetallesPlantacion?.Select(d => new DetallePlantacionDto
            {
                Id = d.Id,
                PlantacionId = d.PlantacionId,
                LoteId = d.LoteId,
                ClaseBulbo = d.ClaseBulbo,
                Cantidad = d.Cantidad
            }).ToList() ?? new List<DetallePlantacionDto>(),
            FechaCreacion = l.FechaCreacion
        };
    }
}
