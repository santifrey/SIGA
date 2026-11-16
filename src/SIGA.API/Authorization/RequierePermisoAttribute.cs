using Microsoft.AspNetCore.Authorization;

namespace SIGA.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequierePermisoAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERMISO_";

    public RequierePermisoAttribute(string permiso)
    {
        if (string.IsNullOrWhiteSpace(permiso))
            throw new ArgumentException("El código de permiso no puede ser nulo o vacío.", nameof(permiso));

        Permisos = new[] { permiso };
        Policy = $"{PolicyPrefix}{permiso}";
    }

    public RequierePermisoAttribute(params string[] permisos)
    {
        if (permisos == null || permisos.Length == 0)
            throw new ArgumentException("Debe especificar al menos un permiso.", nameof(permisos));

        Permisos = permisos;
        Policy = $"{PolicyPrefix}{string.Join(",", permisos)}";
    }

    public string[] Permisos { get; }
}
