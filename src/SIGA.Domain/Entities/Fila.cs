using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Fila : BaseEntity
{
    public int SucursalId { get; set; }
    public virtual Sucursal Sucursal { get; set; } = null!;

    public int TemporadaId { get; set; }
    public virtual Temporada Temporada { get; set; } = null!;

    public string Identificacion { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public EstadoFila Estado { get; set; } = EstadoFila.Disponible;

    public virtual ICollection<Lote> Lotes { get; set; } = new List<Lote>();

    public Fila()
    {
    }

    public Fila(int sucursalId, int temporadaId, string identificacion, int capacidad = 0)
    {
        if (string.IsNullOrWhiteSpace(identificacion))
            throw new OperacionInvalidaException("La identificación de la fila es requerida.");

        if (capacidad < 0)
            throw new OperacionInvalidaException("La capacidad no puede ser negativa.");

        SucursalId = sucursalId;
        TemporadaId = temporadaId;
        Identificacion = identificacion.Trim();
        Capacidad = capacidad;
        Estado = EstadoFila.Disponible;
    }
}
