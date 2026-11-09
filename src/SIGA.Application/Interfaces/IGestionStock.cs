using SIGA.Application.Common;
using SIGA.Application.DTOs.Stock;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionStock
{
    Task<ApiResponse<StockClienteDto>> ConsultarStockPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DisponibilidadStockResponseDto>> VerificarDisponibilidadAsync(int clienteId, ClaseBulbo clase, int cantidad, CancellationToken cancellationToken = default);
    Task<Dictionary<ClaseBulbo, int>> ObtenerSaldoPorClaseAsync(int clienteId, CancellationToken cancellationToken = default);
}
