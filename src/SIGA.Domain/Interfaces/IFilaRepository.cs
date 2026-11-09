using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IFilaRepository : IRepository<Fila>
{
    Task<IReadOnlyList<Fila>> GetBySucursalAndTemporadaAsync(int sucursalId, int temporadaId, CancellationToken cancellationToken = default);
    Task<Fila?> GetByIdentificacionAsync(int sucursalId, int temporadaId, string identificacion, CancellationToken cancellationToken = default);
    Task<Fila?> GetByIdWithLotesAsync(int id, CancellationToken cancellationToken = default);
}
