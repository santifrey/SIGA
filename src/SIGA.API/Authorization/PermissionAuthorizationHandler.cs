using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SIGA.API.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Si el usuario no está autenticado, no otorgar acceso
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // 2. Bypass de superusuario: Rol ADMINISTRADOR satisface automáticamente cualquier requerimiento
        if (context.User.IsInRole("ADMINISTRADOR") ||
            context.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                                       c.Value.Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 3. Verificación de claims "permiso" / "permission"
        var userPermissions = context.User.Claims
            .Where(c => c.Type.Equals("permiso", StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Si el usuario posee cualquiera de los permisos especificados en el requerimiento
        if (requirement.Permisos.Any(p => userPermissions.Contains(p)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
