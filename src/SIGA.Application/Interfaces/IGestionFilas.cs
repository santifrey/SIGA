using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;

namespace SIGA.Application.Interfaces;

public interface IGestionFilas
{
    Task<ApiResponse<IEnumerable<FilaDto>>> ListarPorSucursalYTemporadaAsync(int sucursalId, int temporadaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<FilaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
