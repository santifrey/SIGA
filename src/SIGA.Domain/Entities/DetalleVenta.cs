using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class DetalleVenta : BaseEntity
{
    public int VentaId { get; set; }
    public virtual Venta Venta { get; set; } = null!;

    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioPactado { get; set; }
    public decimal Subtotal { get; set; }

    public DetalleVenta()
    {
    }

    public DetalleVenta(int ventaId, ClaseBulbo claseBulbo, int cantidad, decimal precioUnitarioPactado)
    {
        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad del detalle de venta debe ser mayor a cero.");
        if (precioUnitarioPactado <= 0)
            throw new OperacionInvalidaException("El precio unitario pactado debe ser mayor a cero.");

        VentaId = ventaId;
        ClaseBulbo = claseBulbo;
        Cantidad = cantidad;
        PrecioUnitarioPactado = precioUnitarioPactado;
        Subtotal = Math.Round(cantidad * precioUnitarioPactado, 2);
    }

    public void Actualizar(int cantidad, decimal precioUnitarioPactado)
    {
        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad del detalle de venta debe ser mayor a cero.");
        if (precioUnitarioPactado <= 0)
            throw new OperacionInvalidaException("El precio unitario pactado debe ser mayor a cero.");

        Cantidad = cantidad;
        PrecioUnitarioPactado = precioUnitarioPactado;
        Subtotal = Math.Round(cantidad * precioUnitarioPactado, 2);
    }
}
