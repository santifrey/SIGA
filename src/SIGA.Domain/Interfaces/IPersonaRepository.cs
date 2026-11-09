using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IPersonaRepository : IRepository<Persona>
{
    Task<Persona?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default);
    Task<Persona?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
