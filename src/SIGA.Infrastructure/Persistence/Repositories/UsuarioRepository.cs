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
        return await _dbSet
            .Include(u => u.Persona)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }

    public async Task<Usuario?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Persona)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> GetAllWithRolesAndPersonaAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Persona)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .ToListAsync(cancellationToken);
    }
}
