using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionMovimientos
{
    Task<ApiResponse<MovimientoBulbosDto>> CrearMovimientoAsync(CrearMovimientoDto dto, int usuarioCreadorId, CancellationToken cancellationToken = default);
    Task<ApiResponse<MovimientoBulbosDto>> ConfirmarMovimientoAsync(int movimientoId, int usuarioConfirmadorId, CancellationToken cancellationToken = default);
    Task<ApiResponse<MovimientoBulbosDto>> AnularMovimientoAsync(int movimientoId, string motivo, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<MovimientoBulbosDto>>> ListarMovimientosAsync(EstadoMovimiento? estado = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<MovimientoBulbosDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<MovimientoBulbosDto>>> ObtenerPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
}
