using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class DetallePlantacion : BaseEntity
{
    public int PlantacionId { get; set; }
    public virtual Plantacion Plantacion { get; set; } = null!;

    public int LoteId { get; set; }
    public virtual Lote Lote { get; set; } = null!;

    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }

    public DetallePlantacion()
    {
    }

    public DetallePlantacion(int plantacionId, int loteId, ClaseBulbo claseBulbo, int cantidad)
    {
        if (cantidad <= 0)
            throw new OperacionInvalidaException("La cantidad debe ser mayor a cero.");

        PlantacionId = plantacionId;
        LoteId = loteId;
        ClaseBulbo = claseBulbo;
        Cantidad = cantidad;
    }
}
