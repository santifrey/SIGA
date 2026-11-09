using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class PlantacionRepository : Repository<Plantacion>, IPlantacionRepository
{
    public PlantacionRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<Plantacion?> GetByTemporadaIdAsync(int temporadaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Temporada)
            .Include(p => p.UsuarioCreador)
            .Include(p => p.DetallesPlantacion)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Cliente)
                        .ThenInclude(c => c.Persona)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync(p => p.TemporadaId == temporadaId, cancellationToken);
    }

    public async Task<Plantacion?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Temporada)
            .Include(p => p.UsuarioCreador)
            .Include(p => p.DetallesPlantacion)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Cliente)
                        .ThenInclude(c => c.Persona)
            .Include(p => p.DetallesPlantacion)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l.Fila)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
