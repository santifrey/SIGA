using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByUsernameWithRolesAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLower();
        return await _dbSet
            .Include(u => u.Persona)
                .ThenInclude(p => p.Sucursal)
            .Include(u => u.Rol)
                .ThenInclude(r => r.RolPermisos)
                    .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || (u.Persona != null && u.Persona.Email != null && u.Persona.Email.ToLower() == normalized), cancellationToken);
    }

    public async Task<Usuario?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Persona)
                .ThenInclude(p => p.Sucursal)
            .Include(u => u.Rol)
                .ThenInclude(r => r.RolPermisos)
                    .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> GetAllWithRolesAndPersonaAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Persona)
                .ThenInclude(p => p.Sucursal)
            .Include(u => u.Rol)
                .ThenInclude(r => r.RolPermisos)
                    .ThenInclude(rp => rp.Permiso)
            .ToListAsync(cancellationToken);
    }
}
