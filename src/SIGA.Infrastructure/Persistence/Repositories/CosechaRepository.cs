using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class CosechaRepository : Repository<Cosecha>, ICosechaRepository
{
    public CosechaRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Cosecha?> GetByTemporadaAndSucursalAsync(int temporadaId, int sucursalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Temporada)
            .Include(c => c.Sucursal)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Fila)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Cliente)
                        .ThenInclude(cl => cl.Persona)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Transferencias)
            .FirstOrDefaultAsync(c => c.TemporadaId == temporadaId && c.SucursalId == sucursalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Cosecha>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Temporada)
            .Include(c => c.Sucursal)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Fila)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Cliente)
                        .ThenInclude(cl => cl.Persona)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Transferencias)
            .Where(c => c.TemporadaId == temporadaId)
            .OrderBy(c => c.SucursalId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cosecha?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Temporada)
            .Include(c => c.Sucursal)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Fila)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Cliente)
                        .ThenInclude(cl => cl.Persona)
            .Include(c => c.DetallesCosecha)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Transferencias)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
