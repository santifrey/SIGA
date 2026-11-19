using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Interfaces;

public interface IMovimientoComercialRepository : IRepository<MovimientoComercial>
{
    Task<IReadOnlyList<MovimientoComercial>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoComercial>> GetByFiltersAsync(
        TipoMovimientoComercial? tipo = null,
        EstadoMovimientoComercial? estado = null,
        int? clienteId = null,
        int? temporadaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioRegistroId = null,
        int? sucursalId = null,
        CancellationToken cancellationToken = default);
    Task<MovimientoComercial?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasFinalizedMovementsForClientAsync(int clienteId, int? excludeId = null, CancellationToken cancellationToken = default);
}
