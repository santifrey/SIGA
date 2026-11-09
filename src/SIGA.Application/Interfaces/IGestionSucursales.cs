using SIGA.Application.Common;
using SIGA.Application.DTOs.Sucursales;

namespace SIGA.Application.Interfaces;

public interface IGestionSucursales
{
    Task<ApiResponse<SucursalDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<SucursalDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<SucursalDto>> CreateAsync(CreateSucursalDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<SucursalDto>> UpdateAsync(int id, UpdateSucursalDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
