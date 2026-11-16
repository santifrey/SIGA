namespace SIGA.Application.DTOs.Permisos;

public class PermisoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
}
