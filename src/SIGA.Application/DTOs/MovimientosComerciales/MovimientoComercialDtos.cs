using System.ComponentModel.DataAnnotations;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.MovimientosComerciales;

public class ItemDetalleMovimientoComercialCreateDto
{
    [Required(ErrorMessage = "La clase de bulbo es requerida.")]
    public ClaseBulbo ClaseBulbo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo.")]
    public decimal? PrecioUnitarioPactado { get; set; }
}

public class CrearMovimientoComercialDto
{
    [Required(ErrorMessage = "El tipo de movimiento comercial es requerido.")]
    public TipoMovimientoComercial Tipo { get; set; } = TipoMovimientoComercial.VentaACliente;

    [Range(1, int.MaxValue, ErrorMessage = "El participante origen es requerido.")]
    public int OrigenParticipanteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El participante destino es requerido.")]
    public int DestinoParticipanteId { get; set; }

    public int? ClienteId { get; set; }

    public int? TemporadaId { get; set; }

    [Range(0, 100, ErrorMessage = "El porcentaje de comisión debe estar entre 0 y 100.")]
    public decimal PorcentajeComision { get; set; } = 0m;

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un ítem de detalle.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un ítem de detalle.")]
    public List<ItemDetalleMovimientoComercialCreateDto> Detalles { get; set; } = new();
}

public class ActualizarMovimientoComercialDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El participante origen es requerido.")]
    public int OrigenParticipanteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El participante destino es requerido.")]
    public int DestinoParticipanteId { get; set; }

    public int? ClienteId { get; set; }

    public int? TemporadaId { get; set; }

    [Range(0, 100, ErrorMessage = "El porcentaje de comisión debe estar entre 0 y 100.")]
    public decimal PorcentajeComision { get; set; } = 0m;

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un ítem de detalle.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un ítem de detalle.")]
    public List<ItemDetalleMovimientoComercialCreateDto> Detalles { get; set; } = new();
}

public class DetalleMovimientoComercialDto
{
    public int Id { get; set; }
    public int MovimientoComercialId { get; set; }
    public ClaseBulbo ClaseBulbo { get; set; }
    public string NombreClase => ClaseBulbo.ToString();
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioPactado { get; set; }
    public decimal Subtotal { get; set; }
}

public class ContratoMovimientoDto
{
    public int Id { get; set; }
    public int MovimientoComercialId { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
    public string? ArchivoUrl { get; set; }
    public EstadoContrato Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioGeneradorId { get; set; }
}

public class FacturaMovimientoDto
{
    public int Id { get; set; }
    public int MovimientoComercialId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal MontoTotal { get; set; }
    public string? ArchivoUrl { get; set; }
    public EstadoFactura Estado { get; set; }
    public string NombreEstado => Estado.ToString();
    public int UsuarioEmisionId { get; set; }
}

public class RegistroAprobacionMovimientoDto
{
    public int Id { get; set; }
    public int MovimientoComercialId { get; set; }
    public int UsuarioAprobadorId { get; set; }
    public string NombreUsuarioAprobador { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public AccionAprobacion Accion { get; set; }
    public string NombreAccion => Accion.ToString();
    public string? Observaciones { get; set; }
}

public class MovimientoComercialDto
{
    public int Id { get; set; }
    public TipoMovimientoComercial Tipo { get; set; }
    public string NombreTipo => Tipo.ToString();
    public EstadoMovimientoComercial Estado { get; set; }
    public string NombreEstado => Estado.ToString();

    public int OrigenParticipanteId { get; set; }
    public string NombreOrigen { get; set; } = string.Empty;

    public int DestinoParticipanteId { get; set; }
    public string NombreDestino { get; set; } = string.Empty;

    public int? ClienteId { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;

    public int? TemporadaId { get; set; }
    public string? NombreTemporada { get; set; }

    public DateTime Fecha { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int UsuarioRegistroId { get; set; }
    public string NombreUsuarioRegistro { get; set; } = string.Empty;

    // Financial breakdown
    public decimal MontoBruto { get; set; }
    public decimal PorcentajeComision { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoTotal { get; set; }
    public int TotalBulbos { get; set; }

    public string? Observaciones { get; set; }

    public bool RequiereContrato { get; set; }

    public List<DetalleMovimientoComercialDto> Detalles { get; set; } = new();
    public ContratoMovimientoDto? Contrato { get; set; }
    public FacturaMovimientoDto? Factura { get; set; }
    public List<RegistroAprobacionMovimientoDto> HistorialAprobaciones { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
}

public class AprobarAdminDto
{
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class GenerarContratoDto
{
    [Required(ErrorMessage = "El número de contrato es obligatorio.")]
    [StringLength(100)]
    public string NumeroContrato { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArchivoUrl { get; set; }
}

public class FacturarDto
{
    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [StringLength(100)]
    public string NumeroFactura { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArchivoUrl { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class FinalizarDto
{
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class RechazarDto
{
    [Required(ErrorMessage = "Debe indicar el motivo del rechazo u observación.")]
    [MinLength(1, ErrorMessage = "El motivo del rechazo no puede estar vacío.")]
    [StringLength(1000)]
    public string Motivo { get; set; } = string.Empty;
}

public class VerificarClienteNuevoDto
{
    public bool EsNuevo { get; set; }
    public bool RequiereContrato { get; set; }
}

public class FiltroMovimientosComercialesDto
{
    public TipoMovimientoComercial? Tipo { get; set; }
    public EstadoMovimientoComercial? Estado { get; set; }
    public int? ClienteId { get; set; }
    public int? TemporadaId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public class RechazarMovimientoDto : RechazarDto { }
public class VerificacionClienteNuevoDto : VerificarClienteNuevoDto { }
public class RegistroAprobacionDto : RegistroAprobacionMovimientoDto { }
