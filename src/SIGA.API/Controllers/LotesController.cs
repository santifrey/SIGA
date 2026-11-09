using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize]
public class LotesController : BaseApiController
{
    private readonly IGestionLotes _gestionLotes;

    public LotesController(IGestionLotes gestionLotes)
    {
        _gestionLotes = gestionLotes;
    }

    [HttpGet("por-temporada/{temporadaId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LoteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoteDto>>>> GetByTemporada(
        int temporadaId, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionLotes.ListarPorTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("por-cliente/{clienteId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LoteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoteDto>>>> GetByCliente(
        int clienteId, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionLotes.ListarPorClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LoteDto>>> GetById(
        int id, 
        CancellationToken cancellationToken)
    {
        var result = await _gestionLotes.ObtenerPorIdAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
