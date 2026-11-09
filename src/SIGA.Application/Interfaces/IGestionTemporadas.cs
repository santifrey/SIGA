using SIGA.Application.Common;
using SIGA.Application.DTOs.Temporadas;
using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface IGestionTemporadas
{
    Task<ApiResponse<TemporadaDto>> CrearTemporadaAsync(CreateTemporadaDto dto, int usuarioId, CancellationToken cancellationToken = default);
    Task<ApiResponse<TemporadaDto>> ObtenerActivaAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<TemporadaDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<TemporadaDto>>> ListarAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<TemporadaDto>> CambiarEstadoAsync(int id, EstadoTemporada nuevoEstado, CancellationToken cancellationToken = default);
    Task<ApiResponse<TemporadaDto>> CerrarAsync(int id, CancellationToken cancellationToken = default);
}
