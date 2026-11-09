using SIGA.Application.Common;
using SIGA.Application.DTOs.Reportes;

namespace SIGA.Application.Interfaces;

public interface IGestionReportes
{
    Task<ApiResponse<ReporteIntegralClienteDto>> ObtenerEstadoIntegralClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ReporteRendimientoTemporadaDto>> ObtenerRendimientoTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ResumenEjecutivoDto>> ObtenerResumenEjecutivoAsync(CancellationToken cancellationToken = default);
}
