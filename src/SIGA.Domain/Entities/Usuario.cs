using SIGA.Domain.Common;
using SIGA.Domain.Enums;

namespace SIGA.Domain.Entities;

public class Usuario : BaseEntity
{
    public int PersonaId { get; set; }
    public virtual Persona Persona { get; set; } = null!;

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public bool Activado { get; set; } = false;
    public DateTime? UltimoAcceso { get; set; }

    // Relación 1:N con Rol (cada usuario tiene exactamente un rol asignado)
    public int RolId { get; set; }
    public virtual Rol Rol { get; set; } = null!;
}
