using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface ISucursalRepository : IRepository<Sucursal>
{
    Task<Sucursal?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
}
