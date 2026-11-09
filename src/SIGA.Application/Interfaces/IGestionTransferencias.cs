using SIGA.Application.Common;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionTransferencias
{
    Task<ApiResponse<TransferenciaLoteDto>> TransferirLoteAsync(TransferirLoteDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<TransferenciaLoteDto>> ConfirmarTransferenciaAsync(int transferenciaId, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<TransferenciaLoteDto>> AnularTransferenciaAsync(int transferenciaId, string motivo, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<HistorialTitularidadDto>> ObtenerHistorialTitularidadAsync(int loteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<TransferenciaLoteDto>>> ListarTransferenciasAsync(int? loteId = null, EstadoTransferencia? estado = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<TransferenciaLoteDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
