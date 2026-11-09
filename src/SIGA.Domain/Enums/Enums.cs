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

public enum ClaseBulbo
{
    Clase0 = 0,
    Clase1 = 1,
    Clase2 = 2,
    Clase3 = 3,
    Clase4 = 4
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

public enum EstadoVenta
{
    Registrada = 0,
    EnRevisionAdministracion = 1,
    AprobadaAdministracion = 2,
    Facturada = 3,
    Completada = 4,
    Rechazada = 5,
    Anulada = 6
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
    EnviadoARevision = 0,
    AprobadoAdministracion = 1,
    Observado = 2,
    Facturado = 3,
    AprobacionFinal = 4,
    Rechazado = 5
}



