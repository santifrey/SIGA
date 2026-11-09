using SIGA.Application.Common;
using SIGA.Application.DTOs.Cosechas;

namespace SIGA.Application.Interfaces;

public interface IGestionCosechas
{
    Task<ApiResponse<CosechaDto>> CrearCosechaAsync(CreateCosechaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CosechaDto>> RegistrarDetalleCosechaAsync(int cosechaId, RegistrarDetalleCosechaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CosechaDto>> CerrarCosechaAsync(int cosechaId, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<CosechaDto>>> ListarPorTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CosechaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
