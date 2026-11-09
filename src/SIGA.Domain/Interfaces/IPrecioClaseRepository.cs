using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Interfaces;

public interface IPrecioClaseRepository : IRepository<PrecioClase>
{
    Task<IReadOnlyList<PrecioClase>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<PrecioClase?> GetByTemporadaAndClaseAsync(int temporadaId, ClaseBulbo clase, CancellationToken cancellationToken = default);
}
