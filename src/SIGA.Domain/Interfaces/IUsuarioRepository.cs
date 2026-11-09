using SIGA.Domain.Entities;

namespace SIGA.Domain.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByUsernameWithRolesAsync(string username, CancellationToken cancellationToken = default);
    Task<Usuario?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> GetAllWithRolesAndPersonaAsync(CancellationToken cancellationToken = default);
}
