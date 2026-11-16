using SIGA.Application.Common;
using SIGA.Application.DTOs.Empleados;

namespace SIGA.Application.Interfaces;

public interface IGestionEmpleados
{
    Task<ApiResponse<List<EmpleadoDto>>> GetEmpleadosAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpleadoDto>> GetEmpleadoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpleadoDto>> CreateEmpleadoAsync(CrearEmpleadoDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpleadoDto>> UpdateEmpleadoAsync(int id, ActualizarEmpleadoDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteEmpleadoAsync(int id, CancellationToken cancellationToken = default);
}
