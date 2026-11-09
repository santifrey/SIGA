using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class OperacionComercialRepository : Repository<OperacionComercial>, IOperacionComercialRepository
{
    public OperacionComercialRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<OperacionComercial?> GetByIdWithMovimientosAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.UsuarioCreador)
            .Include(o => o.Movimientos)
                .ThenInclude(m => m.Cantidades)
            .Include(o => o.Movimientos)
                .ThenInclude(m => m.Origen)
            .Include(o => o.Movimientos)
                .ThenInclude(m => m.Destino)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OperacionComercial>> GetAllWithMovimientosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.UsuarioCreador)
            .Include(o => o.Movimientos)
                .ThenInclude(m => m.Cantidades)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync(cancellationToken);
    }
}
