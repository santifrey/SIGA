using Microsoft.EntityFrameworkCore;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Infrastructure.Persistence.Repositories;

public class MovimientoComercialRepository : Repository<MovimientoComercial>, IMovimientoComercialRepository
{
    public MovimientoComercialRepository(SigaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MovimientoComercial>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(m => m.OrigenParticipante)
            .Include(m => m.DestinoParticipante)
            .Include(m => m.Cliente)
                .ThenInclude(c => c!.Persona)
            .Include(m => m.Temporada)
            .Include(m => m.UsuarioRegistro)
                .ThenInclude(u => u!.Persona)
            .Include(m => m.Detalles)
            .Include(m => m.Contrato)
            .Include(m => m.Factura)
            .Include(m => m.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
                    .ThenInclude(u => u!.Persona)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoComercial>> GetByFiltersAsync(
        TipoMovimientoComercial? tipo = null,
        EstadoMovimientoComercial? estado = null,
        int? clienteId = null,
        int? temporadaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioRegistroId = null,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.OrigenParticipante)
            .Include(m => m.DestinoParticipante)
            .Include(m => m.Cliente)
                .ThenInclude(c => c!.Persona)
            .Include(m => m.Temporada)
            .Include(m => m.UsuarioRegistro)
                .ThenInclude(u => u!.Persona)
            .Include(m => m.Detalles)
            .Include(m => m.Contrato)
            .Include(m => m.Factura)
            .Include(m => m.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
                    .ThenInclude(u => u!.Persona)
            .AsQueryable();

        if (tipo.HasValue)
            query = query.Where(m => m.Tipo == tipo.Value);

        if (estado.HasValue)
            query = query.Where(m => m.Estado == estado.Value);

        if (clienteId.HasValue)
            query = query.Where(m => m.ClienteId == clienteId.Value || m.DestinoParticipanteId == clienteId.Value || m.OrigenParticipanteId == clienteId.Value);

        if (temporadaId.HasValue)
            query = query.Where(m => m.TemporadaId == temporadaId.Value);

        if (fechaDesde.HasValue)
            query = query.Where(m => m.Fecha >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(m => m.Fecha <= fechaHasta.Value);

        if (usuarioRegistroId.HasValue)
            query = query.Where(m => m.UsuarioRegistroId == usuarioRegistroId.Value);

        if (sucursalId.HasValue)
            query = query.Where(m => m.OrigenParticipanteId == sucursalId.Value || m.DestinoParticipanteId == sucursalId.Value);

        return await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<MovimientoComercial?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.OrigenParticipante)
            .Include(m => m.DestinoParticipante)
            .Include(m => m.Cliente)
                .ThenInclude(c => c!.Persona)
            .Include(m => m.Temporada)
            .Include(m => m.UsuarioRegistro)
                .ThenInclude(u => u!.Persona)
            .Include(m => m.Detalles)
            .Include(m => m.Contrato)
            .Include(m => m.Factura)
            .Include(m => m.HistorialAprobaciones)
                .ThenInclude(r => r.UsuarioAprobador)
                    .ThenInclude(u => u!.Persona)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<bool> HasFinalizedMovementsForClientAsync(int clienteId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(m =>
            (!excludeId.HasValue || m.Id != excludeId.Value) &&
            m.Estado == EstadoMovimientoComercial.Finalizado &&
            (m.DestinoParticipanteId == clienteId || m.OrigenParticipanteId == clienteId || m.ClienteId == clienteId),
            cancellationToken);
    }
}
