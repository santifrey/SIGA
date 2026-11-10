using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.API.Authorization;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Personas;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

[Authorize(Roles = Roles.Personal)]
public class PersonasController : BaseApiController
{
    private readonly IGestionPersonas _gestionPersonas;

    public PersonasController(IGestionPersonas gestionPersonas)
    {
        _gestionPersonas = gestionPersonas;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PersonaDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PersonaDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.GetAllAsync(cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PersonaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PersonaDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.GetByIdAsync(id, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("documento/{documento}")]
    [ProducesResponseType(typeof(ApiResponse<PersonaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PersonaDto>>> GetByDocumento(string documento, CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.GetByDocumentoAsync(documento, cancellationToken);
        return CustomResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PersonaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PersonaDto>>> Create([FromBody] CreatePersonaDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.CreateAsync(dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PersonaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PersonaDto>>> Update(int id, [FromBody] UpdatePersonaDto dto, CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.UpdateAsync(id, dto, cancellationToken);
        return CustomResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _gestionPersonas.DeleteAsync(id, cancellationToken);
        return CustomResult(result);
    }
}
