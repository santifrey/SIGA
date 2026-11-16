using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.PreciosReferencia;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Route("api/v1/precios-referencia")]
public class PreciosReferenciaController : BaseApiController
{
    private readonly IGestionPreciosReferencia _gestionPreciosReferencia;

    public PreciosReferenciaController(IGestionPreciosReferencia gestionPreciosReferencia)
    {
        _gestionPreciosReferencia = gestionPreciosReferencia;
    }

    [HttpGet("temporada/{temporadaId:int}")]
    [RequierePermiso(Permisos.PreciosRefVer)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrecioReferenciaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrecioReferenciaDto>>>> GetPreciosPorTemporada(
        int temporadaId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionPreciosReferencia.ListarPreciosPorTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("vigente/{temporadaId:int}/{clase}")]
    [RequierePermiso(Permisos.PreciosRefVer)]
    [ProducesResponseType(typeof(ApiResponse<PrecioReferenciaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PrecioReferenciaDto>>> GetPrecioVigente(
        int temporadaId,
        ClaseBulbo clase,
        CancellationToken cancellationToken)
    {
        var result = await _gestionPreciosReferencia.ObtenerPrecioVigenteAsync(temporadaId, clase, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.PreciosRefGestionar)]
    [ProducesResponseType(typeof(ApiResponse<PrecioReferenciaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PrecioReferenciaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PrecioReferenciaDto>>> UpsertPrecio(
        [FromBody] ConfigurarPrecioReferenciaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionPreciosReferencia.ConfigurarPrecioReferenciaAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("temporada")]
    [RequierePermiso(Permisos.PreciosRefGestionar)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrecioReferenciaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrecioReferenciaDto>>>> ConfigurarPreciosTemporada(
        [FromBody] ConfigurarPreciosTemporadaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionPreciosReferencia.ConfigurarPreciosTemporadaAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("temporada/{temporadaId:int}")]
    [RequierePermiso(Permisos.PreciosRefGestionar)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrecioReferenciaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrecioReferenciaDto>>>> UpsertPreciosTemporadaPut(
        int temporadaId,
        [FromBody] ConfigurarPreciosTemporadaDto dto,
        CancellationToken cancellationToken)
    {
        dto.TemporadaId = temporadaId;
        var result = await _gestionPreciosReferencia.ConfigurarPreciosTemporadaAsync(dto, cancellationToken);
        return CustomResult(result);
    }
}
