using System.ComponentModel.DataAnnotations;
using SIGA.Domain.Entities;
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
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de temporada es requerido.")]
    public int TemporadaId { get; set; }

    [Required]
    public ClaseBulbo ClaseBulbo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio de referencia debe ser mayor a cero.")]
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
    [Required]
    public ClaseBulbo ClaseBulbo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }

    public decimal? PrecioUnitarioPactado { get; set; }
}

public class CreateVentaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El cliente comprador es requerido.")]
    public int ClienteId { get; set; }

    public int? TemporadaId { get; set; }

    public int OrigenParticipanteId { get; set; } = Empresa.SingletonId;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un ítem de detalle en la venta.")]
    public List<ItemDetalleVentaCreateDto> Detalles { get; set; } = new();
}

public class GenerarContratoDto
{
    [Required(ErrorMessage = "El número de contrato es obligatorio.")]
    [StringLength(100)]
    public string NumeroContrato { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArchivoUrl { get; set; }
}

public class EmitirFacturaDto
{
    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [StringLength(100)]
    public string NumeroFactura { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArchivoUrl { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class AccionCircuitoVentaDto
{
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}
