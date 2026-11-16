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

    public int? NumeroFila { get; set; }
    public int? Numero { get => NumeroFila; set => NumeroFila = value; }
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
        if (int.TryParse(Identificacion, out var num) && num > 0)
        {
            NumeroFila = num;
        }
        Capacidad = capacidad;
        Estado = EstadoFila.Disponible;
    }

    public Fila(int sucursalId, int temporadaId, int numeroFila, int capacidad = 0)
    {
        if (numeroFila <= 0)
            throw new OperacionInvalidaException("El número de fila debe ser mayor a cero.");

        if (capacidad < 0)
            throw new OperacionInvalidaException("La capacidad no puede ser negativa.");

        SucursalId = sucursalId;
        TemporadaId = temporadaId;
        NumeroFila = numeroFila;
        Identificacion = numeroFila.ToString();
        Capacidad = capacidad;
        Estado = EstadoFila.Disponible;
    }

    public int ObtenerOcupacionActual()
    {
        return Lotes?.Where(l => !l.IsDeleted).Sum(l => l.CantidadPlantadaTotal) ?? 0;
    }

    public int ObtenerCapacidadDisponible()
    {
        if (Capacidad <= 0) return int.MaxValue;
        var ocupacion = ObtenerOcupacionActual();
        return Math.Max(0, Capacidad - ocupacion);
    }

    public bool TieneCapacidadDisponible(int cantidad)
    {
        if (Capacidad <= 0) return true;
        return ObtenerOcupacionActual() + cantidad <= Capacidad;
    }
}
