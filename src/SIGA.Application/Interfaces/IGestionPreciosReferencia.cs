using SIGA.Application.Common;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionPreciosReferencia
{
    Task<ApiResponse<PrecioReferenciaDto>> ConfigurarPrecioReferenciaAsync(
        ConfigurarPrecioReferenciaDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<PrecioReferenciaDto>>> ConfigurarPreciosTemporadaAsync(
        ConfigurarPreciosTemporadaDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<PrecioReferenciaDto>>> ListarPreciosPorTemporadaAsync(
        int temporadaId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PrecioReferenciaDto>> ObtenerPrecioVigenteAsync(
        int temporadaId,
        ClaseBulbo clase,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PrecioReferenciaDto>> ObtenerPrecioReferenciaAsync(
        int temporadaId,
        ClaseBulbo clase,
        CancellationToken cancellationToken = default);
}
