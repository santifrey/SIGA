using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Factura : BaseEntity
{
    public int VentaId { get; set; }
    public virtual Venta Venta { get; set; } = null!;

    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public decimal MontoTotal { get; set; }
    public string? ArchivoUrl { get; set; }
    public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;

    public int UsuarioEmisionId { get; set; }
    public virtual Usuario? UsuarioEmision { get; set; }

    public Factura()
    {
    }

    public Factura(int ventaId, string numeroFactura, decimal montoTotal, int usuarioEmisionId, string? archivoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(numeroFactura))
            throw new OperacionInvalidaException("El número de factura es obligatorio.");
        if (montoTotal <= 0)
            throw new OperacionInvalidaException("El monto total de la factura debe ser mayor a cero.");

        VentaId = ventaId;
        NumeroFactura = numeroFactura.Trim();
        MontoTotal = montoTotal;
        UsuarioEmisionId = usuarioEmisionId;
        ArchivoUrl = archivoUrl;
        FechaEmision = DateTime.UtcNow;
        Estado = EstadoFactura.Emitida;
    }

    public void MarcarPagada()
    {
        if (Estado == EstadoFactura.Anulada)
            throw new OperacionInvalidaException("No se puede marcar como pagada una factura anulada.");

        Estado = EstadoFactura.Pagada;
    }
}
