using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IRolRepository : IRepository<Rol>
{
    Task<Rol?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<Rol?> GetByIdWithPermisosAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rol>> GetAllWithPermisosAsync(CancellationToken cancellationToken = default);
}
