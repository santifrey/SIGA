namespace SIGA.API.Authorization;

public static class Roles
{
    public const string Administrador = "ADMINISTRADOR";
    public const string Vendedor = "VENDEDOR";
    public const string Secretaria = "SECRETARIA";
    public const string Cliente = "CLIENTE";
    public const string Contabilidad = "CONTABILIDAD";

    [Obsolete("Utilice [RequierePermiso(...)] en su lugar.")]
    public const string Personal = Administrador + "," + Vendedor + "," + Secretaria + "," + Contabilidad;

    [Obsolete("Utilice [RequierePermiso(...)] en su lugar.")]
    public const string Comercial = Administrador + "," + Vendedor;

    [Obsolete("Utilice [RequierePermiso(...)] en su lugar.")]
    public const string Gestion = Administrador + "," + Secretaria;

    [Obsolete("Utilice [RequierePermiso(...)] en su lugar.")]
    public const string Contable = Administrador + "," + Contabilidad;

    [Obsolete("Utilice [RequierePermiso(...)] en su lugar.")]
    public const string Revisores = Administrador + "," + Secretaria + "," + Contabilidad;
}
