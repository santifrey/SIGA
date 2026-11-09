using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IVentaRepository : IRepository<Venta>
{
    Task<IReadOnlyList<Venta>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> GetByClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<Venta?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
