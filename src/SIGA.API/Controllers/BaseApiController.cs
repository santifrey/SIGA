using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SIGA.Application.Common;

namespace SIGA.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    protected string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    protected ActionResult<ApiResponse<T>> CustomResult<T>(ApiResponse<T> response)
    {
        return StatusCode(response.StatusCode, response);
    }
}
