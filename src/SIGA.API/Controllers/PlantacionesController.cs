using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class PlantacionesController : BaseApiController
{
    private readonly IGestionPlantaciones _gestionPlantaciones;

    public PlantacionesController(IGestionPlantaciones gestionPlantaciones)
    {
        _gestionPlantaciones = gestionPlantaciones;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<PlantacionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PlantacionDto>>> Create(
        [FromBody] CreatePlantacionDto dto, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionPlantaciones.CrearPlantacionAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("por-temporada/{temporadaId}")]
    [ProducesResponseType(typeof(ApiResponse<PlantacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlantacionDto>>> GetByTemporada(
        int temporadaId, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionPlantaciones.ObtenerPorTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("distribucion")]
    [HttpPost("{id}/distribucion")]
    [Authorize(Roles = Roles.Gestion)]
    [ProducesResponseType(typeof(ApiResponse<LoteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoteDto>>> RegistrarDistribucion(
        int? id,
        [FromBody] RegistrarDistribucionDto dto, 
        CancellationToken cancellationToken)
    {
        if (id.HasValue && id.Value > 0)
        {
            dto.PlantacionId = id.Value;
        }

        var result = await _gestionPlantaciones.RegistrarDistribucionAsync(dto, CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
