using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Movimientos;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class MovimientosController : BaseApiController
{
    private readonly IGestionMovimientos _gestionMovimientos;

    public MovimientosController(IGestionMovimientos gestionMovimientos)
    {
        _gestionMovimientos = gestionMovimientos;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MovimientoBulbosDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MovimientoBulbosDto>>>> GetAll(
        [FromQuery] EstadoMovimiento? estado,
        [FromQuery] int? clienteId,
        CancellationToken cancellationToken)
    {
        if (clienteId.HasValue && clienteId.Value > 0)
        {
            var resultCliente = await _gestionMovimientos.ObtenerPorClienteAsync(clienteId.Value, cancellationToken);
            return CustomResult(resultCliente);
        }

        var result = await _gestionMovimientos.ListarMovimientosAsync(estado, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MovimientoBulbosDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoBulbosDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientos.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("por-cliente/{clienteId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MovimientoBulbosDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MovimientoBulbosDto>>>> GetByCliente(int clienteId, CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientos.ObtenerPorClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MovimientoBulbosDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MovimientoBulbosDto>>> Create([FromBody] CrearMovimientoDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientos.CrearMovimientoAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/confirmar")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoBulbosDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoBulbosDto>>> Confirm(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionMovimientos.ConfirmarMovimientoAsync(id, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/anular")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<MovimientoBulbosDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovimientoBulbosDto>>> Annul(int id, [FromBody] AnularMovimientoDto? dto, CancellationToken cancellationToken)
    {
        var motivo = dto?.Motivo ?? "Anulación solicitada por el usuario";
        var result = await _gestionMovimientos.AnularMovimientoAsync(id, motivo, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
