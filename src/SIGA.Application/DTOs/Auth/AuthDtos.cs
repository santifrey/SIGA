using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public int UsuarioId { get; set; }
    public int PersonaId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activado { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public List<string> Permisos { get; set; } = new();
    public List<string> Roles { get; set; } = new(); // Backward compatibility
}

public class UserInfoResponseDto
{
    public int UsuarioId { get; set; }
    public int PersonaId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activado { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public List<string> Permisos { get; set; } = new();
    public DateTime? UltimoAcceso { get; set; }
    public EstadoUsuario Estado { get; set; }
    public List<string> Roles { get; set; } = new(); // Backward compatibility
}
