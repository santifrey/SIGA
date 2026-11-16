using SIGA.Application.Common;
using SIGA.Application.DTOs.Roles;

namespace SIGA.Application.Interfaces;

public interface IGestionRoles
{
    Task<ApiResponse<List<RolDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<RolDto>> GetRolByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<RolDto>> CreateRolAsync(CrearRolDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<RolDto>> UpdateRolAsync(int id, ActualizarRolDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteRolAsync(int id, CancellationToken cancellationToken = default);
}
