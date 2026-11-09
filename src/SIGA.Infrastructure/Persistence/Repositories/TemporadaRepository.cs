using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class TemporadaRepository : Repository<Temporada>, ITemporadaRepository
{
    public TemporadaRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Temporada?> GetActivaAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.UsuarioCreador)
            .Include(t => t.Filas)
            .Include(t => t.Plantaciones)
            .OrderByDescending(t => t.FechaInicio)
            .FirstOrDefaultAsync(t => t.Estado != EstadoTemporada.Cerrada, cancellationToken);
    }

    public async Task<Temporada?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.UsuarioCreador)
            .Include(t => t.Filas)
                .ThenInclude(f => f.Lotes)
            .Include(t => t.Plantaciones)
                .ThenInclude(p => p.DetallesPlantacion)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Temporada>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.UsuarioCreador)
            .Include(t => t.Filas)
            .Include(t => t.Plantaciones)
            .OrderByDescending(t => t.FechaInicio)
            .ToListAsync(cancellationToken);
    }
}
