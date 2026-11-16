using SIGA.Application.Common;
using SIGA.Application.DTOs.Permisos;

namespace SIGA.Application.Interfaces;

public interface IGestionPermisos
{
    Task<ApiResponse<List<PermisoDto>>> GetAllPermisosAsync(CancellationToken cancellationToken = default);
}
