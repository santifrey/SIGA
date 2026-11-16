using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Route("api/v1/roles")]
public class RolesController : BaseApiController
{
    private readonly IGestionRoles _gestionRoles;

    public RolesController(IGestionRoles gestionRoles)
    {
        _gestionRoles = gestionRoles;
    }

    [HttpGet]
    [RequierePermiso(Permisos.RolesVer)]
    [ProducesResponseType(typeof(ApiResponse<List<RolDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RolDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionRoles.GetRolesAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(Permisos.RolesVer)]
    [ProducesResponseType(typeof(ApiResponse<RolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionRoles.GetRolByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.RolesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<RolDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RolDto>>> Create([FromBody] CrearRolDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionRoles.CreateRolAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Permisos.RolesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<RolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolDto>>> Update(int id, [FromBody] ActualizarRolDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionRoles.UpdateRolAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(Permisos.RolesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionRoles.DeleteRolAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
