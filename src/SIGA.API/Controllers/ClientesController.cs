using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Clientes;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class ClientesController : BaseApiController
{
    private readonly IGestionClientes _gestionClientes;

    public ClientesController(IGestionClientes gestionClientes)
    {
        _gestionClientes = gestionClientes;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClienteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClienteDto>>>> GetAll([FromQuery] string? busqueda, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.GetAllAsync(busqueda, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.GetByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("codigo/{codigo}")]
    [ProducesResponseType(typeof(ApiResponse<ClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> GetByCodigo(string codigo, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.GetByCodigoAsync(codigo, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClienteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Create([FromBody] CreateClienteDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.CreateAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Update(int id, [FromBody] UpdateClienteDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.UpdateAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionClientes.DeleteAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
