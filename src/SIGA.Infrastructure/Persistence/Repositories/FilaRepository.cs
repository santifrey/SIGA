using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class FilaRepository : Repository<Fila>, IFilaRepository
{
    public FilaRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Fila>> GetBySucursalAndTemporadaAsync(
        int sucursalId, 
        int temporadaId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Sucursal)
            .Include(f => f.Lotes)
                .ThenInclude(l => l.Cliente)
                    .ThenInclude(c => c.Persona)
            .Include(f => f.Lotes)
                .ThenInclude(l => l.DetallesPlantacion)
            .Where(f => f.SucursalId == sucursalId && f.TemporadaId == temporadaId)
            .OrderBy(f => f.Identificacion)
            .ToListAsync(cancellationToken);
    }

    public async Task<Fila?> GetByIdentificacionAsync(
        int sucursalId, 
        int temporadaId, 
        string identificacion, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Lotes)
            .FirstOrDefaultAsync(
                f => f.SucursalId == sucursalId && 
                     f.TemporadaId == temporadaId && 
                     f.Identificacion.ToLower() == identificacion.ToLower().Trim(), 
                cancellationToken);
    }

    public async Task<Fila?> GetByIdWithLotesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Sucursal)
            .Include(f => f.Temporada)
            .Include(f => f.Lotes)
                .ThenInclude(l => l.Cliente)
                    .ThenInclude(c => c.Persona)
            .Include(f => f.Lotes)
                .ThenInclude(l => l.DetallesPlantacion)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }
}
