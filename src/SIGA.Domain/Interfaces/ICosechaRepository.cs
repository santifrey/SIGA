using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface ICosechaRepository : IRepository<Cosecha>
{
    Task<Cosecha?> GetByTemporadaAndSucursalAsync(int temporadaId, int sucursalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cosecha>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<Cosecha?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
