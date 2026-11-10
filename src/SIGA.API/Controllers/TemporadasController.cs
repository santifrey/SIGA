using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class TemporadasController : BaseApiController
{
    private readonly IGestionTemporadas _gestionTemporadas;

    public TemporadasController(IGestionTemporadas gestionTemporadas)
    {
        _gestionTemporadas = gestionTemporadas;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TemporadaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TemporadaDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ListarAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("activa")]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> GetActiva(CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ObtenerActivaAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> Create([FromBody] CreateTemporadaDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.CrearTemporadaAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/cambiar-estado")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> CambiarEstado(
        int id, 
        [FromBody] CambiarEstadoTemporadaDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.CambiarEstadoAsync(id, dto.NuevoEstado, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/cerrar")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> Cerrar(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.CerrarAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
