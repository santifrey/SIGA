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
        return await _dbSet
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso)
            .Include(r => r.Usuarios)
            .FirstOrDefaultAsync(r => r.Nombre.ToLower() == nombre.ToLower(), cancellationToken);
    }

    public async Task<Rol?> GetByIdWithPermisosAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso)
            .Include(r => r.Usuarios)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Rol>> GetAllWithPermisosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso)
            .Include(r => r.Usuarios)
            .ToListAsync(cancellationToken);
    }
}
