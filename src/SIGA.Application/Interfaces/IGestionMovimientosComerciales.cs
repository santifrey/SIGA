using SIGA.Application.Common;
using SIGA.Application.DTOs.MovimientosComerciales;

namespace SIGA.Application.Interfaces;

public interface IGestionMovimientosComerciales
{
    Task<ApiResponse<MovimientoComercialDto>> CrearMovimientoAsync(
        CrearMovimientoComercialDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> AprobarAdminAsync(
        int id,
        AprobarAdminDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> GenerarContratoAsync(
        int id,
        GenerarContratoDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> FacturarAsync(
        int id,
        FacturarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> FinalizarAsync(
        int id,
        FinalizarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> RechazarAsync(
        int id,
        RechazarDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> EditarYReenviarAsync(
        int id,
        ActualizarMovimientoComercialDto dto,
        int usuarioId,
        bool tienePermisoCorregir,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<VerificarClienteNuevoDto>> VerificarClienteNuevoAsync(
        int clienteId,
        int? excludeMovimientoId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<MovimientoComercialDto>>> ListarAsync(
        FiltroMovimientosComercialesDto filtro,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<MovimientoComercialDto>>> ListarPorClienteAsync(
        int clienteId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MovimientoComercialDto>> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
