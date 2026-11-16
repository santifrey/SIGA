using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Route("api/v1/empleados")]
public class EmpleadosController : BaseApiController
{
    private readonly IGestionEmpleados _gestionEmpleados;

    public EmpleadosController(IGestionEmpleados gestionEmpleados)
    {
        _gestionEmpleados = gestionEmpleados;
    }

    [HttpGet]
    [RequierePermiso(Permisos.EmpleadosVer)]
    [ProducesResponseType(typeof(ApiResponse<List<EmpleadoDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<EmpleadoDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionEmpleados.GetEmpleadosAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(Permisos.EmpleadosVer)]
    [ProducesResponseType(typeof(ApiResponse<EmpleadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmpleadoDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionEmpleados.GetEmpleadoByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.EmpleadosGestionar)]
    [ProducesResponseType(typeof(ApiResponse<EmpleadoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<EmpleadoDto>>> Create([FromBody] CrearEmpleadoDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionEmpleados.CreateEmpleadoAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Permisos.EmpleadosGestionar)]
    [ProducesResponseType(typeof(ApiResponse<EmpleadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmpleadoDto>>> Update(int id, [FromBody] ActualizarEmpleadoDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionEmpleados.UpdateEmpleadoAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(Permisos.EmpleadosGestionar)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionEmpleados.DeleteEmpleadoAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
