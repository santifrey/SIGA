using Microsoft.AspNetCore.Authorization;

namespace SIGA.API.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permiso { get; }
    public IReadOnlyList<string> Permisos { get; }

    public PermissionRequirement(string permiso)
    {
        if (string.IsNullOrWhiteSpace(permiso))
            throw new ArgumentException("El permiso no puede ser vacío.", nameof(permiso));

        Permiso = permiso;
        Permisos = new[] { permiso };
    }

    public PermissionRequirement(IEnumerable<string> permisos)
    {
        var list = permisos?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
        if (!list.Any())
            throw new ArgumentException("La lista de permisos no puede estar vacía.", nameof(permisos));

        Permisos = list.AsReadOnly();
        Permiso = string.Join(",", Permisos);
    }
}
