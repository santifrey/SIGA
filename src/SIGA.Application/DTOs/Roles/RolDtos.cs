namespace SIGA.Application.DTOs.Roles;

public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public int TotalUsuarios { get; set; }
    public List<string> Permisos { get; set; } = new();
}

public class CrearRolDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<string> Permisos { get; set; } = new();
}

public class ActualizarRolDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<string> Permisos { get; set; } = new();
}
