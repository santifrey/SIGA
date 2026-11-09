using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class PrecioClaseRepository : Repository<PrecioClase>, IPrecioClaseRepository
{
    public PrecioClaseRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PrecioClase>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Temporada)
            .Where(p => p.TemporadaId == temporadaId)
            .OrderBy(p => p.ClaseBulbo)
            .ToListAsync(cancellationToken);
    }

    public async Task<PrecioClase?> GetByTemporadaAndClaseAsync(int temporadaId, ClaseBulbo clase, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Temporada)
            .FirstOrDefaultAsync(p => p.TemporadaId == temporadaId && p.ClaseBulbo == clase, cancellationToken);
    }
}
