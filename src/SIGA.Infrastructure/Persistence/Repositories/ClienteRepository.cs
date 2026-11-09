using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Cliente?> GetByIdWithPersonaAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Persona)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cliente?> GetByCodigoWithPersonaAsync(string codigo, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Persona)
            .FirstOrDefaultAsync(c => c.CodigoCliente.ToLower() == codigo.ToLower(), cancellationToken);
    }

    public async Task<Cliente?> GetByPersonaIdAsync(int personaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Persona)
            .FirstOrDefaultAsync(c => c.PersonaId == personaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> GetAllWithPersonaAsync(string? busqueda = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Persona).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var search = busqueda.Trim().ToLower();
            query = query.Where(c =>
                c.CodigoCliente.ToLower().Contains(search) ||
                c.Persona.Nombre.ToLower().Contains(search) ||
                c.Persona.Apellido.ToLower().Contains(search) ||
                c.Persona.Documento.ToLower().Contains(search) ||
                (c.Persona.Email != null && c.Persona.Email.ToLower().Contains(search)));
        }

        return await query.ToListAsync(cancellationToken);
    }
}
