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
            .Include(m => (m.Origen as Cliente)!.Persona)
            .Include(m => m.Destino)
            .Include(m => (m.Destino as Cliente)!.Persona)
            .Include(m => m.MovimientoComercial)
            .Include(m => m.UsuarioCreador)
                .ThenInclude(u => u!.Persona)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetAllWithDetailsAsync(EstadoMovimiento? estado = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => (m.Origen as Cliente)!.Persona)
            .Include(m => m.Destino)
            .Include(m => (m.Destino as Cliente)!.Persona)
            .Include(m => m.MovimientoComercial)
            .Include(m => m.UsuarioCreador)
                .ThenInclude(u => u!.Persona)
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
            .AsNoTracking()
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => (m.Origen as Cliente)!.Persona)
            .Include(m => m.Destino)
            .Include(m => (m.Destino as Cliente)!.Persona)
            .Where(m => m.OrigenId == participanteId || m.DestinoId == participanteId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetConfirmadosByParticipanteAsync(int participanteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => (m.Origen as Cliente)!.Persona)
            .Include(m => m.Destino)
            .Include(m => (m.Destino as Cliente)!.Persona)
            .Where(m => m.Estado == EstadoMovimiento.Confirmado && 
                       (m.OrigenId == participanteId || m.DestinoId == participanteId))
            .OrderBy(m => m.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoBulbos>> GetByMovimientoComercialIdAsync(int movimientoComercialId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(m => m.Cantidades)
            .Include(m => m.Origen)
            .Include(m => (m.Origen as Cliente)!.Persona)
            .Include(m => m.Destino)
            .Include(m => (m.Destino as Cliente)!.Persona)
            .Where(m => m.MovimientoComercialId == movimientoComercialId)
            .ToListAsync(cancellationToken);
    }
}
