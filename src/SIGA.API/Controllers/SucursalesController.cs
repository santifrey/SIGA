using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Sucursales;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

public class SucursalesController : BaseApiController
{
    private readonly IGestionSucursales _gestionSucursales;

    public SucursalesController(IGestionSucursales gestionSucursales)
    {
        _gestionSucursales = gestionSucursales;
    }

    [HttpGet]
    [RequierePermiso(Permisos.SucursalesVer)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SucursalDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SucursalDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.GetAllAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [RequierePermiso(Permisos.SucursalesVer)]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.GetByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [RequierePermiso(Permisos.SucursalesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> Create([FromBody] CreateSucursalDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.CreateAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}")]
    [RequierePermiso(Permisos.SucursalesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> Update(int id, [FromBody] UpdateSucursalDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.UpdateAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id}")]
    [RequierePermiso(Permisos.SucursalesGestionar)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.DeleteAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
