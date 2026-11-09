using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class SucursalRepository : Repository<Sucursal>, ISucursalRepository
{
    public SucursalRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Sucursal?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Nombre.ToLower() == nombre.ToLower(), cancellationToken);
    }
}
