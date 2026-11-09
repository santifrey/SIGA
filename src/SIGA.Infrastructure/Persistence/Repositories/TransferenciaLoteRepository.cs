using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class TransferenciaLoteRepository : Repository<TransferenciaLote>, ITransferenciaLoteRepository
{
    public TransferenciaLoteRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<TransferenciaLote?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Lote)
                .ThenInclude(l => l.Fila)
                    .ThenInclude(f => f.Sucursal)
            .Include(t => t.Lote)
                .ThenInclude(l => l.Temporada)
            .Include(t => t.TitularAnterior)
            .Include(t => t.NuevoTitular)
            .Include(t => t.UsuarioCreador)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TransferenciaLote>> GetByLoteIdAsync(int loteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TitularAnterior)
            .Include(t => t.NuevoTitular)
            .Include(t => t.UsuarioCreador)
            .Where(t => t.LoteId == loteId)
            .OrderByDescending(t => t.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TransferenciaLote>> GetAllWithDetailsAsync(EstadoTransferencia? estado = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Lote)
                .ThenInclude(l => l.Fila)
                    .ThenInclude(f => f.Sucursal)
            .Include(t => t.TitularAnterior)
            .Include(t => t.NuevoTitular)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(t => t.Estado == estado.Value);
        }

        return await query
            .OrderByDescending(t => t.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPendingTransferAsync(int loteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(t => t.LoteId == loteId && t.Estado == EstadoTransferencia.Pendiente, cancellationToken);
    }
}
