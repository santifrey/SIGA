using SIGA.Application.Common;
using SIGA.Application.DTOs.Clientes;

namespace SIGA.Application.Interfaces;

public interface IGestionClientes
{
    Task<ApiResponse<ClienteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ClienteDto>> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<ClienteDto>>> GetAllAsync(string? busqueda = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<ClienteDto>> CreateAsync(CreateClienteDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
