using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class PersonaRepository : Repository<Persona>, IPersonaRepository
{
    public PersonaRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Persona?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Documento == documento, cancellationToken);
    }

    public async Task<Persona?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }
}
