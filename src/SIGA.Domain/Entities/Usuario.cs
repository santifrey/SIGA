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
    public DateTime? UltimoAcceso { get; set; }

    public virtual ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
