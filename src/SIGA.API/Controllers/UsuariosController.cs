using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Usuarios;
using SIGA.Application.Interfaces;
using SIGA.Domain.Enums;

namespace SIGA.API.Controllers;

[Authorize(Roles = "ADMINISTRADOR")]
public class UsuariosController : BaseApiController
{
    private readonly IGestionUsuarios _gestionUsuarios;

    public UsuariosController(IGestionUsuarios gestionUsuarios)
    {
        _gestionUsuarios = gestionUsuarios;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UsuarioDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuarioDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.GetAllAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UsuarioDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.GetByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UsuarioDto>>> Create([FromBody] CreateUsuarioDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.CreateAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UsuarioDto>>> Update(int id, [FromBody] UpdateUsuarioDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.UpdateAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.DeleteAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}/estado")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarEstado(int id, [FromBody] CambiarEstadoUsuarioDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.CambiarEstadoAsync(id, dto.Estado, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost("{id}/roles")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> AsignarRoles(int id, [FromBody] AsignarRolesDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.AsignarRolesAsync(id, dto.RolIds, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}/password")]
    [Authorize] // Permite al propio usuario o admin cambiar contraseña
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarPassword(int id, [FromBody] CambiarPasswordDto dto, CancellationToken cancellationToken)
    {
        // Un usuario sólo puede cambiar su propia contraseña a menos que sea ADMINISTRADOR
        if (CurrentUserId != id && !User.IsInRole("ADMINISTRADOR"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<bool>.Failure("No tiene permisos para modificar la contraseña de otro usuario.", 403));
        }

        var result = await _gestionUsuarios.CambiarPasswordAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }
}
