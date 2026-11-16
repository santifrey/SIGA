using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class DetalleMovimientoComercial : BaseEntity
{
    public int MovimientoComercialId { get; set; }
    public virtual MovimientoComercial MovimientoComercial { get; set; } = null!;

    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioPactado { get; set; }
    public decimal Subtotal { get; set; }

    public DetalleMovimientoComercial()
    {
    }

    public DetalleMovimientoComercial(int movimientoComercialId, ClaseBulbo claseBulbo, int cantidad, decimal precioUnitarioPactado)
    {
        Validar(claseBulbo, cantidad, precioUnitarioPactado);

        MovimientoComercialId = movimientoComercialId;
        ClaseBulbo = claseBulbo;
        Cantidad = cantidad;
        PrecioUnitarioPactado = precioUnitarioPactado;
        Subtotal = Math.Round(cantidad * precioUnitarioPactado, 2);
    }

    public void Actualizar(int cantidad, decimal precioUnitarioPactado)
    {
        Validar(ClaseBulbo, cantidad, precioUnitarioPactado);

        Cantidad = cantidad;
        PrecioUnitarioPactado = precioUnitarioPactado;
        Subtotal = Math.Round(cantidad * precioUnitarioPactado, 2);
    }

    private static void Validar(ClaseBulbo claseBulbo, int cantidad, decimal precioUnitarioPactado)
    {
        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad debe ser mayor a cero.");

        if (claseBulbo == ClaseBulbo.MIX && cantidad % 3 != 0)
            throw new OperacionInvalidaException($"La cantidad para ítems MIX debe ser múltiplo de 3 (recibido: {cantidad}).");

        if (precioUnitarioPactado < 0)
            throw new OperacionInvalidaException("El precio unitario no puede ser negativo.");
    }
}
