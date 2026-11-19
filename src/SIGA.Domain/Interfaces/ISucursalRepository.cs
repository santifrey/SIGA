using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface ISucursalRepository : IRepository<Sucursal>
{
    Task<Sucursal?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<Sucursal?> GetCasaMatrizAsync(CancellationToken cancellationToken = default);
    Task<Sucursal?> GetByIdWithParticipanteAsync(int id, CancellationToken cancellationToken = default);
    Task<Sucursal?> GetByParticipanteComercialIdAsync(int participanteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sucursal>> GetAllWithParticipanteAsync(CancellationToken cancellationToken = default);
}
