using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Sucursales;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class SucursalesController : BaseApiController
{
    private readonly IGestionSucursales _gestionSucursales;

    public SucursalesController(IGestionSucursales gestionSucursales)
    {
        _gestionSucursales = gestionSucursales;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SucursalDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SucursalDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.GetAllAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.GetByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> Create([FromBody] CreateSucursalDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.CreateAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<SucursalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SucursalDto>>> Update(int id, [FromBody] UpdateSucursalDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.UpdateAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionSucursales.DeleteAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
