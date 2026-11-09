using SIGA.Domain.Common;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;

namespace SIGA.Domain.Entities;

public class Plantacion : BaseEntity
{
    public int TemporadaId { get; set; }
    public virtual Temporada Temporada { get; set; } = null!;

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public EstadoPlantacion Estado { get; set; } = EstadoPlantacion.Planificada;

    public int UsuarioCreadorId { get; set; }
    public virtual Usuario? UsuarioCreador { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<DetallePlantacion> DetallesPlantacion { get; set; } = new List<DetallePlantacion>();

    public Plantacion()
    {
    }

    public Plantacion(int temporadaId, int usuarioCreadorId, string? observaciones = null)
    {
        if (temporadaId <= 0)
            throw new OperacionInvalidaException("La temporada es requerida para la plantación.");

        TemporadaId = temporadaId;
        UsuarioCreadorId = usuarioCreadorId;
        Observaciones = observaciones;
        FechaInicio = DateTime.UtcNow;
        Estado = EstadoPlantacion.Planificada;
    }

    public void Iniciar()
    {
        if (Estado != EstadoPlantacion.Planificada)
            throw new OperacionInvalidaException($"No se puede iniciar la plantación en estado '{Estado}'.");

        Estado = EstadoPlantacion.EnProceso;
    }

    public void Finalizar()
    {
        if (Estado != EstadoPlantacion.EnProceso)
            throw new OperacionInvalidaException($"No se puede finalizar una plantación en estado '{Estado}'.");

        Estado = EstadoPlantacion.Finalizada;
        FechaFin = DateTime.UtcNow;
    }
}
