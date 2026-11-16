using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Route("api/v1/movimientos-comerciales")]
public class MovimientosComercialesController : BaseApiController
{
    private readonly IGestionMovimientosComerciales _gestionMovimientosComerciales;

    public MovimientosComercialesController(IGestionMovimientosComerciales gestionMovimientosComerciales)
    {
        _gestionMovimientosComerciales = gestionMovimientosComerciales;
    }

    [HttpGet]
    [RequierePermiso(Permisos.MovComVer)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MovimientoComercialDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MovimientoComercialDto>>>> GetAll(
        [FromQuery] FiltroMovimientosComercialesDto filtro,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.ListarAsync(filtro, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(Permisos.MovComVer)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("cliente/{clienteId:int}")]
    [RequierePermiso(Permisos.MovComVer)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MovimientoComercialDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MovimientoComercialDto>>>> GetByCliente(
        int clienteId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.ListarPorClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("verificar-cliente-nuevo/{clienteId:int}")]
    [RequierePermiso(Permisos.MovComVer)]
    [ProducesResponseType(typeof(ApiResponse<VerificarClienteNuevoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VerificarClienteNuevoDto>>> VerificarClienteNuevo(
        int clienteId,
        [FromQuery] int? excludeId,
        [FromQuery] int? excludeMovimientoId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.VerificarClienteNuevoAsync(
            clienteId,
            excludeId ?? excludeMovimientoId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.MovComCrear)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> Create(
        [FromBody] CrearMovimientoComercialDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.CrearMovimientoAsync(
            dto,
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Permisos.MovComCrear, Permisos.MovComCorregir)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> Update(
        int id,
        [FromBody] ActualizarMovimientoComercialDto dto,
        CancellationToken cancellationToken)
    {
        bool tienePermisoCorregir = User.IsInRole("ADMINISTRADOR") ||
            User.HasClaim(c => (c.Type == "permiso" || c.Type == "permission") &&
                               c.Value.Equals(Permisos.MovComCorregir, StringComparison.OrdinalIgnoreCase));

        var result = await _gestionMovimientosComerciales.EditarYReenviarAsync(
            id,
            dto,
            CurrentUserId,
            tienePermisoCorregir,
            cancellationToken);

        return CustomResult(result);
    }

    [HttpPost("{id:int}/aprobar-admin")]
    [RequierePermiso(Permisos.MovComAprobar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> AprobarAdmin(
        int id,
        [FromBody] AprobarAdminDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.AprobarAdminAsync(
            id,
            dto ?? new AprobarAdminDto(),
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/aprobar")]
    [RequierePermiso(Permisos.MovComAprobar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> AprobarAlias(
        int id,
        [FromBody] AprobarAdminDto? dto,
        CancellationToken cancellationToken)
    {
        return await AprobarAdmin(id, dto, cancellationToken);
    }

    [HttpPost("{id:int}/contrato")]
    [RequierePermiso(Permisos.MovComAprobar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> GenerarContrato(
        int id,
        [FromBody] GenerarContratoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.GenerarContratoAsync(
            id,
            dto,
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/facturar")]
    [RequierePermiso(Permisos.MovComFacturar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> Facturar(
        int id,
        [FromBody] FacturarDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.FacturarAsync(
            id,
            dto,
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/finalizar")]
    [RequierePermiso(Permisos.MovComFinalizar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> Finalizar(
        int id,
        [FromBody] FinalizarDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.FinalizarAsync(
            id,
            dto ?? new FinalizarDto(),
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/rechazar")]
    [RequierePermiso(Permisos.MovComRechazar)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoComercialDto>>> Rechazar(
        int id,
        [FromBody] RechazarDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientosComerciales.RechazarAsync(
            id,
            dto,
            CurrentUserId,
            cancellationToken);
        return CustomResult(result);
    }
}
