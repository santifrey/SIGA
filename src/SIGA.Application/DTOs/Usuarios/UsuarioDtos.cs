using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Usuarios;

public class UsuarioDto
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EstadoUsuario Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUsuarioDto
{
    public int? PersonaId { get; set; }
    // En caso de que se cree la persona junto con el usuario:
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<int> RolIds { get; set; } = new();
}

public class UpdateUsuarioDto
{
    public EstadoUsuario Estado { get; set; }
    public List<int> RolIds { get; set; } = new();
}

public class CambiarEstadoUsuarioDto
{
    public EstadoUsuario Estado { get; set; }
}

public class CambiarPasswordDto
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}

public class AsignarRolesDto
{
    public List<int> RolIds { get; set; } = new();
}
