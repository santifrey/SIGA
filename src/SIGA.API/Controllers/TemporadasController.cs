using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

public class TemporadasController : BaseApiController
{
    private readonly IGestionTemporadas _gestionTemporadas;

    public TemporadasController(IGestionTemporadas gestionTemporadas)
    {
        _gestionTemporadas = gestionTemporadas;
    }

    [HttpGet]
    [RequierePermiso(Permisos.TemporadasVer)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TemporadaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TemporadaDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ListarAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("activa")]
    [RequierePermiso(Permisos.TemporadasVer)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> GetActiva(CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ObtenerActivaAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(Permisos.TemporadasVer)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.TemporadasGestionar)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> Create([FromBody] CreateTemporadaDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.CrearTemporadaAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/cambiar-estado")]
    [RequierePermiso(Permisos.TemporadasGestionar)]
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

    [HttpPost("{id:int}/cerrar")]
    [RequierePermiso(Permisos.TemporadasGestionar)]
    [ProducesResponseType(typeof(ApiResponse<TemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemporadaDto>>> Cerrar(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionTemporadas.CerrarAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
