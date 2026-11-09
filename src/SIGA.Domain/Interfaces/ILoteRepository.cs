using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface ILoteRepository : IRepository<Lote>
{
    Task<IReadOnlyList<Lote>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lote>> GetByClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<Lote?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetNextOrdenAsync(int filaId, CancellationToken cancellationToken = default);
}
