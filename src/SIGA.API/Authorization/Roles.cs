namespace SIGA.API.Authorization;

/// <summary>
/// Nombres de roles del sistema (deben coincidir con los sembrados en SigaDbContext)
/// y agrupaciones utilizadas en los atributos [Authorize(Roles = ...)].
/// </summary>
/// <remarks>
/// En ASP.NET Core, cuando un controlador y una acción declaran ambos [Authorize(Roles = ...)],
/// el usuario debe cumplir TODOS los atributos (AND). Por eso los controladores de negocio
/// declaran <see cref="Personal"/> a nivel de clase y las acciones sensibles restringen aún más.
/// </remarks>
public static class Roles
{
    public const string Administrador = "ADMINISTRADOR";
    public const string Vendedor = "VENDEDOR";
    public const string Secretaria = "SECRETARIA";
    public const string Cliente = "CLIENTE";
    public const string Contabilidad = "CONTABILIDAD";

    /// <summary>Personal interno de la empresa (excluye usuarios con rol CLIENTE).</summary>
    public const string Personal = Administrador + "," + Vendedor + "," + Secretaria + "," + Contabilidad;

    /// <summary>Circuito comercial: alta de ventas y envío a revisión.</summary>
    public const string Comercial = Administrador + "," + Vendedor;

    /// <summary>Gestión administrativa/operativa: contratos, confirmación y anulación de operaciones.</summary>
    public const string Gestion = Administrador + "," + Secretaria;

    /// <summary>Gestión contable: facturación y precios de referencia.</summary>
    public const string Contable = Administrador + "," + Contabilidad;

    /// <summary>Roles que pueden observar/rechazar una venta en revisión.</summary>
    public const string Revisores = Administrador + "," + Secretaria + "," + Contabilidad;
}
