using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Interfaces;

public interface ITransferenciaLoteRepository : IRepository<TransferenciaLote>
{
    Task<TransferenciaLote?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransferenciaLote>> GetByLoteIdAsync(int loteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransferenciaLote>> GetAllWithDetailsAsync(EstadoTransferencia? estado = null, CancellationToken cancellationToken = default);
    Task<bool> HasPendingTransferAsync(int loteId, CancellationToken cancellationToken = default);
}
