using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class PrecioClase : BaseEntity
{
    public int TemporadaId { get; set; }
    public virtual Temporada Temporada { get; set; } = null!;

    public ClaseBulbo ClaseBulbo { get; set; }
    public decimal PrecioReferencia { get; set; }
    public DateTime FechaVigencia { get; set; } = DateTime.UtcNow;

    public PrecioClase()
    {
    }

    public PrecioClase(int temporadaId, ClaseBulbo claseBulbo, decimal precioReferencia)
    {
        if (temporadaId <= 0)
            throw new OperacionInvalidaException("La temporada es requerida para definir el precio de referencia.");
        if (precioReferencia <= 0)
            throw new OperacionInvalidaException("El precio de referencia por clase debe ser mayor a cero.");

        TemporadaId = temporadaId;
        ClaseBulbo = claseBulbo;
        PrecioReferencia = precioReferencia;
        FechaVigencia = DateTime.UtcNow;
    }

    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            throw new OperacionInvalidaException("El nuevo precio de referencia debe ser mayor a cero.");

        PrecioReferencia = nuevoPrecio;
        FechaVigencia = DateTime.UtcNow;
    }
}
