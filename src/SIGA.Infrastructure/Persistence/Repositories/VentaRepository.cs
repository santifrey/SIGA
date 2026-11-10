using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class VentaRepository : Repository<Venta>, IVentaRepository
{
    public VentaRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Venta>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.Cliente)
                .ThenInclude(c => c.Persona)
            .Include(v => v.Vendedor)
                .ThenInclude(u => u!.Persona)
            .Include(v => v.Temporada)
            .Include(v => v.Detalles)
            .Include(v => v.Contrato)
            .Include(v => v.Factura)
            .Include(v => v.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Venta>> GetByClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.Cliente)
                .ThenInclude(c => c.Persona)
            .Include(v => v.Vendedor)
                .ThenInclude(u => u!.Persona)
            .Include(v => v.Temporada)
            .Include(v => v.Detalles)
            .Include(v => v.Contrato)
            .Include(v => v.Factura)
            .Include(v => v.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
            .Where(v => v.ClienteId == clienteId)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Venta?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Cliente)
                .ThenInclude(c => c.Persona)
            .Include(v => v.Vendedor)
                .ThenInclude(u => u!.Persona)
            .Include(v => v.Temporada)
            .Include(v => v.Detalles)
            .Include(v => v.Contrato)
            .Include(v => v.Factura)
            .Include(v => v.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }
}
