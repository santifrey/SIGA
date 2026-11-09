using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;

namespace SIGA.Application.Interfaces;

public interface IGestionLotes
{
    Task<ApiResponse<IEnumerable<LoteDto>>> ListarPorTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<LoteDto>>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoteDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
