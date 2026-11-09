using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize]
public class FilasController : BaseApiController
{
    private readonly IGestionFilas _gestionFilas;

    public FilasController(IGestionFilas gestionFilas)
    {
        _gestionFilas = gestionFilas;
    }

    [HttpGet("por-sucursal/{sucursalId}/temporada/{temporadaId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FilaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FilaDto>>>> GetBySucursalYTemporada(
        int sucursalId, 
        int temporadaId, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionFilas.ListarPorSucursalYTemporadaAsync(sucursalId, temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FilaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FilaDto>>> GetById(
        int id, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionFilas.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
