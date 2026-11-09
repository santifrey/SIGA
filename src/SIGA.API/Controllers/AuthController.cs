using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.Interfaces;

namespace SIGA.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly IGestionUsuarios _gestionUsuarios;

    public AuthController(IGestionUsuarios gestionUsuarios)
    {
        _gestionUsuarios = gestionUsuarios;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.LoginAsync(request, cancellationToken);
        return CustomResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserInfoResponseDto>>> GetMe(CancellationToken cancellationToken)
    {
        var result = await _gestionUsuarios.GetMeAsync(CurrentUserId, cancellationToken);
        return CustomResult(result);
    }
}
