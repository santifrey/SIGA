using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IPlantacionRepository : IRepository<Plantacion>
{
    Task<Plantacion?> GetByTemporadaIdAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<Plantacion?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
