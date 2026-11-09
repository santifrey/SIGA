using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.DTOs.Usuarios;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionUsuarios
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserInfoResponseDto>> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<UsuarioDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDto>> CreateAsync(CreateUsuarioDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDto>> UpdateAsync(int id, UpdateUsuarioDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CambiarEstadoAsync(int id, EstadoUsuario nuevoEstado, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> AsignarRolesAsync(int id, List<int> rolIds, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CambiarPasswordAsync(int id, CambiarPasswordDto dto, CancellationToken cancellationToken = default);
}
