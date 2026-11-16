using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Permisos;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Route("api/v1/permisos")]
public class PermisosController : BaseApiController
{
    private readonly IGestionPermisos _gestionPermisos;

    public PermisosController(IGestionPermisos gestionPermisos)
    {
        _gestionPermisos = gestionPermisos;
    }

    [HttpGet]
    [RequierePermiso(Permisos.PermisosVer)]
    [ProducesResponseType(typeof(ApiResponse<List<PermisoDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PermisoDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionPermisos.GetAllPermisosAsync(cancellationToken);
        return CustomResult(result);
    }
}
