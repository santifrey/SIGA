using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class CantidadBulbos
{
    public int Id { get; set; }
    public int MovimientoBulbosId { get; set; }
    public virtual MovimientoBulbos MovimientoBulbos { get; set; } = null!;
    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }

    public CantidadBulbos()
    {
    }

    public CantidadBulbos(ClaseBulbo claseBulbo, int cantidad)
    {
        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad de bulbos debe ser mayor a cero.");

        ClaseBulbo = claseBulbo;
        Cantidad = cantidad;
    }
}
