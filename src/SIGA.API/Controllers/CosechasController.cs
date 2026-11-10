using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Cosechas;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class CosechasController : BaseApiController
{
    private readonly IGestionCosechas _gestionCosechas;

    public CosechasController(IGestionCosechas gestionCosechas)
    {
        _gestionCosechas = gestionCosechas;
    }

    [HttpGet("por-temporada/{temporadaId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CosechaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CosechaDto>>>> GetByTemporada(
        int temporadaId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionCosechas.ListarPorTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CosechaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CosechaDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _gestionCosechas.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<CosechaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CosechaDto>>> Create(
        [FromBody] CreateCosechaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionCosechas.CrearCosechaAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/detalle")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<CosechaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CosechaDto>>> RegistrarDetalle(
        int id,
        [FromBody] RegistrarDetalleCosechaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _gestionCosechas.RegistrarDetalleCosechaAsync(id, dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id:int}/cerrar")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<CosechaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CosechaDto>>> Cerrar(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _gestionCosechas.CerrarCosechaAsync(id, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
