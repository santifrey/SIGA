using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Authorize]
public class TransferenciasController : BaseApiController
{
    private readonly IGestionTransferencias _gestionTransferencias;

    public TransferenciasController(IGestionTransferencias gestionTransferencias)
    {
        _gestionTransferencias = gestionTransferencias;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransferenciaLoteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TransferenciaLoteDto>>>> GetAll(
        [FromQuery] int? loteId,
        [FromQuery] EstadoTransferencia? estado,
        CancellationToken cancellationToken)
    {
        var result = await _gestionTransferencias.ListarTransferenciasAsync(loteId, estado, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TransferenciaLoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransferenciaLoteDto>>> GetById(
        int id, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionTransferencias.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("lote/{loteId}/historial")]
    [ProducesResponseType(typeof(ApiResponse<HistorialTitularidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HistorialTitularidadDto>>> GetHistorialTitularidad(
        int loteId, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionTransferencias.ObtenerHistorialTitularidadAsync(loteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransferenciaLoteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TransferenciaLoteDto>>> TransferirLote(
        [FromBody] TransferirLoteDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionTransferencias.TransferirLoteAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/confirmar")]
    [ProducesResponseType(typeof(ApiResponse<TransferenciaLoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransferenciaLoteDto>>> Confirm(
        int id, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionTransferencias.ConfirmarTransferenciaAsync(id, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/anular")]
    [ProducesResponseType(typeof(ApiResponse<TransferenciaLoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransferenciaLoteDto>>> Annul(
        int id, 
        [FromBody] AnularTransferenciaDto? dto, 
        CancellationToken cancellationToken)
    {
        var motivo = dto?.Motivo ?? "Anulación solicitada por el usuario";
        var result = await _gestionTransferencias.AnularTransferenciaAsync(id, motivo, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
