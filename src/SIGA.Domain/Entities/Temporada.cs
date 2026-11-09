using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Temporada : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public EstadoTemporada Estado { get; set; } = EstadoTemporada.Planificada;

    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }

    public virtual ICollection<Fila> Filas { get; set; } = new List<Fila>();
    public virtual ICollection<Plantacion> Plantaciones { get; set; } = new List<Plantacion>();
    public virtual ICollection<Lote> Lotes { get; set; } = new List<Lote>();
    public virtual ICollection<Cosecha> Cosechas { get; set; } = new List<Cosecha>();
    public virtual ICollection<PrecioClase> PreciosClase { get; set; } = new List<PrecioClase>();

    public Temporada()
    {
    }

    public Temporada(string nombre, int usuarioCreadorId, DateTime? fechaInicio = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new OperacionInvalidaException("El nombre de la temporada es requerido.");

        Nombre = nombre.Trim();
        UsuarioCreadorId = usuarioCreadorId;
        FechaInicio = fechaInicio ?? DateTime.UtcNow;
        Estado = EstadoTemporada.Planificada;
    }

    public void IniciarPlantacion()
    {
        if (Estado != EstadoTemporada.Planificada)
            throw new OperacionInvalidaException($"No se puede iniciar la plantación desde el estado '{Estado}'. Debe estar en 'Planificada'.");

        Estado = EstadoTemporada.Plantacion;
    }

    public void FinalizarPlantacion()
    {
        if (Estado != EstadoTemporada.Plantacion)
            throw new OperacionInvalidaException($"No se puede finalizar la plantación desde el estado '{Estado}'. Debe estar en 'Plantacion'.");

        Estado = EstadoTemporada.EnCrecimiento;
    }

    public void IniciarCosecha()
    {
        if (Estado != EstadoTemporada.EnCrecimiento && Estado != EstadoTemporada.Plantacion)
            throw new OperacionInvalidaException($"No se puede iniciar la cosecha desde el estado '{Estado}'. Debe estar en 'EnCrecimiento' o 'Plantacion'.");

        Estado = EstadoTemporada.Cosecha;
    }

    public void HabilitarComercializacion()
    {
        if (Estado != EstadoTemporada.Cosecha)
            throw new OperacionInvalidaException($"No se puede habilitar la comercialización desde el estado '{Estado}'. Debe estar en 'Cosecha'.");

        Estado = EstadoTemporada.Comercializacion;
    }

    public void Cerrar()
    {
        if (Estado == EstadoTemporada.Cerrada)
            throw new OperacionInvalidaException("La temporada ya se encuentra cerrada.");

        Estado = EstadoTemporada.Cerrada;
        FechaFin = DateTime.UtcNow;
    }
}
