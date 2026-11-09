using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByIdWithPersonaAsync(int id, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByCodigoWithPersonaAsync(string codigo, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByPersonaIdAsync(int personaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cliente>> GetAllWithPersonaAsync(string? busqueda = null, CancellationToken cancellationToken = default);
}
