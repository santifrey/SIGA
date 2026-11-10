using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class StockController : BaseApiController
{
    private readonly IGestionStock _gestionStock;

    public StockController(IGestionStock gestionStock)
    {
        _gestionStock = gestionStock;
    }

    [HttpGet("por-cliente/{clienteId}")]
    [ProducesResponseType(typeof(ApiResponse<StockClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StockClienteDto>>> GetStockPorCliente(int clienteId, CancellationToken cancellationToken)
    {
        var result = await _gestionStock.ConsultarStockPorClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(ApiResponse<DisponibilidadStockResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DisponibilidadStockResponseDto>>> CheckDisponibilidad(
        [FromQuery] int clienteId,
        [FromQuery] ClaseBulbo clase,
        [FromQuery] int cantidad,
        CancellationToken cancellationToken)
    {
        var result = await _gestionStock.VerificarDisponibilidadAsync(clienteId, clase, cantidad, cancellationToken);
        return CustomResult(result);
    }
}
