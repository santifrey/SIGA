using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Interfaces;

public interface IMovimientoBulbosRepository : IRepository<MovimientoBulbos>
{
    Task<MovimientoBulbos?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoBulbos>> GetAllWithDetailsAsync(EstadoMovimiento? estado = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoBulbos>> GetByParticipanteAsync(int participanteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoBulbos>> GetConfirmadosByParticipanteAsync(int participanteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoBulbos>> GetByOperacionIdAsync(int operacionId, CancellationToken cancellationToken = default);
}
