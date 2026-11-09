namespace SIGA.Domain.Entities;

public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    public int RolId { get; set; }
    public virtual Rol Rol { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
