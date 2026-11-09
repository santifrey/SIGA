using SIGA.Application.Common;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.Plantaciones;

namespace SIGA.Application.Interfaces;

public interface IGestionPlantaciones
{
    Task<ApiResponse<PlantacionDto>> CrearPlantacionAsync(CreatePlantacionDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoteDto>> RegistrarDistribucionAsync(RegistrarDistribucionDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PlantacionDto>> ObtenerPorTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
}
