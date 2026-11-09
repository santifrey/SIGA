using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class RolRepository : Repository<Rol>, IRolRepository
{
    public RolRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Rol?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Nombre.ToLower() == nombre.ToLower(), cancellationToken);
    }
}
