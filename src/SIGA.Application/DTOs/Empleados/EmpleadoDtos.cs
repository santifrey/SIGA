using SIGA.Domain.Enums;

namespace SIGA.Application.DTOs.Empleados;

public class EmpleadoDto
{
    public int Id { get; set; } // UsuarioId
    public int PersonaId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Legajo { get; set; }
    public string? Posicion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int? SucursalId { get; set; }
    public string? SucursalNombre { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public EstadoUsuario Estado { get; set; }
    public bool Activado { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CrearEmpleadoDto
{
    // Datos de Persona
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Legajo { get; set; }
    public string? Posicion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int? SucursalId { get; set; }

    // Datos de Usuario (Opcionales: si se omiten, Username = Email y Password = Documento)
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RolId { get; set; }
}

public class ActualizarEmpleadoDto
{
    // Datos de Persona
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Legajo { get; set; }
    public string? Posicion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int? SucursalId { get; set; }

    // Datos de Usuario
    public int RolId { get; set; }
    public EstadoUsuario Estado { get; set; }
    public string? Password { get; set; } // Opcional para actualización
}
