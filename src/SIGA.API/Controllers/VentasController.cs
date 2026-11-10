using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Ventas;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class VentasController : BaseApiController
{
    private readonly IGestionVentas _gestionVentas;

    public VentasController(IGestionVentas gestionVentas)
    {
        _gestionVentas = gestionVentas;
    }

    [HttpGet("precios/temporada/{temporadaId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrecioClaseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrecioClaseDto>>>> GetPreciosPorTemporada(
        int temporadaId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.ListarPreciosPorTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("precios")]
    [Authorize(Roles = Roles.Contable)]
    [ProducesResponseType(typeof(ApiResponse<PrecioClaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PrecioClaseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PrecioClaseDto>>> UpsertPrecioClase(
        [FromBody] UpsertPrecioClaseDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.ConfigurarPrecioClaseAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VentaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<VentaDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.ListarTodasAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("cliente/{clienteId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VentaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<VentaDto>>>> GetByCliente(
        int clienteId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.ListarPorClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Comercial)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> Create(
        [FromBody] CreateVentaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.CrearVentaAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/enviar-revision")]
    [HttpPost("{id:int}/enviar-aprobacion")]
    [Authorize(Roles = Roles.Comercial)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> EnviarARevision(
        int id,
        [FromBody] AccionCircuitoVentaDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.EnviarARevisionAsync(id, dto ?? new AccionCircuitoVentaDto(), CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/contrato")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> GenerarContrato(
        int id,
        [FromBody] GenerarContratoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.GenerarContratoAsync(id, dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/aprobar-administracion")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> AprobarAdministracion(
        int id,
        [FromBody] AccionCircuitoVentaDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.AprobarAdministracionAsync(id, dto ?? new AccionCircuitoVentaDto(), CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/factura")]
    [Authorize(Roles = Roles.Contable)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> EmitirFactura(
        int id,
        [FromBody] EmitirFacturaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.EmitirFacturaAsync(id, dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/aprobar-final")]
    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> AprobarFinal(
        int id,
        [FromBody] AccionCircuitoVentaDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.AprobarFinalAsync(id, dto ?? new AccionCircuitoVentaDto(), CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = Roles.Revisores)]
    [ProducesResponseType(typeof(ApiResponse<VentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VentaDto>>> Rechazar(
        int id,
        [FromBody] AccionCircuitoVentaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionVentas.RechazarVentaAsync(id, dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
