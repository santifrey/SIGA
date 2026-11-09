using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public abstract class ParticipanteComercial : ISoftDeletable
{
    public int Id { get; set; }
    public TipoParticipante TipoParticipante { get; set; }
    public EstadoParticipante Estado { get; set; } = EstadoParticipante.Activo;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? FechaEliminacion { get; set; }
}
