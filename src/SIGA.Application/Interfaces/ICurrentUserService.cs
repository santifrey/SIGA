using SIGA.Domain.Enums;

namespace SIGA.Application.Interfaces;

public interface ICurrentUserService
{
    int UsuarioId { get; }
    string Username { get; }
    bool EsAdministrador { get; }
    Task<int?> GetPersonaIdAsync(CancellationToken cancellationToken = default);
    Task<int?> GetSucursalIdAsync(CancellationToken cancellationToken = default);
    Task<AlcancePermiso> ObtenerAlcanceAsync(string permisoBase, CancellationToken cancellationToken = default);
    Task<bool> TienePermisoAsync(string permiso, CancellationToken cancellationToken = default);
}

