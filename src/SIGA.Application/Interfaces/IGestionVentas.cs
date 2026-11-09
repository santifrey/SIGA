using SIGA.Application.Common;
using SIGA.Application.DTOs.Ventas;

namespace SIGA.Application.Interfaces;

public interface IGestionVentas
{
    Task<ApiResponse<PrecioClaseDto>> ConfigurarPrecioClaseAsync(UpsertPrecioClaseDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<PrecioClaseDto>>> ListarPreciosPorTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);

    Task<ApiResponse<VentaDto>> CrearVentaAsync(CreateVentaDto dto, int vendedorUsuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> EnviarARevisionAsync(int ventaId, AccionCircuitoVentaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> GenerarContratoAsync(int ventaId, GenerarContratoDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> AprobarAdministracionAsync(int ventaId, AccionCircuitoVentaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> EmitirFacturaAsync(int ventaId, EmitirFacturaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> AprobarFinalAsync(int ventaId, AccionCircuitoVentaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> RechazarVentaAsync(int ventaId, AccionCircuitoVentaDto dto, int usuarioId, CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<VentaDto>>> ListarTodasAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<VentaDto>>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<VentaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
