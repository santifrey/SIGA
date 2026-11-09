using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface ITemporadaRepository : IRepository<Temporada>
{
    Task<Temporada?> GetActivaAsync(CancellationToken cancellationToken = default);
    Task<Temporada?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Temporada>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}
