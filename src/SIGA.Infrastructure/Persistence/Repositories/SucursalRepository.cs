using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class SucursalRepository : Repository<Sucursal>, ISucursalRepository
{
    public SucursalRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Sucursal?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Nombre.ToLower() == nombre.ToLower(), cancellationToken);
    }

    public async Task<Sucursal?> GetCasaMatrizAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.EsCasaMatriz && !s.IsDeleted, cancellationToken);
    }

    public async Task<Sucursal?> GetByIdWithParticipanteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sucursal?> GetByParticipanteComercialIdAsync(int participanteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Id == participanteId, cancellationToken);
    }

    public async Task<IReadOnlyList<Sucursal>> GetAllWithParticipanteAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
}
