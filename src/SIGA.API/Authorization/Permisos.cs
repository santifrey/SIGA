namespace SIGA.API.Authorization;

public static class Permisos
{
    // Roles y Permisos
    public const string RolesVer = "ROLES_VER";
    public const string RolesGestionar = "ROLES_GESTIONAR";
    public const string PermisosVer = "PERMISOS_VER";

    // Empleados y Personal
    public const string EmpleadosVer = "EMPLEADOS_VER";
    public const string EmpleadosVerSucursal = "EMPLEADOS_VER_SUCURSAL";
    public const string EmpleadosVerTodos = "EMPLEADOS_VER_TODOS";
    public const string EmpleadosGestionar = "EMPLEADOS_GESTIONAR";
    public const string EmpleadosGestionarSucursal = "EMPLEADOS_GESTIONAR_SUCURSAL";
    public const string EmpleadosGestionarTodos = "EMPLEADOS_GESTIONAR_TODOS";

    // Usuarios del Sistema
    public const string UsuariosVer = "USUARIOS_VER";
    public const string UsuariosGestionar = "USUARIOS_GESTIONAR";

    // Clientes
    public const string ClientesVer = "CLIENTES_VER";
    public const string ClientesVerPropio = "CLIENTES_VER_PROPIO";
    public const string ClientesVerSucursal = "CLIENTES_VER_SUCURSAL";
    public const string ClientesVerTodos = "CLIENTES_VER_TODOS";
    public const string ClientesGestionar = "CLIENTES_GESTIONAR";
    public const string ClientesGestionarPropio = "CLIENTES_GESTIONAR_PROPIO";
    public const string ClientesGestionarSucursal = "CLIENTES_GESTIONAR_SUCURSAL";
    public const string ClientesGestionarTodos = "CLIENTES_GESTIONAR_TODOS";

    // Sucursales
    public const string SucursalesVer = "SUCURSALES_VER";
    public const string SucursalesVerSucursal = "SUCURSALES_VER_SUCURSAL";
    public const string SucursalesVerTodos = "SUCURSALES_VER_TODOS";
    public const string SucursalesGestionar = "SUCURSALES_GESTIONAR";

    // Stock y Bulbos
    public const string StockVer = "STOCK_VER";
    public const string StockVerPropio = "STOCK_VER_PROPIO";
    public const string StockVerSucursal = "STOCK_VER_SUCURSAL";
    public const string StockVerTodos = "STOCK_VER_TODOS";
    public const string StockAjustar = "STOCK_AJUSTAR";
    public const string StockAjustarSucursal = "STOCK_AJUSTAR_SUCURSAL";
    public const string StockAjustarTodos = "STOCK_AJUSTAR_TODOS";

    // Temporadas
    public const string TemporadasVer = "TEMPORADAS_VER";
    public const string TemporadasGestionar = "TEMPORADAS_GESTIONAR";

    // Plantaciones y Filas
    public const string PlantacionesVer = "PLANTACIONES_VER";
    public const string PlantacionesVerPropio = "PLANTACIONES_VER_PROPIO";
    public const string PlantacionesVerSucursal = "PLANTACIONES_VER_SUCURSAL";
    public const string PlantacionesVerTodos = "PLANTACIONES_VER_TODOS";
    public const string PlantacionesGestionar = "PLANTACIONES_GESTIONAR";
    public const string PlantacionesGestionarSucursal = "PLANTACIONES_GESTIONAR_SUCURSAL";
    public const string PlantacionesGestionarTodos = "PLANTACIONES_GESTIONAR_TODOS";

    // Transferencias de Lotes
    public const string TransferenciasVer = "TRANSFERENCIAS_VER";
    public const string TransferenciasVerPropio = "TRANSFERENCIAS_VER_PROPIO";
    public const string TransferenciasVerSucursal = "TRANSFERENCIAS_VER_SUCURSAL";
    public const string TransferenciasVerTodos = "TRANSFERENCIAS_VER_TODOS";
    public const string TransferenciasCrear = "TRANSFERENCIAS_CREAR";
    public const string TransferenciasGestionar = "TRANSFERENCIAS_GESTIONAR";
    public const string TransferenciasGestionarSucursal = "TRANSFERENCIAS_GESTIONAR_SUCURSAL";
    public const string TransferenciasGestionarTodos = "TRANSFERENCIAS_GESTIONAR_TODOS";

    // Cosechas
    public const string CosechasVer = "COSECHAS_VER";
    public const string CosechasVerPropio = "COSECHAS_VER_PROPIO";
    public const string CosechasVerSucursal = "COSECHAS_VER_SUCURSAL";
    public const string CosechasVerTodos = "COSECHAS_VER_TODOS";
    public const string CosechasGestionar = "COSECHAS_GESTIONAR";
    public const string CosechasGestionarPropio = "COSECHAS_GESTIONAR_PROPIO";
    public const string CosechasGestionarSucursal = "COSECHAS_GESTIONAR_SUCURSAL";
    public const string CosechasGestionarTodos = "COSECHAS_GESTIONAR_TODOS";

    // Movimientos Comerciales / Ventas
    public const string MovComVer = "MOVCOM_VER";
    public const string MovComVerPropio = "MOVCOM_VER_PROPIO";
    public const string MovComVerSucursal = "MOVCOM_VER_SUCURSAL";
    public const string MovComVerTodos = "MOVCOM_VER_TODOS";
    public const string MovComCrear = "MOVCOM_CREAR";
    public const string MovComCrearPropio = "MOVCOM_CREAR_PROPIO";
    public const string MovComCrearSucursal = "MOVCOM_CREAR_SUCURSAL";
    public const string MovComCrearTodos = "MOVCOM_CREAR_TODOS";
    public const string MovComGestionar = "MOVCOM_GESTIONAR";
    public const string MovComGestionarPropio = "MOVCOM_GESTIONAR_PROPIO";
    public const string MovComGestionarSucursal = "MOVCOM_GESTIONAR_SUCURSAL";
    public const string MovComGestionarTodos = "MOVCOM_GESTIONAR_TODOS";
    public const string MovComAprobar = "MOVCOM_APROBAR";
    public const string MovComAprobarSucursal = "MOVCOM_APROBAR_SUCURSAL";
    public const string MovComAprobarTodos = "MOVCOM_APROBAR_TODOS";
    public const string MovComFacturar = "MOVCOM_FACTURAR";
    public const string MovComFacturarSucursal = "MOVCOM_FACTURAR_SUCURSAL";
    public const string MovComFacturarTodos = "MOVCOM_FACTURAR_TODOS";
    public const string MovComFinalizar = "MOVCOM_FINALIZAR";
    public const string MovComFinalizarSucursal = "MOVCOM_FINALIZAR_SUCURSAL";
    public const string MovComFinalizarTodos = "MOVCOM_FINALIZAR_TODOS";
    public const string MovComRechazar = "MOVCOM_RECHAZAR";
    public const string MovComRechazarSucursal = "MOVCOM_RECHAZAR_SUCURSAL";
    public const string MovComRechazarTodos = "MOVCOM_RECHAZAR_TODOS";
    public const string MovComCorregir = "MOVCOM_CORREGIR";

    // Precios de Referencia
    public const string PreciosRefVer = "PRECIOS_REF_VER";
    public const string PreciosRefGestionar = "PRECIOS_REF_GESTIONAR";

    // Reportes
    public const string ReportesVer = "REPORTES_VER";
    public const string ReportesVerPropio = "REPORTES_VER_PROPIO";
    public const string ReportesVerSucursal = "REPORTES_VER_SUCURSAL";
    public const string ReportesVerTodos = "REPORTES_VER_TODOS";

    // Personas
    public const string PersonasVer = "PERSONAS_VER";
    public const string PersonasGestionar = "PERSONAS_GESTIONAR";

    // Mi Cuenta / Perfil
    public const string PerfilVer = "PERFIL_VER";
    public const string PerfilModificar = "PERFIL_MODIFICAR";
}
