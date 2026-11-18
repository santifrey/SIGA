namespace SIGA.Domain.Enums;

public enum EstadoUsuario
{
    Inactivo = 0,
    Activo = 1,
    Bloqueado = 2
}

public enum EstadoCliente
{
    Inactivo = 0,
    Activo = 1,
    Suspendido = 2
}

public enum EstadoParticipante
{
    Inactivo = 0,
    Activo = 1
}

public enum EstadoSucursal
{
    Inactivo = 0,
    Activo = 1,
    Mantenimiento = 2
}

public enum TipoParticipante
{
    Cliente = 1,
    Empresa = 2,
    Proveedor = 3
}

public enum TipoMovimientoComercial
{
    VentaACliente = 1,
    CompraACliente = 2,
    TraspasoEntreClientes = 3
}

public enum EstadoMovimientoComercial
{
    PendienteRevision = 0,
    AprobadoAdmin = 1,
    Facturado = 2,
    Finalizado = 3,
    Rechazado = 4
}

public enum ClaseBulbo
{
    Clase0 = 0,
    Clase1 = 1,
    Clase2 = 2,
    Clase3 = 3,
    Clase4 = 4,
    MIX = 5
}

public enum EstadoOperacion
{
    Pendiente = 0,
    Confirmada = 1,
    Anulada = 2
}

public enum EstadoMovimiento
{
    Pendiente = 0,
    Confirmado = 1,
    Anulado = 2
}

public enum TipoOperacionComercial
{
    CompraVentaBulbos = 1,
    Traspaso = 2,
    AjusteStock = 3,
    Devolucion = 4,
    IngresoCosecha = 5
}

public enum EstadoTemporada
{
    Planificada = 0,
    Plantacion = 1,
    EnCrecimiento = 2,
    Cosecha = 3,
    Comercializacion = 4,
    Cerrada = 5
}

public enum EstadoFila
{
    Disponible = 0,
    Plantada = 1,
    Cosechada = 2,
    Cerrada = 3
}

public enum EstadoLote
{
    Asignado = 0,
    Plantado = 1,
    EnCrecimiento = 2,
    Cosechado = 3,
    Cerrado = 4
}

public enum EstadoPlantacion
{
    Planificada = 0,
    EnProceso = 1,
    Finalizada = 2
}

public enum EstadoTransferencia
{
    Pendiente = 0,
    Confirmada = 1,
    Anulada = 2
}

public enum EstadoCosecha
{
    EnProceso = 0,
    Finalizada = 1,
    Cerrada = 2
}


public enum EstadoContrato
{
    Generado = 0,
    Firmado = 1,
    Anulado = 2
}

public enum EstadoFactura
{
    Emitida = 0,
    Pagada = 1,
    Anulada = 2
}

public enum AccionAprobacion
{
    Registrado = 0,
    EnviadoARevision = 0,
    AprobadoAdmin = 1,
    AprobadoAdministracion = 1,
    Observado = 2,
    Facturado = 3,
    Finalizado = 4,
    AprobacionFinal = 4,
    Rechazado = 5,
    Corregido = 6
}



