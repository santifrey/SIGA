using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Ventas;

public class PrecioClaseDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string NombreTemporada { get; set; } = string.Empty;
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public decimal PrecioReferencia { get; set; }
    public DateTime FechaVigencia { get; set; }
}

public class UpsertPrecioClaseDto
{
    public int TemporadaId { get; set; }
    public ClaseBulbo ClaseBulbo { get; set; }
    public decimal PrecioReferencia { get; set; }
}

public class DetalleVentaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioPactado { get; set; }
    public decimal Subtotal { get; set; }
}

public class ContratoDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
    public string? ArchivoUrl { get; set; }
    public EstadoContrato Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioGeneradorId { get; set; }
}

public class FacturaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal MontoTotal { get; set; }
    public string? ArchivoUrl { get; set; }
    public EstadoFactura Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioEmisionId { get; set; }
}

public class RegistroAprobacionDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int UsuarioAprobadorId { get; set; }
    public string NombreUsuarioAprobador { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public AccionAprobacion Accion { get; set; }
    public string NombreAccion => Accion.ToString();
    public string? Observaciones { get; set; }
}

public class VentaDto
{
    public int Id { get; set; }
    public int? OperacionComercialId { get; set; }
    public int VendedorId { get; set; }
    public string NombreVendedor { get; set; } = string.Empty;
    public int OrigenParticipanteId { get; set; }
    public int ClienteId { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public int? TemporadaId { get; set; }
    public string? NombreTemporada { get; set; }
    public DateTime Fecha { get; set; }
    public decimal MontoTotal { get; set; }
    public int TotalBulbos { get; set; }
    public EstadoVenta Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public string? Observaciones { get; set; }
    public List<DetalleVentaDto> Detalles { get; set; } = new();
    public ContratoDto? Contrato { get; set; }
    public FacturaDto? Factura { get; set; }
    public List<RegistroAprobacionDto> HistorialAprobaciones { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}

public class ItemDetalleVentaCreateDto
{
    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioUnitarioPactado { get; set; }
}

public class CreateVentaDto
{
    public int ClienteId { get; set; }
    public int? TemporadaId { get; set; }
    public int OrigenParticipanteId { get; set; } = 1;
    public string? Observaciones { get; set; }
    public List<ItemDetalleVentaCreateDto> Detalles { get; set; } = new();
}

public class GenerarContratoDto
{
    public string NumeroContrato { get; set; } = string.Empty;
    public string? ArchivoUrl { get; set; }
}

public class EmitirFacturaDto
{
    public string NumeroFactura { get; set; } = string.Empty;
    public string? ArchivoUrl { get; set; }
    public string? Observaciones { get; set; }
}

public class AccionCircuitoVentaDto
{
    public string? Observaciones { get; set; }
}
