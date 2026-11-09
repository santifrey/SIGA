using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IOperacionComercialRepository : IRepository<OperacionComercial>
{
    Task<OperacionComercial?> GetByIdWithMovimientosAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperacionComercial>> GetAllWithMovimientosAsync(CancellationToken cancellationToken = default);
}
