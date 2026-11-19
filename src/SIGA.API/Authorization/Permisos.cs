namespace SIGA.API.Authorization;

public static class Permisos
{
    // Roles y Permisos
    public const string RolesVer = "ROLES_VER";
    public const string RolesGestionar = "ROLES_GESTIONAR";
    public const string PermisosVer = "PERMISOS_VER";

    // Empleados y Personal
    public const string EmpleadosVer = "EMPLEADOS_VER";
    public const string EmpleadosGestionar = "EMPLEADOS_GESTIONAR";

    // Usuarios del Sistema
    public const string UsuariosVer = "USUARIOS_VER";
    public const string UsuariosGestionar = "USUARIOS_GESTIONAR";

    // Clientes
    public const string ClientesVer = "CLIENTES_VER";
    public const string ClientesGestionar = "CLIENTES_GESTIONAR";

    // Sucursales
    public const string SucursalesVer = "SUCURSALES_VER";
    public const string SucursalesGestionar = "SUCURSALES_GESTIONAR";

    // Stock y Bulbos
    public const string StockVer = "STOCK_VER";
    public const string StockAjustar = "STOCK_AJUSTAR";

    // Temporadas
    public const string TemporadasVer = "TEMPORADAS_VER";
    public const string TemporadasGestionar = "TEMPORADAS_GESTIONAR";

    // Plantaciones y Filas
    public const string PlantacionesVer = "PLANTACIONES_VER";
    public const string PlantacionesGestionar = "PLANTACIONES_GESTIONAR";

    // Transferencias de Lotes
    public const string TransferenciasVer = "TRANSFERENCIAS_VER";
    public const string TransferenciasCrear = "TRANSFERENCIAS_CREAR";
    public const string TransferenciasGestionar = "TRANSFERENCIAS_GESTIONAR";

    // Cosechas
    public const string CosechasVer = "COSECHAS_VER";
    public const string CosechasGestionar = "COSECHAS_GESTIONAR";

    // Movimientos Comerciales / Ventas
    public const string MovComVer = "MOVCOM_VER";
    public const string MovComCrear = "MOVCOM_CREAR";
    public const string MovComAprobar = "MOVCOM_APROBAR";
    public const string MovComFacturar = "MOVCOM_FACTURAR";
    public const string MovComFinalizar = "MOVCOM_FINALIZAR";
    public const string MovComRechazar = "MOVCOM_RECHAZAR";
    public const string MovComCorregir = "MOVCOM_CORREGIR";

    // Precios de Referencia
    public const string PreciosRefVer = "PRECIOS_REF_VER";
    public const string PreciosRefGestionar = "PRECIOS_REF_GESTIONAR";

    // Reportes
    public const string ReportesVer = "REPORTES_VER";

    // Personas
    public const string PersonasVer = "PERSONAS_VER";
    public const string PersonasGestionar = "PERSONAS_GESTIONAR";

    // Mi Cuenta / Perfil
    public const string PerfilVer = "PERFIL_VER";
    public const string PerfilModificar = "PERFIL_MODIFICAR";
}
