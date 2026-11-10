using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Reportes;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class ReportesController : BaseApiController
{
    private readonly IGestionReportes _gestionReportes;

    public ReportesController(IGestionReportes gestionReportes)
    {
        _gestionReportes = gestionReportes;
    }

    [HttpGet("cliente/{clienteId:int}/integral")]
    [ProducesResponseType(typeof(ApiResponse<ReporteIntegralClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReporteIntegralClienteDto>>> GetEstadoIntegralCliente(
        int clienteId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionReportes.ObtenerEstadoIntegralClienteAsync(clienteId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("produccion/temporada/{temporadaId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReporteRendimientoTemporadaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReporteRendimientoTemporadaDto>>> GetRendimientoTemporada(
        int temporadaId,
        CancellationToken cancellationToken)
    {
        var result = await _gestionReportes.ObtenerRendimientoTemporadaAsync(temporadaId, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("resumen-ejecutivo")]
    [ProducesResponseType(typeof(ApiResponse<ResumenEjecutivoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ResumenEjecutivoDto>>> GetResumenEjecutivo(
        CancellationToken cancellationToken)
    {
        var result = await _gestionReportes.ObtenerResumenEjecutivoAsync(cancellationToken);
        return CustomResult(result);
    }
}
