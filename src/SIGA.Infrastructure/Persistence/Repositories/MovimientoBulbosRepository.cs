using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class MovimientoBulbosRepository : Repository<MovimientoBulbos>, IMovimientoBulbosRepository
{
    public MovimientoBulbosRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<MovimientoBulbos?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => m.Destino)
            .Include(m => m.OperacionComercial)
            .Include(m => m.UsuarioCreador)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetAllWithDetailsAsync(EstadoMovimiento? estado = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => m.Destino)
            .Include(m => m.OperacionComercial)
            .Include(m => m.UsuarioCreador)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(m => m.Estado == estado.Value);
        }

        return await query.OrderByDescending(m => m.Fecha).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetByParticipanteAsync(int participanteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => m.Destino)
            .Include(m => m.OperacionComercial)
            .Where(m => m.OrigenId == participanteId || m.DestinoId == participanteId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetConfirmadosByParticipanteAsync(int participanteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => m.Destino)
            .Where(m => m.Estado == EstadoMovimiento.Confirmado && 
                       (m.OrigenId == participanteId || m.DestinoId == participanteId))
            .OrderBy(m => m.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetByOperacionIdAsync(int operacionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => m.Destino)
            .Where(m => m.OperacionComercialId == operacionId)
            .ToListAsync(cancellationToken);
    }
}
