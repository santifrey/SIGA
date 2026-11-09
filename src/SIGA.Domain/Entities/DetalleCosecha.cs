using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class DetalleCosecha : BaseEntity
{
    public int CosechaId { get; set; }
    public virtual Cosecha Cosecha { get; set; } = null!;

    public int LoteId { get; set; }
    public virtual Lote Lote { get; set; } = null!;

    public ClaseBulbo ClaseBulbo { get; set; }
    public int Cantidad { get; set; }

    public DetalleCosecha()
    {
    }

    public DetalleCosecha(int cosechaId, int loteId, ClaseBulbo claseBulbo, int cantidad)
    {
        if (loteId <= 0)
            throw new OperacionInvalidaException("El lote es requerido en el detalle de cosecha.");
        if (cantidad < 0)
            throw new OperacionInvalidaException("La cantidad cosechada no puede ser negativa.");

        CosechaId = cosechaId;
        LoteId = loteId;
        ClaseBulbo = claseBulbo;
        Cantidad = cantidad;
    }
}
