using SIGA.Domain.Enums;

namespace SIGA.Domain.Common;

public static class PermisoHierarchy
{
    public const string SufijoPropio = "_PROPIO";
    public const string SufijoSucursal = "_SUCURSAL";
    public const string SufijoTodos = "_TODOS";

    public static AlcancePermiso ObtenerAlcance(IEnumerable<string> userPermissions, string permisoBase)
    {
        if (userPermissions == null) return AlcancePermiso.Ninguno;

        var set = userPermissions is HashSet<string> hs
            ? hs
            : new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);

        // 1. Nivel Todos (Global): explícito _TODOS o permiso base histórico
        if (set.Contains($"{permisoBase}{SufijoTodos}") || set.Contains(permisoBase))
            return AlcancePermiso.Todos;

        // 2. Nivel Sucursal
        if (set.Contains($"{permisoBase}{SufijoSucursal}"))
            return AlcancePermiso.Sucursal;

        // 3. Nivel Propio
        if (set.Contains($"{permisoBase}{SufijoPropio}"))
            return AlcancePermiso.Propio;

        return AlcancePermiso.Ninguno;
    }

    public static bool SatisfaceRequerimiento(IEnumerable<string> userPermissions, string permisoRequerido)
    {
        if (userPermissions == null) return false;

        var set = userPermissions is HashSet<string> hs
            ? hs
            : new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);

        if (set.Contains(permisoRequerido)) return true;

        if (permisoRequerido.EndsWith(SufijoPropio, StringComparison.OrdinalIgnoreCase))
        {
            var baseCode = permisoRequerido[..^SufijoPropio.Length];
            return set.Contains(baseCode) ||
                   set.Contains($"{baseCode}{SufijoTodos}") ||
                   set.Contains($"{baseCode}{SufijoSucursal}") ||
                   set.Contains($"{baseCode}{SufijoPropio}");
        }

        if (permisoRequerido.EndsWith(SufijoSucursal, StringComparison.OrdinalIgnoreCase))
        {
            var baseCode = permisoRequerido[..^SufijoSucursal.Length];
            return set.Contains(baseCode) ||
                   set.Contains($"{baseCode}{SufijoTodos}") ||
                   set.Contains($"{baseCode}{SufijoSucursal}");
        }

        if (permisoRequerido.EndsWith(SufijoTodos, StringComparison.OrdinalIgnoreCase))
        {
            var baseCode = permisoRequerido[..^SufijoTodos.Length];
            return set.Contains(baseCode) || set.Contains($"{baseCode}{SufijoTodos}");
        }

        // Si el requerimiento es un permiso base (sin sufijo), cualquier nivel lo satisface
        return set.Contains($"{permisoRequerido}{SufijoTodos}") ||
               set.Contains($"{permisoRequerido}{SufijoSucursal}") ||
               set.Contains($"{permisoRequerido}{SufijoPropio}");
    }
}

