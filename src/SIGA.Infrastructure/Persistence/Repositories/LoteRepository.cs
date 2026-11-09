using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class LoteRepository : Repository<Lote>, ILoteRepository
{
    public LoteRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Lote>> GetByTemporadaAsync(int temporadaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Fila)
                .ThenInclude(f => f.Sucursal)
            .Include(l => l.Cliente)
                .ThenInclude(c => c.Persona)
            .Include(l => l.Temporada)
            .Include(l => l.DetallesPlantacion)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.NuevoTitular)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.TitularAnterior)
            .Where(l => l.TemporadaId == temporadaId)
            .OrderBy(l => l.FilaId)
            .ThenBy(l => l.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lote>> GetByClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Fila)
            .Include(l => l.Temporada)
            .Include(l => l.DetallesPlantacion)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.NuevoTitular)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.TitularAnterior)
            .Where(l => l.ClienteId == clienteId)
            .OrderByDescending(l => l.TemporadaId)
            .ThenBy(l => l.FilaId)
            .ThenBy(l => l.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lote?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Fila)
                .ThenInclude(f => f.Sucursal)
            .Include(l => l.Cliente)
                .ThenInclude(c => c.Persona)
            .Include(l => l.Temporada)
            .Include(l => l.DetallesPlantacion)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.NuevoTitular)
            .Include(l => l.Transferencias)
                .ThenInclude(t => t.TitularAnterior)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<int> GetNextOrdenAsync(int filaId, CancellationToken cancellationToken = default)
    {
        var maxOrden = await _dbSet
            .Where(l => l.FilaId == filaId)
            .Select(l => (int?)l.Orden)
            .MaxAsync(cancellationToken);

        return (maxOrden ?? 0) + 1;
    }
}
