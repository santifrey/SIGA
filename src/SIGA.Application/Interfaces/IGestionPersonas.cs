using SIGA.Application.Common;
using SIGA.Application.DTOs.Personas;

namespace SIGA.Application.Interfaces;

public interface IGestionPersonas
{
    Task<ApiResponse<PersonaDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PersonaDto>> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<PersonaDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PersonaDto>> CreateAsync(CreatePersonaDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<PersonaDto>> UpdateAsync(int id, UpdatePersonaDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
